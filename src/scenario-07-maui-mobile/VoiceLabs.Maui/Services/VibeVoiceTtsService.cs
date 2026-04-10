using ElBruno.VibeVoiceTTS;

namespace VoiceLabs.Maui.Services;

/// <summary>
/// Wraps IVibeVoiceSynthesizer for in-process TTS via ONNX — no Python backend needed.
/// </summary>
public class VibeVoiceTtsService
{
    private readonly IVibeVoiceSynthesizer _synthesizer;

    public VibeVoiceTtsService(IVibeVoiceSynthesizer synthesizer)
    {
        _synthesizer = synthesizer;
    }

    /// <summary>
    /// Downloads ONNX models if not already cached (~1.5 GB on first run).
    /// </summary>
    public async Task InitializeAsync(IProgress<string>? progress = null, CancellationToken ct = default)
    {
        var downloadProgress = new Progress<DownloadProgress>(p =>
        {
            var message = p.Stage switch
            {
                DownloadStage.Checking => "Checking models...",
                DownloadStage.Downloading => $"Downloading {p.CurrentFile ?? "models"} ({p.PercentComplete:F0}%)",
                DownloadStage.Validating => "Validating models...",
                DownloadStage.Complete => "Models ready",
                DownloadStage.Failed => $"Download failed: {p.Message}",
                _ => p.Message ?? "Preparing..."
            };
            progress?.Report(message);
        });

        await _synthesizer.EnsureModelAvailableAsync(downloadProgress, ct);
    }

    /// <summary>
    /// Returns all supported voices (Carter, Davis, Emma, Frank, Grace, Mike).
    /// </summary>
    public List<VoiceDisplayItem> GetVoices()
    {
        return _synthesizer.GetSupportedVoiceDetails()
            .Select(v => new VoiceDisplayItem
            {
                Name = v.Name,
                Language = v.Language,
                Gender = v.Gender
            })
            .ToList();
    }

    /// <summary>
    /// Generates speech audio and returns WAV bytes ready for playback.
    /// </summary>
    public async Task<byte[]?> GenerateAudioAsync(string text, string voiceName, CancellationToken ct = default)
    {
        var samples = await _synthesizer.GenerateAudioAsync(text, voiceName, ct);
        if (samples is not { Length: > 0 })
            return null;

        return ConvertToWavBytes(samples, sampleRate: 24000);
    }

    /// <summary>
    /// Builds a WAV file (RIFF/WAVE, 24 kHz, 16-bit PCM, mono) in memory from float samples.
    /// </summary>
    private static byte[] ConvertToWavBytes(float[] samples, int sampleRate)
    {
        const int bitsPerSample = 16;
        const int channels = 1;
        int byteRate = sampleRate * channels * bitsPerSample / 8;
        int blockAlign = channels * bitsPerSample / 8;
        int dataSize = samples.Length * blockAlign;

        using var ms = new MemoryStream(44 + dataSize);
        using var writer = new BinaryWriter(ms);

        // RIFF header
        writer.Write("RIFF"u8);
        writer.Write(36 + dataSize);
        writer.Write("WAVE"u8);

        // fmt sub-chunk
        writer.Write("fmt "u8);
        writer.Write(16);                // sub-chunk size
        writer.Write((short)1);          // PCM format
        writer.Write((short)channels);
        writer.Write(sampleRate);
        writer.Write(byteRate);
        writer.Write((short)blockAlign);
        writer.Write((short)bitsPerSample);

        // data sub-chunk
        writer.Write("data"u8);
        writer.Write(dataSize);

        foreach (var sample in samples)
        {
            var clamped = Math.Clamp(sample, -1f, 1f);
            var pcm = (short)(clamped * short.MaxValue);
            writer.Write(pcm);
        }

        writer.Flush();
        return ms.ToArray();
    }
}

/// <summary>
/// Simple display model for the voice picker UI.
/// </summary>
public class VoiceDisplayItem
{
    public string Name { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Gender { get; set; } = string.Empty;
    public string DisplayName => $"{Name} ({Gender}, {Language})";
}
