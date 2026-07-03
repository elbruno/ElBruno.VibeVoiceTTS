using ElBruno.VibeVoiceTTS.Extensions;
using ElBruno.VibeVoiceTTS.Pipeline;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElBruno.VibeVoiceTTS.Tests;

public class VibeVoiceTextToSpeechClientTests
{
    [Fact]
    public async Task GetAudioAsync_DefaultsToWavAndReturnsResolvedVoiceMetadata()
    {
        using var synth = CreateSynthesizer(new[] { 0f, 0.5f, -0.5f });
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions()));

        TextToSpeechResponse response = await client.GetAudioAsync("hello world");

        Assert.Equal(synth.HuggingFaceRepo, response.ModelId);
        Assert.NotNull(response.ResponseId);

        var content = Assert.IsType<DataContent>(Assert.Single(response.Contents));
        Assert.Equal("audio/wav", content.MediaType);
        Assert.Equal("speech.wav", content.Name);
        Assert.StartsWith("RIFF", System.Text.Encoding.ASCII.GetString(content.Data.Span[..4]));

        Assert.NotNull(response.AdditionalProperties);
        Assert.Equal("en-Carter_man", response.AdditionalProperties[VibeVoiceTextToSpeechClient.VoiceIdPropertyName]);
        Assert.Equal(24_000, response.AdditionalProperties[VibeVoiceTextToSpeechClient.SampleRatePropertyName]);
    }

    [Theory]
    [InlineData("s16le", "audio/pcm;rate=24000;channels=1;format=s16le", 4)]
    [InlineData("audio/pcm;format=f32le", "audio/pcm;rate=24000;channels=1;format=f32le", 8)]
    public async Task GetAudioAsync_ReturnsCanonicalPcmPayload(string requestedFormat, string expectedMediaType, int expectedLength)
    {
        using var synth = CreateSynthesizer(new[] { -1f, 1f });
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions()));

        TextToSpeechResponse response = await client.GetAudioAsync(
            "hello world",
            new TextToSpeechOptions { AudioFormat = requestedFormat, VoiceId = "Emma" });

        var content = Assert.IsType<DataContent>(Assert.Single(response.Contents));
        Assert.Equal(expectedMediaType, content.MediaType);
        Assert.Equal(expectedLength, content.Data.Length);
        Assert.Equal("en-Emma_woman", response.AdditionalProperties![VibeVoiceTextToSpeechClient.VoiceIdPropertyName]);
    }

    [Fact]
    public async Task GetAudioAsync_UsesConfiguredMetadataOverride()
    {
        using var synth = CreateSynthesizer([0.25f]);
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions
            {
                ProviderName = "custom-vibevoice",
                DefaultModelId = "custom-model-id"
            }));

        TextToSpeechClientMetadata metadata = Assert.IsType<TextToSpeechClientMetadata>(
            client.GetService(typeof(TextToSpeechClientMetadata)));
        TextToSpeechResponse response = await client.GetAudioAsync("metadata");

        Assert.Equal("custom-vibevoice", metadata.ProviderName);
        Assert.Equal("custom-model-id", metadata.DefaultModelId);
        Assert.Equal("custom-model-id", response.ModelId);
    }

    [Fact]
    public async Task GetAudioAsync_CancelledWhileWaiting_Throws()
    {
        var firstStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var synth = CreateSynthesizer(new BlockingPipeline(firstStarted, releaseFirst));
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions()));

        Task<TextToSpeechResponse> first = client.GetAudioAsync("first");
        await firstStarted.Task;

        using var cts = new CancellationTokenSource();
        Task<TextToSpeechResponse> second = client.GetAudioAsync("second", cancellationToken: cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => second);

        releaseFirst.SetResult();
        await first;
    }

    [Fact]
    public async Task GetStreamingAudioAsync_ForPcm_ReturnsOrderedUpdates()
    {
        float[] audio = Enumerable.Range(0, (VibeVoiceSynthesizer.DefaultStreamingChunkSizeSamples * 2) + 5)
            .Select(i => i / 100f)
            .ToArray();
        using var synth = CreateSynthesizer(new BufferedPipeline(audio));
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions
            {
                DefaultAudioFormat = "audio/pcm;format=s16le"
            }));
        var updates = new List<TextToSpeechResponseUpdate>();

        await foreach (TextToSpeechResponseUpdate update in client.GetStreamingAudioAsync("stream"))
        {
            updates.Add(update);
        }

        Assert.Equal(3, updates.Count);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdating, updates[0].Kind);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdating, updates[1].Kind);
        Assert.Equal(TextToSpeechResponseUpdateKind.AudioUpdated, updates[2].Kind);
        Assert.All(updates, update =>
        {
            var content = Assert.IsType<DataContent>(Assert.Single(update.Contents));
            Assert.Equal("audio/pcm;rate=24000;channels=1;format=s16le", content.MediaType);
        });
        Assert.Equal(0L, updates[0].AdditionalProperties!["sequenceNumber"]);
        Assert.Equal(2L, updates[2].AdditionalProperties!["sequenceNumber"]);
    }

    [Fact]
    public async Task GetAudioAsync_RejectsMismatchedSampleRateRequest()
    {
        using var synth = CreateSynthesizer([0.1f]);
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions()));
        var options = new TextToSpeechOptions
        {
            AdditionalProperties = new AdditionalPropertiesDictionary
            {
                [VibeVoiceTextToSpeechClient.SampleRatePropertyName] = 16_000
            }
        };

        var exception = await Assert.ThrowsAsync<NotSupportedException>(() => client.GetAudioAsync("rate", options));

        Assert.Contains("24000", exception.Message);
    }

    [Fact]
    public void GetService_ReturnsWrappedServices()
    {
        using var synth = CreateSynthesizer([0.25f]);
        using var client = new VibeVoiceTextToSpeechClient(
            synth,
            Options.Create(new VibeVoiceTextToSpeechOptions()));

        Assert.Same(client, client.GetService(typeof(ITextToSpeechClient)));
        Assert.Same(synth, client.GetService(typeof(VibeVoiceSynthesizer)));
        Assert.Same(synth, client.GetService(typeof(IVibeVoiceSynthesizer)));
        Assert.NotNull(client.GetService(typeof(TextToSpeechClientMetadata)));
        Assert.Null(client.GetService(typeof(TextToSpeechClientMetadata), serviceKey: "unsupported"));
    }

    [Fact]
    public void AddVibeVoice_RegistersITextToSpeechClient()
    {
        var services = new ServiceCollection();
        services.AddVibeVoice(
            configure: options => options.SampleRate = 16_000,
            configureTextToSpeech: options =>
            {
                options.ProviderName = "test-provider";
                options.DefaultAudioFormat = "audio/pcm;format=f32le";
            });

        using ServiceProvider provider = services.BuildServiceProvider();

        var client = Assert.IsType<VibeVoiceTextToSpeechClient>(provider.GetRequiredService<ITextToSpeechClient>());
        var synth = provider.GetRequiredService<VibeVoiceSynthesizer>();
        var metadata = Assert.IsType<TextToSpeechClientMetadata>(client.GetService(typeof(TextToSpeechClientMetadata)));

        Assert.Same(synth, client.GetService(typeof(VibeVoiceSynthesizer)));
        Assert.Equal("test-provider", metadata.ProviderName);
        Assert.Equal(16_000, synth.SampleRate);
    }

    private static VibeVoiceSynthesizer CreateSynthesizer(float[] audio, int sampleRate = 24_000)
    {
        return CreateSynthesizer(new BufferedPipeline(audio), sampleRate);
    }

    private static VibeVoiceSynthesizer CreateSynthesizer(IGenerationPipeline pipeline, int sampleRate = 24_000)
    {
        return new VibeVoiceSynthesizer(
            new VibeVoiceOptions
            {
                ModelPath = VibeVoiceOptions.GetDefaultModelPath(),
                SampleRate = sampleRate
            },
            new VibeVoiceRuntimeDependencies
            {
                IsModelAvailable = static _ => true,
                IsVoiceAvailable = static (_, _) => true,
                EnsureModelAvailableAsync = static (_, _, _, _) => Task.CompletedTask,
                EnsureVoiceAvailableAsync = static (_, _, _, _, _) => Task.CompletedTask,
                CreatePipeline = (_, _) => pipeline
            });
    }

    private sealed class BufferedPipeline(float[] audio) : IGenerationPipeline
    {
        public float[] GenerateAudio(string text, string voice, CancellationToken cancellationToken, Action<GenerationMetric>? reportMetric = null)
            => audio;

        public string[] GetAvailableVoices() => ["en-Carter_man", "en-Emma_woman"];

        public void Dispose()
        {
        }
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
}
