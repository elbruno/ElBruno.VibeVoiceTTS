namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// Configuration for the Microsoft.Extensions.AI text-to-speech adapter.
/// </summary>
public sealed class VibeVoiceTextToSpeechOptions
{
    /// <summary>
    /// Canonical WAV media type.
    /// </summary>
    public const string WavMediaType = "audio/wav";

    /// <summary>
    /// Canonical PCM float media type template.
    /// </summary>
    public const string PcmFloat32MediaTypeTemplate = "audio/pcm;rate={0};channels=1;format=f32le";

    /// <summary>
    /// Canonical PCM 16-bit integer media type template.
    /// </summary>
    public const string PcmInt16MediaTypeTemplate = "audio/pcm;rate={0};channels=1;format=s16le";

    /// <summary>
    /// Default voice ID used when the request does not provide one.
    /// </summary>
    public string DefaultVoiceId { get; set; } = nameof(VibeVoicePreset.Carter);

    /// <summary>
    /// Default audio format returned by the adapter.
    /// </summary>
    public string DefaultAudioFormat { get; set; } = WavMediaType;

    /// <summary>
    /// Provider name exposed through <see cref="Microsoft.Extensions.AI.TextToSpeechClientMetadata"/>.
    /// </summary>
    public string ProviderName { get; set; } = "vibevoice";

    /// <summary>
    /// Provider URI exposed through <see cref="Microsoft.Extensions.AI.TextToSpeechClientMetadata"/>.
    /// </summary>
    public Uri ProviderUri { get; set; } = new("https://huggingface.co/elbruno/VibeVoice-Realtime-0.5B-ONNX");

    /// <summary>
    /// Default model ID exposed through <see cref="Microsoft.Extensions.AI.TextToSpeechClientMetadata"/>.
    /// If null, the adapter falls back to the synthesizer's configured HuggingFace repository.
    /// </summary>
    public string? DefaultModelId { get; set; }
}
