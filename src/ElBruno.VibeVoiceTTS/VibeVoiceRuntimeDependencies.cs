using ElBruno.VibeVoiceTTS.Pipeline;

namespace ElBruno.VibeVoiceTTS;

internal sealed class VibeVoiceRuntimeDependencies
{
    public static VibeVoiceRuntimeDependencies Default { get; } = new();

    public Func<string, bool> IsModelAvailable { get; init; } = ModelManager.IsModelAvailable;

    public Func<string, string, bool> IsVoiceAvailable { get; init; } = ModelManager.IsVoiceAvailable;

    public Func<string, string, IProgress<DownloadProgress>?, CancellationToken, Task> EnsureModelAvailableAsync { get; init; }
        = ModelManager.EnsureModelAvailableAsync;

    public Func<string, string, string, IProgress<DownloadProgress>?, CancellationToken, Task> EnsureVoiceAvailableAsync { get; init; }
        = ModelManager.EnsureVoiceAvailableAsync;

    public Func<string, VibeVoiceOptions, IGenerationPipeline> CreatePipeline { get; init; }
        = static (modelPath, options) => new OnnxInferencePipeline(
            modelPath,
            options.DiffusionSteps,
            options.CfgScale,
            options.Seed,
            options.ExecutionProvider,
            options.GpuDeviceId);
}
