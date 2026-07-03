using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace ElBruno.VibeVoiceTTS.Pipeline;

/// <summary>
/// Orchestrates the full VibeVoice TTS autoregressive pipeline using ONNX models.
/// Flow: text → LM (with KV-cache) → TTS-LM prefill (with voice preset KV-cache) →
///       autoregressive loop: diffusion → acoustic_connector → TTS-LM step → EOS check →
///       batch acoustic decode → audio
/// </summary>
internal sealed class OnnxInferencePipeline : IGenerationPipeline
{
    private readonly InferenceSession _lmWithKv;
    private readonly InferenceSession _ttsLmPrefill;
    private readonly InferenceSession _ttsLmStep;
    private readonly InferenceSession _predictionHead;
    private readonly InferenceSession _acousticDecoder;
    private readonly InferenceSession _acousticConnector;
    private readonly InferenceSession _eosClassifier;
    private readonly SessionOptions _sessionOptions;
    private readonly SessionOptions? _cpuSessionOptions; // For LM models when using DirectML (Reshape node incompatibility)
    private readonly float[] _typeEmbeddings; // [2, 896] flattened: speech=0..895, text=896..1791
    private readonly BpeTokenizer _tokenizer;
    private readonly VoicePresetLoader _voicePresets;
    private bool _disposed;

    public int DiffusionSteps { get; set; } = 20;
    public float CfgScale { get; set; } = 1.5f;
    public int Seed { get; set; } = 42;

    private const int LatentDim = 64;
    private const int HiddenSize = 896;
    private const int NumTtsLayers = 20;
    private const int NumLmLayers = 4;
    private const int NumKvHeads = 2;
    private const int HeadDim = 64;
    private const float SpeechScalingFactor = 0.2333984375f;
    private const float SpeechBiasFactor = -0.0703125f;
    private const int MaxFrames = 200;

    public OnnxInferencePipeline(string modelsDir, int diffusionSteps = 20, float cfgScale = 1.5f, int seed = 42,
        ExecutionProvider executionProvider = ExecutionProvider.Cpu, int gpuDeviceId = 0)
    {
        DiffusionSteps = diffusionSteps;
        CfgScale = cfgScale;
        Seed = seed;

        _sessionOptions = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        ConfigureExecutionProvider(_sessionOptions, executionProvider, gpuDeviceId);

        // DirectML has known incompatibilities with dynamic tensor shapes in:
        // - LM models: Reshape nodes with variable sequence lengths
        // - acoustic_decoder: ConvTranspose nodes with variable-length latent input
        // Use CPU for these models; GPU for fixed-shape models (prediction_head, acoustic_connector, eos_classifier).
        SessionOptions cpuOptions;
        if (executionProvider == ExecutionProvider.DirectML)
        {
            _cpuSessionOptions = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
            cpuOptions = _cpuSessionOptions;
        }
        else
        {
            cpuOptions = _sessionOptions;
        }

        _lmWithKv = new InferenceSession(Path.Combine(modelsDir, "lm_with_kv.onnx"), cpuOptions);
        _ttsLmPrefill = new InferenceSession(Path.Combine(modelsDir, "tts_lm_prefill.onnx"), cpuOptions);
        _ttsLmStep = new InferenceSession(Path.Combine(modelsDir, "tts_lm_step.onnx"), cpuOptions);
        _predictionHead = new InferenceSession(Path.Combine(modelsDir, "prediction_head.onnx"), _sessionOptions);
        _acousticDecoder = new InferenceSession(Path.Combine(modelsDir, "acoustic_decoder.onnx"), cpuOptions);
        _acousticConnector = new InferenceSession(Path.Combine(modelsDir, "acoustic_connector.onnx"), _sessionOptions);
        _eosClassifier = new InferenceSession(Path.Combine(modelsDir, "eos_classifier.onnx"), _sessionOptions);
        _typeEmbeddings = VoicePresetLoader.ReadNpyFile(Path.Combine(modelsDir, "type_embeddings.npy"));
        _tokenizer = new BpeTokenizer(Path.Combine(modelsDir, "tokenizer.json"));
        _voicePresets = new VoicePresetLoader(Path.Combine(modelsDir, "voices"));
    }

    /// <summary>
    /// Configures the SessionOptions with the requested execution provider.
    /// Falls back to CPU if the provider is unavailable (e.g., missing NuGet package or no compatible GPU).
    /// </summary>
    internal static void ConfigureExecutionProvider(SessionOptions options, ExecutionProvider provider, int deviceId)
    {
        if (provider == ExecutionProvider.Cpu)
            return;

        try
        {
            switch (provider)
            {
                case ExecutionProvider.DirectML:
                    options.AppendExecutionProvider_DML(deviceId);
                    break;
                case ExecutionProvider.Cuda:
                    options.AppendExecutionProvider_CUDA(deviceId);
                    break;
            }
        }
        catch (Exception)
        {
            // GPU provider not available — fall back to CPU silently.
            // This happens when the consumer hasn't installed the corresponding NuGet package
            // (Microsoft.ML.OnnxRuntime.DirectML or Microsoft.ML.OnnxRuntime.Gpu)
            // or when no compatible GPU hardware is present.
        }
    }

    public float[] GenerateAudio(
        string text,
        string voice,
        CancellationToken cancellationToken,
        Action<GenerationMetric>? reportMetric = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        cancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();

        // Load voice preset KV-cache
        var preset = _voicePresets.GetVoicePreset(voice);
        int ttsPromptLen = GetKvSeqLen(preset, "tts_kv_key_0");
        int lmPromptLen = GetKvSeqLen(preset, "lm_kv_key_0");

        // Tokenize
        int[] tokenIds = _tokenizer.Encode(text);
        int textLen = tokenIds.Length;
        cancellationToken.ThrowIfCancellationRequested();

        // Step 1: Language model with KV-cache
        float[] lmHidden = RunLmWithKv(tokenIds, preset, lmPromptLen, cancellationToken);

        // Step 2: Add text type embedding and run TTS-LM prefill
        float[] textTypeEmbed = GetTypeEmbedding(1); // text = type 1
        float[] speechTypeEmbed = GetTypeEmbedding(0); // speech = type 0
        float[] ttsInput = AddTypeEmbedding(lmHidden, textLen, textTypeEmbed);

        // Positive path: prefill with voice preset KV-cache
        var (posHidden, posKeys, posValues) = RunTtsLmPrefill(
            ttsInput, textLen, ttsPromptLen,
            StackKvArrays(preset, "tts_kv_key_", NumTtsLayers),
            StackKvArrays(preset, "tts_kv_value_", NumTtsLayers),
            cancellationToken);

        // Negative path: prefill with negative voice preset KV-cache
        int negPromptLen = GetKvSeqLen(preset, "neg_tts_kv_key_0");
        var (negHidden, negKeys, negValues) = RunTtsLmPrefill(
            ttsInput, textLen, negPromptLen,
            StackKvArrays(preset, "neg_tts_kv_key_", NumTtsLayers),
            StackKvArrays(preset, "neg_tts_kv_value_", NumTtsLayers),
            cancellationToken);

        // Step 3: Autoregressive speech generation
        var allLatents = new List<float[]>();
        int posTotalLen = ttsPromptLen + textLen;
        int negTotalLen = negPromptLen + textLen;

        for (int frame = 0; frame < MaxFrames; frame++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Extract condition from last hidden state
            float[] posCond = ExtractLastHidden(posHidden, textLen > 0 && frame == 0 ? textLen : 1);
            float[] negCond = ExtractLastHidden(negHidden, textLen > 0 && frame == 0 ? textLen : 1);

            // EOS check (skip frame 0)
            if (frame > 0)
            {
                float eosProb = RunEosClassifier(posCond, cancellationToken);
                if (eosProb > 0.5f) break;
            }

            // Diffusion with CFG
            float[] latent = RunDiffusion(posCond, negCond, frame, cancellationToken);
            allLatents.Add(latent);

            if (allLatents.Count == 1)
            {
                reportMetric?.Invoke(new GenerationMetric
                {
                    Stage = GenerationMetricStage.FirstAudioReady,
                    VoiceName = voice,
                    Elapsed = stopwatch.Elapsed,
                    FramesGenerated = allLatents.Count
                });
            }

            // Feedback: acoustic_connector → type embedding → TTS-LM step
            float[] acEmbed = RunAcousticConnector(latent, cancellationToken);
            float[] speechEmbed = new float[HiddenSize];
            for (int i = 0; i < HiddenSize; i++)
                speechEmbed[i] = acEmbed[i] + speechTypeEmbed[i];

            // Update positive path
            posTotalLen++;
            (posHidden, posKeys, posValues) = RunTtsLmStep(
                speechEmbed, posTotalLen, posKeys, posValues, cancellationToken);

            // Update negative path
            negTotalLen++;
            (negHidden, negKeys, negValues) = RunTtsLmStep(
                speechEmbed, negTotalLen, negKeys, negValues, cancellationToken);

            // After first frame, textLen is consumed
            textLen = 0;
        }

        // Decode all latents
        cancellationToken.ThrowIfCancellationRequested();
        float[] audio = DecodeLatents(allLatents, cancellationToken);
        reportMetric?.Invoke(new GenerationMetric
        {
            Stage = GenerationMetricStage.Completed,
            VoiceName = voice,
            Elapsed = stopwatch.Elapsed,
            FramesGenerated = allLatents.Count
        });
        return audio;
    }

    public string[] GetAvailableVoices() => _voicePresets.GetAvailableVoices();

    private float[] RunLmWithKv(
        int[] tokenIds,
        Dictionary<string, float[]> preset,
        int lmPromptLen,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int textLen = tokenIds.Length;
        int totalLen = lmPromptLen + textLen;

        var inputIds = new long[textLen];
        for (int i = 0; i < textLen; i++) inputIds[i] = tokenIds[i];

        var mask = new long[totalLen];
        Array.Fill(mask, 1L);

        var pastKeys = StackKvArrays(preset, "lm_kv_key_", NumLmLayers);
        var pastValues = StackKvArrays(preset, "lm_kv_value_", NumLmLayers);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("input_ids",
                Utils.TensorHelpers.CreateTensor(inputIds, [1, textLen])),
            NamedOnnxValue.CreateFromTensor("attention_mask",
                Utils.TensorHelpers.CreateTensor(mask, [1, totalLen])),
            NamedOnnxValue.CreateFromTensor("past_keys",
                CreateKvTensor(pastKeys, NumLmLayers, lmPromptLen)),
            NamedOnnxValue.CreateFromTensor("past_values",
                CreateKvTensor(pastValues, NumLmLayers, lmPromptLen)),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _lmWithKv.Run(inputs);
        var resultList = results.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        return resultList[0].AsTensor<float>().ToArray(); // [1, textLen, 896]
    }

    private (float[] hidden, float[] keys, float[] values) RunTtsLmPrefill(
        float[] inputEmbeds, int seqLen, int promptLen,
        float[] pastKeys, float[] pastValues,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int totalLen = promptLen + seqLen;
        var mask = new long[totalLen];
        Array.Fill(mask, 1L);

        var posIds = new long[seqLen];
        for (int i = 0; i < seqLen; i++) posIds[i] = promptLen + i;

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("inputs_embeds",
                Utils.TensorHelpers.CreateTensor(inputEmbeds, [1, seqLen, HiddenSize])),
            NamedOnnxValue.CreateFromTensor("attention_mask",
                Utils.TensorHelpers.CreateTensor(mask, [1, totalLen])),
            NamedOnnxValue.CreateFromTensor("position_ids",
                Utils.TensorHelpers.CreateTensor(posIds, [1, seqLen])),
            NamedOnnxValue.CreateFromTensor("past_keys",
                CreateKvTensor(pastKeys, NumTtsLayers, promptLen)),
            NamedOnnxValue.CreateFromTensor("past_values",
                CreateKvTensor(pastValues, NumTtsLayers, promptLen)),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _ttsLmPrefill.Run(inputs);
        var resultList = results.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        var hidden = resultList[0].AsTensor<float>().ToArray();
        var newKeys = resultList[1].AsTensor<float>().ToArray();
        var newValues = resultList[2].AsTensor<float>().ToArray();
        return (hidden, newKeys, newValues);
    }

    private (float[] hidden, float[] keys, float[] values) RunTtsLmStep(
        float[] embedVec, int totalLen, float[] pastKeys, float[] pastValues, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int pastSeqLen = totalLen - 1;
        var mask = new long[totalLen];
        Array.Fill(mask, 1L);

        var inputEmbeds = new float[HiddenSize];
        Array.Copy(embedVec, inputEmbeds, HiddenSize);

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("inputs_embeds",
                Utils.TensorHelpers.CreateTensor(inputEmbeds, [1, 1, HiddenSize])),
            NamedOnnxValue.CreateFromTensor("attention_mask",
                Utils.TensorHelpers.CreateTensor(mask, [1, totalLen])),
            NamedOnnxValue.CreateFromTensor("position_ids",
                Utils.TensorHelpers.CreateTensor(new long[] { pastSeqLen }, [1, 1])),
            NamedOnnxValue.CreateFromTensor("past_keys",
                CreateKvTensorFromFlat(pastKeys, NumTtsLayers, pastSeqLen)),
            NamedOnnxValue.CreateFromTensor("past_values",
                CreateKvTensorFromFlat(pastValues, NumTtsLayers, pastSeqLen)),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _ttsLmStep.Run(inputs);
        var resultList = results.ToList();
        cancellationToken.ThrowIfCancellationRequested();
        var hidden = resultList[0].AsTensor<float>().ToArray();
        var newKeys = resultList[1].AsTensor<float>().ToArray();
        var newValues = resultList[2].AsTensor<float>().ToArray();
        return (hidden, newKeys, newValues);
    }

    private float[] RunDiffusion(float[] posCond, float[] negCond, int frame, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var scheduler = new DiffusionScheduler(DiffusionSteps);
        int[] timesteps = scheduler.GetTimesteps();

        // Initialize noise - batch of 2 (positive + negative)
        float[] posNoise = Utils.TensorHelpers.Randn([LatentDim], Seed >= 0 ? Seed + frame : Random.Shared.Next());
        float[] speech = (float[])posNoise.Clone();

        for (int i = 0; i < timesteps.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Run prediction head for both positive and negative conditions
            float[] condPred = RunPredictionHead(speech, posCond, timesteps[i], cancellationToken);
            float[] uncondPred = RunPredictionHead(speech, negCond, timesteps[i], cancellationToken);

            // CFG: uncond + scale * (cond - uncond)
            float[] guidedPred = new float[LatentDim];
            for (int j = 0; j < LatentDim; j++)
                guidedPred[j] = uncondPred[j] + CfgScale * (condPred[j] - uncondPred[j]);

            speech = scheduler.Step(guidedPred, timesteps[i], speech);
        }

        return speech;
    }

    private float[] RunPredictionHead(float[] latent, float[] condition, int timestep, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("noisy_latent",
                Utils.TensorHelpers.CreateTensor(latent, [1, LatentDim])),
            NamedOnnxValue.CreateFromTensor("timestep",
                Utils.TensorHelpers.CreateTensor(new long[] { timestep }, [1])),
            NamedOnnxValue.CreateFromTensor("conditioning",
                Utils.TensorHelpers.CreateTensor(condition, [1, HiddenSize])),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _predictionHead.Run(inputs);
        return results.First().AsTensor<float>().ToArray();
    }

    private float[] RunAcousticConnector(float[] latent, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("speech_latent",
                Utils.TensorHelpers.CreateTensor(latent, [1, LatentDim])),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _acousticConnector.Run(inputs);
        return results.First().AsTensor<float>().ToArray();
    }

    private float RunEosClassifier(float[] hiddenState, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("hidden_state",
                Utils.TensorHelpers.CreateTensor(hiddenState, [1, HiddenSize])),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _eosClassifier.Run(inputs);
        float logit = results.First().AsTensor<float>().ToArray()[0];
        return 1.0f / (1.0f + MathF.Exp(-logit)); // sigmoid
    }

    private float[] DecodeLatents(List<float[]> allLatents, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        int numFrames = allLatents.Count;
        // Scale latents: (latent - bias) / scaling
        // Layout for decoder: [1, 64, numFrames] (transposed)
        float[] scaled = new float[LatentDim * numFrames];
        for (int f = 0; f < numFrames; f++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (int d = 0; d < LatentDim; d++)
            {
                float val = (allLatents[f][d] - SpeechBiasFactor) / SpeechScalingFactor;
                scaled[d * numFrames + f] = val;
            }
        }

        var inputs = new List<NamedOnnxValue>
        {
            NamedOnnxValue.CreateFromTensor("latent",
                Utils.TensorHelpers.CreateTensor(scaled, [1, LatentDim, numFrames])),
        };

        cancellationToken.ThrowIfCancellationRequested();
        using var results = _acousticDecoder.Run(inputs);
        float[] audio = results.First().AsTensor<float>().ToArray();

        for (int i = 0; i < audio.Length; i++)
        {
            audio[i] = Math.Clamp(audio[i], -1.0f, 1.0f);
            if ((i & 1023) == 0)
                cancellationToken.ThrowIfCancellationRequested();
        }

        return audio;
    }

    // Helper: Get type embedding (0=speech, 1=text)
    private float[] GetTypeEmbedding(int typeIndex)
    {
        float[] embed = new float[HiddenSize];
        Array.Copy(_typeEmbeddings, typeIndex * HiddenSize, embed, 0, HiddenSize);
        return embed;
    }

    // Helper: Add type embedding to LM hidden states [1, seqLen, 896]
    private static float[] AddTypeEmbedding(float[] hidden, int seqLen, float[] typeEmbed)
    {
        float[] result = new float[seqLen * HiddenSize];
        for (int s = 0; s < seqLen; s++)
        {
            int offset = s * HiddenSize;
            for (int i = 0; i < HiddenSize; i++)
                result[offset + i] = hidden[offset + i] + typeEmbed[i];
        }
        return result;
    }

    // Helper: Extract last hidden state vector from output
    private static float[] ExtractLastHidden(float[] hidden, int seqLen)
    {
        float[] result = new float[HiddenSize];
        int offset = (seqLen - 1) * HiddenSize;
        Array.Copy(hidden, offset, result, 0, HiddenSize);
        return result;
    }

    // Helper: Get KV sequence length from preset array
    private static int GetKvSeqLen(Dictionary<string, float[]> preset, string keyName)
    {
        // KV arrays are [1, num_kv_heads, seq_len, head_dim] = 1*2*seq*64
        float[] data = preset[keyName];
        return data.Length / (NumKvHeads * HeadDim);
    }

    // Helper: Stack KV arrays from preset into flat array [numLayers * 1 * numKvHeads * seqLen * headDim]
    private static float[] StackKvArrays(Dictionary<string, float[]> preset, string prefix, int numLayers)
    {
        int layerSize = preset[prefix + "0"].Length;
        float[] stacked = new float[numLayers * layerSize];
        for (int i = 0; i < numLayers; i++)
            Array.Copy(preset[prefix + i], 0, stacked, i * layerSize, layerSize);
        return stacked;
    }

    // Helper: Create KV tensor [numLayers, 1, numKvHeads, seqLen, headDim] from flat preset data
    private static DenseTensor<float> CreateKvTensor(float[] data, int numLayers, int seqLen)
    {
        return Utils.TensorHelpers.CreateTensor(data, [numLayers, 1, NumKvHeads, seqLen, HeadDim]);
    }

    // Helper: Create KV tensor from flat ONNX output (already in correct layout)
    private static DenseTensor<float> CreateKvTensorFromFlat(float[] data, int numLayers, int seqLen)
    {
        return Utils.TensorHelpers.CreateTensor(data, [numLayers, 1, NumKvHeads, seqLen, HeadDim]);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lmWithKv.Dispose();
        _ttsLmPrefill.Dispose();
        _ttsLmStep.Dispose();
        _predictionHead.Dispose();
        _acousticDecoder.Dispose();
        _acousticConnector.Dispose();
        _eosClassifier.Dispose();
        _sessionOptions.Dispose();
        _cpuSessionOptions?.Dispose();
    }
}
