using System.Collections.Concurrent;
using ElBruno.VibeVoiceTTS.Pipeline;
using ElBruno.VibeVoiceTTS;

namespace ElBruno.VibeVoiceTTS.Tests;

public class VibeVoiceSynthesizerTests
{
    [Fact]
    public void Constructor_WithDefaultOptions_DoesNotThrow()
    {
        using var tts = new VibeVoiceSynthesizer();
        Assert.NotNull(tts);
    }

    [Fact]
    public void Constructor_WithCustomOptions_DoesNotThrow()
    {
        var opts = new VibeVoiceOptions { DiffusionSteps = 10, CfgScale = 2.0f };
        using var tts = new VibeVoiceSynthesizer(opts);
        Assert.NotNull(tts);
    }

    [Fact]
    public void Constructor_NullOptions_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new VibeVoiceSynthesizer(null!));
    }

    [Fact]
    public void ModelPath_ReturnsNonEmpty()
    {
        using var tts = new VibeVoiceSynthesizer();
        Assert.False(string.IsNullOrWhiteSpace(tts.ModelPath));
    }

    [Fact]
    public void GetAvailableVoices_ReturnsOnlyDownloadedVoices()
    {
        using var tts = new VibeVoiceSynthesizer();
        string[] voices = tts.GetAvailableVoices();
        // Only Carter and Emma are downloaded by default
        Assert.DoesNotContain("en-Carter_man", voices); // Should be friendly names
        // Voices returned should be a subset of supported voices
        string[] supported = tts.GetSupportedVoices();
        foreach (var v in voices)
            Assert.Contains(v, supported);
    }

    [Fact]
    public void GetSupportedVoices_ReturnsAllSixPresets()
    {
        using var tts = new VibeVoiceSynthesizer();
        string[] voices = tts.GetSupportedVoices();
        Assert.Equal(6, voices.Length);
        Assert.Contains("Carter", voices);
        Assert.Contains("Davis", voices);
        Assert.Contains("Emma", voices);
        Assert.Contains("Frank", voices);
        Assert.Contains("Grace", voices);
        Assert.Contains("Mike", voices);
    }

    [Fact]
    public void GetSupportedVoiceDetails_ReturnsAllSixPresets()
    {
        using var tts = new VibeVoiceSynthesizer();
        VoiceInfo[] details = tts.GetSupportedVoiceDetails();
        Assert.Equal(6, details.Length);
    }

    [Fact]
    public void GetSupportedVoiceDetails_ContainsCorrectMetadata()
    {
        using var tts = new VibeVoiceSynthesizer();
        VoiceInfo[] details = tts.GetSupportedVoiceDetails();

        var carter = details.First(v => v.Name == "Carter");
        Assert.Equal("en-Carter_man", carter.InternalName);
        Assert.Equal("en", carter.Language);
        Assert.Equal("man", carter.Gender);

        var emma = details.First(v => v.Name == "Emma");
        Assert.Equal("en-Emma_woman", emma.InternalName);
        Assert.Equal("en", emma.Language);
        Assert.Equal("woman", emma.Gender);
    }

    [Fact]
    public void GetAvailableVoiceDetails_NamesMatchGetAvailableVoices()
    {
        using var tts = new VibeVoiceSynthesizer();
        string[] voices = tts.GetAvailableVoices();
        VoiceInfo[] details = tts.GetAvailableVoiceDetails();

        Assert.Equal(voices.Length, details.Length);
        for (int i = 0; i < voices.Length; i++)
            Assert.Equal(voices[i], details[i].Name);
    }

    [Theory]
    [InlineData("Carter", "en-Carter_man")]
    [InlineData("carter", "en-Carter_man")]
    [InlineData("Emma", "en-Emma_woman")]
    [InlineData("Davis", "en-Davis_man")]
    [InlineData("Frank", "en-Frank_man")]
    [InlineData("Grace", "en-Grace_woman")]
    [InlineData("Mike", "en-Mike_man")]
    public void ResolveVoiceName_MapsShortNamesToInternalNames(string input, string expected)
    {
        Assert.Equal(expected, VibeVoiceSynthesizer.ResolveVoiceName(input));
    }

    [Theory]
    [InlineData("en-Carter_man")]
    [InlineData("en-Emma_woman")]
    public void ResolveVoiceName_ResolvesInternalNamesToSame(string input)
    {
        // Internal names map back to the same internal name via preset lookup
        Assert.Equal(input, VibeVoiceSynthesizer.ResolveVoiceName(input));
    }

    [Theory]
    [InlineData("custom-voice")]
    [InlineData("fr-Marie_woman")]
    public void ResolveVoiceName_PassesThroughUnknownNames(string input)
    {
        Assert.Equal(input, VibeVoiceSynthesizer.ResolveVoiceName(input));
    }

    [Theory]
    [InlineData("en-Carter_man", "Carter")]
    [InlineData("en-Emma_woman", "Emma")]
    [InlineData("en-Mike_man", "Mike")]
    public void TryParseVoice_ParsesInternalNames(string internalName, string expectedEnumName)
    {
        Assert.True(VibeVoicePresetExtensions.TryParseVoice(internalName, out var preset));
        Assert.Equal(expectedEnumName, preset.ToString());
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        var tts = new VibeVoiceSynthesizer();
        tts.Dispose();
        tts.Dispose(); // Should not throw
    }

    [Fact]
    public async Task GenerateAudioAsync_AfterDispose_Throws()
    {
        var tts = new VibeVoiceSynthesizer();
        tts.Dispose();
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => tts.GenerateAudioAsync("test", "Carter"));
    }

    [Fact]
    public async Task GenerateAudioAsync_NullText_Throws()
    {
        using var tts = new VibeVoiceSynthesizer(new VibeVoiceOptions
        {
            ModelPath = VibeVoiceOptions.GetDefaultModelPath()
        });

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => tts.GenerateAudioAsync(null!, "Carter"));
    }

    [Fact]
    public async Task GenerateAudioAsync_EmptyText_Throws()
    {
        using var tts = new VibeVoiceSynthesizer(new VibeVoiceOptions
        {
            ModelPath = VibeVoiceOptions.GetDefaultModelPath()
        });

        await Assert.ThrowsAsync<ArgumentException>(
            () => tts.GenerateAudioAsync("", "Carter"));
    }

    [Fact]
    public void ValidateTextLength_UsesConfiguredLimit()
    {
        using var tts = new VibeVoiceSynthesizer(new VibeVoiceOptions
        {
            MaxTextLength = 10
        });

        tts.ValidateTextLength("short text");
        var ex = Assert.Throws<ArgumentException>(() => tts.ValidateTextLength("this text is too long"));
        Assert.Contains("maximum length of 10", ex.Message);
    }

    [Fact]
    public void ValidateTextLength_AllowsDisabledLimit()
    {
        using var tts = new VibeVoiceSynthesizer(new VibeVoiceOptions
        {
            MaxTextLength = 0
        });

        tts.ValidateTextLength(new string('a', 5000));
    }

    [Fact]
    public void GetVoiceFiles_ReturnsExpectedFileCount()
    {
        var files = ModelManager.GetVoiceFiles("en-Carter_man");
        // metadata.json + 40 TTS KV + 8 LM KV + 40 negative TTS KV + negative/tts_lm_hidden + tts_lm_hidden + lm_hidden = 92
        Assert.Equal(92, files.Count);
    }

    [Fact]
    public void GetVoiceFiles_ContainsMetadata()
    {
        var files = ModelManager.GetVoiceFiles("en-Davis_man");
        Assert.Contains("voices/en-Davis_man/metadata.json", files);
    }

    [Fact]
    public void GetVoiceFiles_ContainsKvCacheFiles()
    {
        var files = ModelManager.GetVoiceFiles("en-Emma_woman");
        Assert.Contains("voices/en-Emma_woman/tts_kv_key_0.npy", files);
        Assert.Contains("voices/en-Emma_woman/tts_kv_value_19.npy", files);
        Assert.Contains("voices/en-Emma_woman/lm_kv_key_0.npy", files);
        Assert.Contains("voices/en-Emma_woman/lm_kv_value_3.npy", files);
    }

    [Fact]
    public void GetVoiceFiles_ContainsNegativePathFiles()
    {
        var files = ModelManager.GetVoiceFiles("en-Frank_man");
        Assert.Contains("voices/en-Frank_man/negative/tts_lm_hidden.npy", files);
        Assert.Contains("voices/en-Frank_man/negative/tts_kv_key_0.npy", files);
        Assert.Contains("voices/en-Frank_man/negative/tts_kv_value_19.npy", files);
    }

    [Fact]
    public void GetVoiceFiles_ContainsHiddenStateFiles()
    {
        var files = ModelManager.GetVoiceFiles("en-Grace_woman");
        Assert.Contains("voices/en-Grace_woman/tts_lm_hidden.npy", files);
        Assert.Contains("voices/en-Grace_woman/lm_hidden.npy", files);
    }

    [Fact]
    public void IsVoiceAvailable_ReturnsFalse_ForNonExistentPath()
    {
        Assert.False(ModelManager.IsVoiceAvailable(@"C:\nonexistent\path", "en-Carter_man"));
    }

    [Fact]
    public async Task EnsureVoiceAvailableAsync_AlreadyAvailable_ReportsComplete()
    {
        // Use the default model path — if Carter is downloaded, should report Complete
        var modelPath = VibeVoiceOptions.GetDefaultModelPath();
        if (!ModelManager.IsVoiceAvailable(modelPath, "en-Carter_man"))
            return; // Skip if models not available

        var reports = new List<DownloadProgress>();
        var progress = new Progress<DownloadProgress>(p => reports.Add(p));

        await ModelManager.EnsureVoiceAvailableAsync(
            modelPath, "elbruno/VibeVoice-Realtime-0.5B-ONNX", "en-Carter_man", progress);

        // Allow time for Progress<T> callbacks
        await Task.Delay(100);

        Assert.NotEmpty(reports);
        Assert.Equal(DownloadStage.Complete, reports.Last().Stage);
        Assert.Equal(100, reports.Last().PercentComplete);
    }

    [Fact]
    public async Task GenerateAudioAsync_WhenBusy_WaitingRequestCanBeCancelled()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var tts = CreateTestSynthesizer(new BlockingPipeline(firstStarted, releaseFirst));

        Task<float[]> first = tts.GenerateAudioAsync("first", "Carter");
        await firstStarted.Task;

        using var cts = new CancellationTokenSource();
        Task<float[]> second = tts.GenerateAudioAsync("second", "Emma", cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);

        releaseFirst.SetResult();
        await first;
    }

    [Fact]
    public async Task GenerateAudioAsync_ReportsFirstAudioAndCompletionMetrics()
    {
        using var tts = CreateTestSynthesizer(new MetricsPipeline());
        var reports = new List<GenerationMetric>();
        tts.GenerationMetricReported += (_, metric) => reports.Add(metric);

        float[] audio = await tts.GenerateAudioAsync("hello", "Carter");

        Assert.Single(audio);
        Assert.Equal(2, reports.Count);
        Assert.Equal(GenerationMetricStage.FirstAudioReady, reports[0].Stage);
        Assert.Equal(GenerationMetricStage.Completed, reports[1].Stage);
        Assert.Equal("en-Carter_man", reports[0].VoiceName);
        Assert.Equal("en-Carter_man", reports[1].VoiceName);
        Assert.Equal(1, reports[0].FramesGenerated);
        Assert.Equal(2, reports[1].FramesGenerated);
        Assert.True(reports[1].Elapsed >= reports[0].Elapsed);
    }

    [Fact]
    public async Task GenerateAudioAsync_StressSwitchesVoicesWithoutCrossRequestLeakage()
    {
        var pipeline = new TrackingPipeline();
        using var tts = CreateTestSynthesizer(pipeline);
        string[] voices = ["Carter", "Emma", "Davis", "Grace"];

        Task<float[]>[] tasks = Enumerable.Range(0, 32)
            .Select(i => tts.GenerateAudioAsync($"text-{i}", voices[i % voices.Length]))
            .ToArray();

        float[][] results = await Task.WhenAll(tasks);

        Assert.Equal(1, pipeline.MaxConcurrency);
        Assert.Equal(32, pipeline.Requests.Count);

        for (int i = 0; i < results.Length; i++)
        {
            string expectedVoice = VibeVoiceSynthesizer.ResolveVoiceName(voices[i % voices.Length]);
            Assert.Single(results[i]);
            Assert.Equal(TrackingPipeline.GetVoiceMarker(expectedVoice), (int)results[i][0]);
        }
    }

    private static VibeVoiceSynthesizer CreateTestSynthesizer(IGenerationPipeline pipeline)
    {
        return new VibeVoiceSynthesizer(
            new VibeVoiceOptions { ModelPath = VibeVoiceOptions.GetDefaultModelPath() },
            new VibeVoiceRuntimeDependencies
            {
                IsModelAvailable = static _ => true,
                IsVoiceAvailable = static (_, _) => true,
                EnsureModelAvailableAsync = static (_, _, _, _) => Task.CompletedTask,
                EnsureVoiceAvailableAsync = static (_, _, _, _, _) => Task.CompletedTask,
                CreatePipeline = (_, _) => pipeline
            });
    }

    private sealed class BlockingPipeline(TaskCompletionSource firstStarted, TaskCompletionSource releaseFirst) : IGenerationPipeline
    {
        private int _callCount;

        public float[] GenerateAudio(string text, string voice, CancellationToken cancellationToken, Action<GenerationMetric>? reportMetric = null)
        {
            if (Interlocked.Increment(ref _callCount) == 1)
            {
                firstStarted.SetResult();
                releaseFirst.Task.GetAwaiter().GetResult();
            }

            return [1f];
        }

        public string[] GetAvailableVoices() => ["en-Carter_man", "en-Emma_woman"];

        public void Dispose()
        {
        }
    }

    private sealed class MetricsPipeline : IGenerationPipeline
    {
        public float[] GenerateAudio(string text, string voice, CancellationToken cancellationToken, Action<GenerationMetric>? reportMetric = null)
        {
            reportMetric?.Invoke(new GenerationMetric
            {
                Stage = GenerationMetricStage.FirstAudioReady,
                VoiceName = voice,
                Elapsed = TimeSpan.FromMilliseconds(15),
                FramesGenerated = 1
            });
            reportMetric?.Invoke(new GenerationMetric
            {
                Stage = GenerationMetricStage.Completed,
                VoiceName = voice,
                Elapsed = TimeSpan.FromMilliseconds(40),
                FramesGenerated = 2
            });
            return [1f];
        }

        public string[] GetAvailableVoices() => ["en-Carter_man"];

        public void Dispose()
        {
        }
    }

    private sealed class TrackingPipeline : IGenerationPipeline
    {
        private static readonly IReadOnlyDictionary<string, int> VoiceMarkers = new Dictionary<string, int>
        {
            ["en-Carter_man"] = 101,
            ["en-Davis_man"] = 102,
            ["en-Emma_woman"] = 103,
            ["en-Grace_woman"] = 104
        };

        private int _currentConcurrency;
        public int MaxConcurrency { get; private set; }
        public ConcurrentQueue<string> Requests { get; } = new();

        public static int GetVoiceMarker(string voice) => VoiceMarkers[voice];

        public float[] GenerateAudio(string text, string voice, CancellationToken cancellationToken, Action<GenerationMetric>? reportMetric = null)
        {
            int current = Interlocked.Increment(ref _currentConcurrency);
            MaxConcurrency = Math.Max(MaxConcurrency, current);
            Requests.Enqueue(voice);

            try
            {
                Thread.Sleep(10);
                return [GetVoiceMarker(voice)];
            }
            finally
            {
                Interlocked.Decrement(ref _currentConcurrency);
            }
        }

        public string[] GetAvailableVoices() => ["en-Carter_man", "en-Davis_man", "en-Emma_woman", "en-Grace_woman"];

        public void Dispose()
        {
        }
    }
}
