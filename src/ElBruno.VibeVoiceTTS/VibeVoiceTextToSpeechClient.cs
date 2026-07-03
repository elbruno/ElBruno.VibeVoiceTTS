using System.Runtime.CompilerServices;
using ElBruno.VibeVoiceTTS.Utils;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// Microsoft.Extensions.AI adapter over <see cref="VibeVoiceSynthesizer"/>.
/// </summary>
public sealed class VibeVoiceTextToSpeechClient : ITextToSpeechClient
{
    internal const string AudioFormatPropertyName = "audioFormat";
    internal const string SampleRatePropertyName = "sampleRate";
    internal const string VoiceIdPropertyName = "voiceId";

    private readonly VibeVoiceSynthesizer _synthesizer;
    private readonly IOptions<VibeVoiceTextToSpeechOptions> _optionsAccessor;
    private readonly TextToSpeechClientMetadata _metadata;
    private bool _disposed;

    /// <summary>
    /// Creates a new adapter over an existing <see cref="VibeVoiceSynthesizer"/>.
    /// </summary>
    public VibeVoiceTextToSpeechClient(
        VibeVoiceSynthesizer synthesizer,
        IOptions<VibeVoiceTextToSpeechOptions> options)
    {
        _synthesizer = synthesizer ?? throw new ArgumentNullException(nameof(synthesizer));
        _optionsAccessor = options ?? throw new ArgumentNullException(nameof(options));

        VibeVoiceTextToSpeechOptions configured = options.Value
            ?? throw new InvalidOperationException("VibeVoiceTextToSpeechOptions must be configured.");
        _metadata = new(
            configured.ProviderName,
            configured.ProviderUri,
            configured.DefaultModelId ?? synthesizer.HuggingFaceRepo);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        return
            serviceKey is not null ? null :
            serviceType == typeof(TextToSpeechClientMetadata) ? _metadata :
            serviceType == typeof(VibeVoiceSynthesizer) ? _synthesizer :
            serviceType == typeof(IVibeVoiceSynthesizer) ? _synthesizer :
            serviceType == typeof(VibeVoiceTextToSpeechOptions) ? _optionsAccessor.Value :
            serviceType == typeof(IOptions<VibeVoiceTextToSpeechOptions>) ? _optionsAccessor :
            serviceType.IsInstanceOfType(this) ? this :
            null;
    }

    /// <inheritdoc />
    public Task<TextToSpeechResponse> GetAudioAsync(
        string text,
        TextToSpeechOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(text);

        ResolvedRequest request = ResolveRequest(options);
        return GenerateResponseAsync(text, request, cancellationToken);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<TextToSpeechResponseUpdate> GetStreamingAudioAsync(
        string text,
        TextToSpeechOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(text);

        ResolvedRequest request = ResolveRequest(options);
        if (!request.Format.SupportsChunkStreaming)
        {
            foreach (TextToSpeechResponseUpdate update in
                (await GenerateResponseAsync(text, request, cancellationToken).ConfigureAwait(false)).ToTextToSpeechResponseUpdates())
            {
                yield return update;
            }

            yield break;
        }

        await foreach (VibeVoiceAudioChunk chunk in _synthesizer
            .GenerateAudioStreamingAsync(text, request.VoiceId, cancellationToken)
            .WithCancellation(cancellationToken)
            .ConfigureAwait(false))
        {
            yield return new TextToSpeechResponseUpdate(
                [CreateDataContent(chunk.Samples, request.Format, chunk.SampleRate)])
            {
                Kind = chunk.IsFinal
                    ? TextToSpeechResponseUpdateKind.AudioUpdated
                    : TextToSpeechResponseUpdateKind.AudioUpdating,
                ModelId = request.ModelId,
                ResponseId = request.ResponseId,
                AdditionalProperties = CreateAdditionalProperties(
                    request,
                    chunk.SampleRate,
                    chunk.SequenceNumber,
                    chunk.IsFinal),
                RawRepresentation = chunk
            };
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _disposed = true;
    }

    private async Task<TextToSpeechResponse> GenerateResponseAsync(
        string text,
        ResolvedRequest request,
        CancellationToken cancellationToken)
    {
        float[] audio = await _synthesizer
            .GenerateAudioAsync(text, request.VoiceId, cancellationToken)
            .ConfigureAwait(false);
        var content = CreateDataContent(audio, request.Format, _synthesizer.SampleRate);

        return new TextToSpeechResponse([content])
        {
            ModelId = request.ModelId,
            ResponseId = request.ResponseId,
            AdditionalProperties = CreateAdditionalProperties(
                request,
                _synthesizer.SampleRate,
                sequenceNumber: null,
                isFinal: true),
            RawRepresentation = audio
        };
    }

    private ResolvedRequest ResolveRequest(TextToSpeechOptions? options)
    {
        VibeVoiceTextToSpeechOptions configured = _optionsAccessor.Value;
        int sampleRate = ResolveSampleRate(options?.AdditionalProperties, _synthesizer.SampleRate);
        string voiceId = string.IsNullOrWhiteSpace(options?.VoiceId)
            ? configured.DefaultVoiceId
            : options!.VoiceId!;
        string resolvedVoiceId = VibeVoiceSynthesizer.ResolveVoiceName(voiceId);
        ResolvedAudioFormat format = ResolveAudioFormat(options?.AudioFormat ?? configured.DefaultAudioFormat, sampleRate);
        string responseId = $"vibevoice-{Guid.NewGuid():N}";

        return new ResolvedRequest(
            resolvedVoiceId,
            options?.ModelId ?? _metadata.DefaultModelId,
            format,
            responseId,
            options?.AdditionalProperties?.Clone());
    }

    private static int ResolveSampleRate(AdditionalPropertiesDictionary? properties, int defaultSampleRate)
    {
        if (properties is null || !properties.TryGetValue(SampleRatePropertyName, out object? value) || value is null)
        {
            return defaultSampleRate;
        }

        int requestedSampleRate = value switch
        {
            int intValue => intValue,
            long longValue when longValue is >= int.MinValue and <= int.MaxValue => (int)longValue,
            short shortValue => shortValue,
            byte byteValue => byteValue,
            string text when int.TryParse(text, out int parsed) => parsed,
            _ => throw new ArgumentException(
                $"AdditionalProperties['{SampleRatePropertyName}'] must be an integer value.",
                nameof(properties))
        };

        if (requestedSampleRate != defaultSampleRate)
        {
            throw new NotSupportedException(
                $"The configured synthesizer sample rate is {defaultSampleRate} Hz, but {requestedSampleRate} Hz was requested. " +
                "Configure VibeVoiceOptions.SampleRate on the synthesizer to change the output rate.");
        }

        return requestedSampleRate;
    }

    private static ResolvedAudioFormat ResolveAudioFormat(string? requestedFormat, int sampleRate)
    {
        string format = string.IsNullOrWhiteSpace(requestedFormat)
            ? VibeVoiceTextToSpeechOptions.WavMediaType
            : requestedFormat.Trim();

        if (format.Equals("wav", StringComparison.OrdinalIgnoreCase)
            || format.Equals(VibeVoiceTextToSpeechOptions.WavMediaType, StringComparison.OrdinalIgnoreCase))
        {
            return new ResolvedAudioFormat(
                AudioOutputKind.Wav,
                VibeVoiceTextToSpeechOptions.WavMediaType,
                "speech.wav",
                false);
        }

        if (TryResolvePcmFormat(format, sampleRate, out ResolvedAudioFormat resolved))
        {
            return resolved;
        }

        throw new NotSupportedException(
            $"Unsupported audio format '{requestedFormat}'. Supported formats are '{VibeVoiceTextToSpeechOptions.WavMediaType}', " +
            $"'audio/pcm;rate={sampleRate};channels=1;format=f32le', and 'audio/pcm;rate={sampleRate};channels=1;format=s16le'.");
    }

    private static bool TryResolvePcmFormat(string requestedFormat, int sampleRate, out ResolvedAudioFormat resolved)
    {
        resolved = default;

        if (requestedFormat.Equals("f32le", StringComparison.OrdinalIgnoreCase)
            || requestedFormat.Equals("pcm-f32le", StringComparison.OrdinalIgnoreCase))
        {
            resolved = CreatePcmFormat(AudioOutputKind.PcmFloat32, sampleRate);
            return true;
        }

        if (requestedFormat.Equals("s16le", StringComparison.OrdinalIgnoreCase)
            || requestedFormat.Equals("pcm-s16le", StringComparison.OrdinalIgnoreCase))
        {
            resolved = CreatePcmFormat(AudioOutputKind.PcmInt16, sampleRate);
            return true;
        }

        if (!requestedFormat.StartsWith("audio/pcm", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parameters = requestedFormat.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Skip(1)
            .Select(parameter => parameter.Split('=', 2, StringSplitOptions.TrimEntries))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0], parts => parts[1], StringComparer.OrdinalIgnoreCase);

        if (parameters.TryGetValue("rate", out string? rateText)
            && (!int.TryParse(rateText, out int rate) || rate != sampleRate))
        {
            throw new NotSupportedException(
                $"Requested PCM rate '{rateText}' is not supported. Configure the synthesizer for {sampleRate} Hz or request that exact rate.");
        }

        if (parameters.TryGetValue("channels", out string? channelsText)
            && (!int.TryParse(channelsText, out int channels) || channels != 1))
        {
            throw new NotSupportedException("Only mono PCM output (channels=1) is supported.");
        }

        if (!parameters.TryGetValue("format", out string? format))
        {
            throw new NotSupportedException("PCM output requires a 'format' parameter of either 'f32le' or 's16le'.");
        }

        if (format.Equals("f32le", StringComparison.OrdinalIgnoreCase))
        {
            resolved = CreatePcmFormat(AudioOutputKind.PcmFloat32, sampleRate);
            return true;
        }

        if (format.Equals("s16le", StringComparison.OrdinalIgnoreCase))
        {
            resolved = CreatePcmFormat(AudioOutputKind.PcmInt16, sampleRate);
            return true;
        }

        throw new NotSupportedException(
            $"PCM format '{format}' is not supported. Use 'f32le' or 's16le'.");
    }

    private static ResolvedAudioFormat CreatePcmFormat(AudioOutputKind kind, int sampleRate)
    {
        return kind switch
        {
            AudioOutputKind.PcmFloat32 => new ResolvedAudioFormat(
                kind,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    VibeVoiceTextToSpeechOptions.PcmFloat32MediaTypeTemplate,
                    sampleRate),
                "speech-f32le.pcm",
                true),
            AudioOutputKind.PcmInt16 => new ResolvedAudioFormat(
                kind,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    VibeVoiceTextToSpeechOptions.PcmInt16MediaTypeTemplate,
                    sampleRate),
                "speech-s16le.pcm",
                true),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
        };
    }

    private static DataContent CreateDataContent(
        ReadOnlyMemory<float> audio,
        ResolvedAudioFormat format,
        int sampleRate)
    {
        byte[] bytes = format.Kind switch
        {
            AudioOutputKind.Wav => AudioWriter.GetWavBytes(audio, sampleRate),
            AudioOutputKind.PcmFloat32 => AudioWriter.GetPcmFloat32LeBytes(audio),
            AudioOutputKind.PcmInt16 => AudioWriter.GetPcm16LeBytes(audio),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        return new DataContent(bytes, format.MediaType)
        {
            Name = format.FileName
        };
    }

    private static AdditionalPropertiesDictionary CreateAdditionalProperties(
        ResolvedRequest request,
        int sampleRate,
        long? sequenceNumber,
        bool isFinal)
    {
        AdditionalPropertiesDictionary properties = request.AdditionalProperties?.Clone() ?? new AdditionalPropertiesDictionary();
        properties[AudioFormatPropertyName] = request.Format.MediaType;
        properties[SampleRatePropertyName] = sampleRate;
        properties[VoiceIdPropertyName] = request.VoiceId;
        properties["isFinal"] = isFinal;

        if (sequenceNumber is not null)
        {
            properties["sequenceNumber"] = sequenceNumber.Value;
        }

        return properties;
    }

    private enum AudioOutputKind
    {
        Wav,
        PcmFloat32,
        PcmInt16
    }

    private readonly record struct ResolvedAudioFormat(
        AudioOutputKind Kind,
        string MediaType,
        string FileName,
        bool SupportsChunkStreaming);

    private readonly record struct ResolvedRequest(
        string VoiceId,
        string? ModelId,
        ResolvedAudioFormat Format,
        string ResponseId,
        AdditionalPropertiesDictionary? AdditionalProperties);
}
