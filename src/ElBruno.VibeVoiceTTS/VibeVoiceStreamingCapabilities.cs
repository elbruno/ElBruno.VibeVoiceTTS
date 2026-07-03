namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// Describes how the synthesizer can emit streamed audio updates.
/// </summary>
public sealed record VibeVoiceStreamingCapabilities(
    bool SupportsProgressiveGeneration,
    bool SupportsChunkedDelivery);
