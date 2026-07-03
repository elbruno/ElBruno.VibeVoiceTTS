namespace ElBruno.VibeVoiceTTS;

/// <summary>
/// Generation timing checkpoints reported by the synthesizer.
/// </summary>
public enum GenerationMetricStage
{
    /// <summary>
    /// The first latent audio frame has been generated.
    /// </summary>
    FirstAudioReady,

    /// <summary>
    /// Full audio generation has completed.
    /// </summary>
    Completed
}
