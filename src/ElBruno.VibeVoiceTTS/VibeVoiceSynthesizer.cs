using ElBruno.VibeVoiceTTS.Pipeline;

namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// VibeVoice text-to-speech synthesizer using ONNX Runtime.
/// Handles model management (auto-download) and inference.
/// </summary>
public sealed class VibeVoiceSynthesizer : IVibeVoiceSynthesizer
{
    private readonly VibeVoiceOptions _options;
    private readonly VibeVoiceRuntimeDependencies _dependencies;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private readonly SemaphoreSlim _generationLock = new(1, 1);
    private IGenerationPipeline? _pipeline;
    private bool _disposed;

    /// <inheritdoc/>
    public event EventHandler<GenerationMetric>? GenerationMetricReported;

    /// <summary>
    /// Creates a synthesizer with default options (models stored in shared OS cache).
    /// </summary>
    public VibeVoiceSynthesizer() : this(new VibeVoiceOptions()) { }

    /// <summary>
    /// Creates a synthesizer with the specified options.
    /// </summary>
    public VibeVoiceSynthesizer(VibeVoiceOptions options) : this(options, VibeVoiceRuntimeDependencies.Default)
    {
    }

    internal VibeVoiceSynthesizer(VibeVoiceOptions options, VibeVoiceRuntimeDependencies dependencies)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
    }

    /// <inheritdoc/>
    public string ModelPath => _options.GetEffectiveModelPath();

    /// <inheritdoc/>
    public bool IsModelAvailable => _dependencies.IsModelAvailable(ModelPath);

    /// <inheritdoc/>
    public async Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (IsModelAvailable)
        {
            progress?.Report(new DownloadProgress
            {
                Stage = DownloadStage.Complete,
                PercentComplete = 100,
                Message = "Model files already available."
            });
            return;
        }

        await _dependencies.EnsureModelAvailableAsync(
            ModelPath,
            _options.HuggingFaceRepo,
            progress,
            cancellationToken);
    }

    /// <inheritdoc/>
    public Task<float[]> GenerateAudioAsync(
        string text,
        VibeVoicePreset voice,
        CancellationToken cancellationToken = default)
    {
        ValidateVoicePreset(voice);
        return GenerateAudioAsync(text, voice.ToVoiceName(), cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<float[]> GenerateAudioAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _generationLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            ArgumentException.ThrowIfNullOrWhiteSpace(voiceName);
            ValidateTextLength(text);
            cancellationToken.ThrowIfCancellationRequested();

            // Resolve short preset names (e.g. "Carter") to internal names (e.g. "en-Carter_man")
            var resolvedName = ResolveVoiceName(voiceName);

            // Auto-download voice if not available on disk
            if (!_dependencies.IsVoiceAvailable(ModelPath, resolvedName))
            {
                await _dependencies.EnsureVoiceAvailableAsync(
                    ModelPath, _options.HuggingFaceRepo, resolvedName, null, cancellationToken);

                // Reload pipeline to pick up newly downloaded voice
                await ReloadPipelineAsync();
            }

            cancellationToken.ThrowIfCancellationRequested();
            var pipeline = await GetOrCreatePipelineAsync(cancellationToken);

            // Run inference on a thread pool thread to avoid blocking
            return await Task.Run(
                () => pipeline.GenerateAudio(text, resolvedName, cancellationToken, ReportGenerationMetric),
                cancellationToken);
        }
        finally
        {
            _generationLock.Release();
        }
    }

    /// <inheritdoc/>
    public void SaveWav(string path, float[] audioSamples)
    {
        Utils.AudioWriter.SaveWav(path, audioSamples, _options.SampleRate);
    }

    /// <inheritdoc/>
    public string[] GetAvailableVoices()
    {
        var pipeline = _pipeline;
        if (pipeline is not null)
        {
            return pipeline.GetAvailableVoices()
                .Select(internalName =>
                {
                    if (VibeVoicePresetExtensions.TryParseVoice(internalName, out var preset))
                        return preset.ToString();
                    return internalName;
                })
                .ToArray();
        }

        // No pipeline loaded — check disk for downloaded voices
        return Enum.GetValues<VibeVoicePreset>()
            .Where(p => _dependencies.IsVoiceAvailable(ModelPath, p.ToVoiceName()))
            .Select(p => p.ToString())
            .ToArray();
    }

    /// <inheritdoc/>
    public VoiceInfo[] GetAvailableVoiceDetails()
    {
        var pipeline = _pipeline;
        if (pipeline is not null)
        {
            return pipeline.GetAvailableVoices()
                .Select(internalName =>
                {
                    if (VibeVoicePresetExtensions.TryParseVoice(internalName, out var preset))
                        return preset.ToVoiceInfo();

                    var parts = internalName.Split('-', 2);
                    var lang = parts.Length > 1 ? parts[0] : "unknown";
                    var rest = parts.Length > 1 ? parts[1] : internalName;
                    var nameParts = rest.Split('_', 2);
                    var name = nameParts[0];
                    var gender = nameParts.Length > 1 ? nameParts[1] : "unknown";
                    return new VoiceInfo(name, internalName, lang, gender);
                })
                .ToArray();
        }

        // No pipeline loaded — check disk for downloaded voices
        return Enum.GetValues<VibeVoicePreset>()
            .Where(p => _dependencies.IsVoiceAvailable(ModelPath, p.ToVoiceName()))
            .Select(p => p.ToVoiceInfo())
            .ToArray();
    }

    /// <inheritdoc/>
    public string[] GetSupportedVoices()
    {
        return Enum.GetNames<VibeVoicePreset>();
    }

    /// <inheritdoc/>
    public VoiceInfo[] GetSupportedVoiceDetails()
    {
        return Enum.GetValues<VibeVoicePreset>()
            .Select(p => p.ToVoiceInfo())
            .ToArray();
    }

    /// <inheritdoc/>
    public async Task EnsureVoiceAvailableAsync(
        string voiceName,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _generationLock.WaitAsync(cancellationToken);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            ArgumentException.ThrowIfNullOrWhiteSpace(voiceName);

            var resolvedName = ResolveVoiceName(voiceName);

            await _dependencies.EnsureVoiceAvailableAsync(
                ModelPath, _options.HuggingFaceRepo, resolvedName, progress, cancellationToken);

            // Reload pipeline to pick up newly downloaded voice
            await ReloadPipelineAsync();
        }
        finally
        {
            _generationLock.Release();
        }
    }

    /// <summary>
    /// Resolves a voice name, mapping short enum names (e.g. "Carter") to internal preset names (e.g. "en-Carter_man").
    /// If the name is already an internal name, returns it unchanged.
    /// </summary>
    internal static string ResolveVoiceName(string voiceName)
    {
        // If it matches a preset enum name, convert to internal name
        if (VibeVoicePresetExtensions.TryParseVoice(voiceName, out var preset))
            return preset.ToVoiceName();

        // Otherwise assume it's already an internal name (e.g. "en-Carter_man")
        return voiceName;
    }

    /// <summary>
    /// Validates that text input is within the configured length limit.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when text exceeds the configured length limit.</exception>
    internal void ValidateTextLength(string text)
    {
        if (_options.MaxTextLength == 0 || text.Length <= _options.MaxTextLength)
            return;

        throw new ArgumentException(
                $"Text input exceeds maximum length of {_options.MaxTextLength} characters (received {text.Length} characters).",
                nameof(text));
    }

    /// <summary>
    /// Validates that a voice preset enum value is defined.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when voice preset is not a valid enum value.</exception>
    internal static void ValidateVoicePreset(VibeVoicePreset voice)
    {
        if (!Enum.IsDefined(typeof(VibeVoicePreset), voice))
            throw new ArgumentException($"Invalid voice preset value: {voice}", nameof(voice));
    }

    private async Task<IGenerationPipeline> GetOrCreatePipelineAsync(CancellationToken cancellationToken)
    {
        if (_pipeline is not null)
            return _pipeline;

        await _initLock.WaitAsync(cancellationToken);
        try
        {
            if (_pipeline is not null)
                return _pipeline;

            if (!IsModelAvailable)
                throw new InvalidOperationException(
                    $"Model files not found at '{ModelPath}'. Call EnsureModelAvailableAsync() first or provide a valid model path.");

            _pipeline = _dependencies.CreatePipeline(ModelPath, _options);

            return _pipeline;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task ReloadPipelineAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            _pipeline?.Dispose();
            _pipeline = null;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pipeline?.Dispose();
        _initLock.Dispose();
        _generationLock.Dispose();
    }

    private void ReportGenerationMetric(GenerationMetric metric)
    {
        GenerationMetricReported?.Invoke(this, metric);
    }
}
