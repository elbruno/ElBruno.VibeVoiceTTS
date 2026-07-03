namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// A streamed slice of generated PCM audio.
/// </summary>
public sealed record VibeVoiceAudioChunk(
    ReadOnlyMemory<float> Samples,
    int SampleRate,
    long SequenceNumber,
    bool IsFinal);
