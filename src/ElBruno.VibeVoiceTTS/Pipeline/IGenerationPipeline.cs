namespace ElBruno.VibeVoiceTTS.Pipeline;

internal interface IGenerationPipeline : IDisposable
{
    float[] GenerateAudio(
        string text,
        string voice,
        CancellationToken cancellationToken,
        Action<GenerationMetric>? reportMetric = null);

    string[] GetAvailableVoices();
}
