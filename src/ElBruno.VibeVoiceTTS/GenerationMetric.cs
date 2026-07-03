namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// Timing metric emitted during speech generation.
/// </summary>
public sealed class GenerationMetric
{
    /// <summary>
    /// Which generation checkpoint emitted this metric.
    /// </summary>
    public required GenerationMetricStage Stage { get; init; }

    /// <summary>
    /// Internal voice name used for generation (for example, "en-Carter_man").
    /// </summary>
    public required string VoiceName { get; init; }

    /// <summary>
    /// Elapsed time since generation started.
    /// </summary>
    public required TimeSpan Elapsed { get; init; }

    /// <summary>
    /// Number of latent frames generated when the metric was emitted.
    /// </summary>
    public required int FramesGenerated { get; init; }
}
