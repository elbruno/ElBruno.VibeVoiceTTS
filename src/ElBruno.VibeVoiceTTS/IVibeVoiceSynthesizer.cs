namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// Interface for VibeVoice text-to-speech synthesis.
/// </summary>
public interface IVibeVoiceSynthesizer : IDisposable
{
    /// <summary>
    /// Raised when generation reaches the first-audio and completion checkpoints.
    /// </summary>
    event EventHandler<GenerationMetric>? GenerationMetricReported;

    /// <summary>
    /// Gets the streaming behavior supported by this synthesizer implementation.
    /// </summary>
    VibeVoiceStreamingCapabilities StreamingCapabilities { get; }

    /// <summary>
    /// Gets whether all required ONNX model files are present at the configured model path.
    /// </summary>
    bool IsModelAvailable { get; }

    /// <summary>
    /// Gets the effective model path (configured or default cache location).
    /// </summary>
    string ModelPath { get; }

    /// <summary>
    /// Ensures model files are available, downloading from HuggingFace if needed.
    /// </summary>
    Task EnsureModelAvailableAsync(
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates audio samples from text using the specified voice preset.
    /// A single synthesizer instance serializes generation requests to protect the shared ONNX pipeline.
    /// If the synthesizer is already busy, waiting for capacity honors the supplied cancellation token.
    /// </summary>
    /// <returns>Float array of audio samples at 24kHz, normalized to [-1, 1].</returns>
    Task<float[]> GenerateAudioAsync(
        string text,
        VibeVoicePreset voice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates audio samples from text using a voice name string.
    /// A single synthesizer instance serializes generation requests to protect the shared ONNX pipeline.
    /// If the synthesizer is already busy, waiting for capacity honors the supplied cancellation token.
    /// </summary>
    Task<float[]> GenerateAudioAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates ordered PCM chunks using the specified voice preset.
    /// Current ONNX inference completes waveform generation before chunk emission, so
    /// SupportsProgressiveGeneration is false while SupportsChunkedDelivery is true.
    /// </summary>
    IAsyncEnumerable<VibeVoiceAudioChunk> GenerateAudioStreamingAsync(
        string text,
        VibeVoicePreset voice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates ordered PCM chunks using a voice name string.
    /// Current ONNX inference completes waveform generation before chunk emission, so
    /// SupportsProgressiveGeneration is false while SupportsChunkedDelivery is true.
    /// </summary>
    IAsyncEnumerable<VibeVoiceAudioChunk> GenerateAudioStreamingAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves audio samples to a WAV file (24kHz, 16-bit PCM).
    /// </summary>
    void SaveWav(string path, float[] audioSamples);

    /// <summary>
    /// Returns the friendly names of voice presets currently downloaded on disk (e.g. "Carter", "Emma").
    /// These names can be used directly with GenerateAudioAsync.
    /// Use GetSupportedVoices() to see all voices that can be downloaded on demand.
    /// </summary>
    string[] GetAvailableVoices();

    /// <summary>
    /// Returns detailed information about voice presets currently downloaded on disk.
    /// </summary>
    VoiceInfo[] GetAvailableVoiceDetails();

    /// <summary>
    /// Returns the friendly names of all supported voice presets, including those not yet downloaded.
    /// Voices not on disk will be auto-downloaded when first used with GenerateAudioAsync.
    /// </summary>
    string[] GetSupportedVoices();

    /// <summary>
    /// Returns detailed information about all supported voice presets, including those not yet downloaded.
    /// </summary>
    VoiceInfo[] GetSupportedVoiceDetails();

    /// <summary>
    /// Downloads a specific voice preset if not already available.
    /// Accepts both short names ("Davis") and internal names ("en-Davis_man").
    /// This operation is serialized with generation so the shared pipeline cannot be reloaded mid-request.
    /// </summary>
    Task EnsureVoiceAvailableAsync(
        string voiceName,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
