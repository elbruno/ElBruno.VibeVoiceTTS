# 🎙️ VibeVoiceTTS

[![NuGet](https://img.shields.io/nuget/v/ElBruno.VibeVoiceTTS.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ElBruno.VibeVoiceTTS.svg?style=flat-square&logo=nuget)](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS)
[![Build Status](https://github.com/elbruno/ElBruno.VibeVoiceTTS/actions/workflows/publish.yml/badge.svg)](https://github.com/elbruno/ElBruno.VibeVoiceTTS/actions/workflows/publish.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg?style=flat-square)](LICENSE)
[![HuggingFace](https://img.shields.io/badge/🤗_HuggingFace-ONNX_Models-orange?style=flat-square)](https://huggingface.co/elbruno/VibeVoice-Realtime-0.5B-ONNX)
[![GitHub stars](https://img.shields.io/github/stars/elbruno/ElBruno.VibeVoiceTTS?style=social)](https://github.com/elbruno/ElBruno.VibeVoiceTTS)
[![Twitter Follow](https://img.shields.io/twitter/follow/elbruno?style=social)](https://twitter.com/elbruno)

A .NET library for text-to-speech synthesis using Microsoft's [VibeVoice-Realtime-0.5B](https://huggingface.co/microsoft/VibeVoice-Realtime-0.5B) — native C# inference via ONNX Runtime, no Python required at runtime.

## Features

- 🔊 **Natural Text-to-Speech** — High-quality speech synthesis powered by VibeVoice-Realtime-0.5B
- 📦 **NuGet Package** — [`ElBruno.VibeVoiceTTS`](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS) — install and start generating speech in minutes
- 🤖 **Pure C# Inference** — ONNX Runtime, zero Python dependency at runtime
- 🚀 **GPU Acceleration** — DirectML (any Windows GPU) and CUDA (NVIDIA) support with automatic CPU fallback
- 📥 **Auto-Download** — Models automatically downloaded from 🤗 HuggingFace on first use
- 📡 **Streaming Chunks API** — ordered PCM chunk delivery through `GenerateAudioStreamingAsync()`
- 🌍 **6 Voice Presets** — Carter, Davis, Emma, Frank, Grace, Mike (English voices with multilingual experimental support)
- 💉 **Dependency Injection** — First-class `IServiceCollection` integration
- 🖥️ **Cross-Platform** — Windows, Linux, macOS, MAUI-ready

## Installation

```bash
dotnet add package ElBruno.VibeVoiceTTS
```

## Quick Start

### 1) Generate speech and save to WAV

```csharp
using ElBruno.VibeVoiceTTS;

using var tts = new VibeVoiceSynthesizer();
await tts.EnsureModelAvailableAsync(); // auto-downloads ~1.5 GB on first run

float[] audio = await tts.GenerateAudioAsync("Hello! Welcome to VibeVoiceTTS.", "Carter");
tts.SaveWav("output.wav", audio);
```

### 2) Use voice presets

```csharp
// Use the enum (recommended)
float[] carter = await tts.GenerateAudioAsync("Hello from Carter!", VibeVoicePreset.Carter);
float[] emma = await tts.GenerateAudioAsync("Hello from Emma!", VibeVoicePreset.Emma);

// Or use a string name — both short and internal names work
float[] audio = await tts.GenerateAudioAsync("Hello!", "Carter");
float[] audio2 = await tts.GenerateAudioAsync("Hello!", "en-Carter_man"); // also works
```

### 3) Discover available voices

```csharp
// Voices currently downloaded on disk
string[] available = tts.GetAvailableVoices();
// → ["Carter", "Emma"]  (default download includes Carter and Emma)

// All supported voices (including those not yet downloaded)
string[] supported = tts.GetSupportedVoices();
// → ["Carter", "Davis", "Emma", "Frank", "Grace", "Mike"]

// Detailed metadata for all supported voices
VoiceInfo[] details = tts.GetSupportedVoiceDetails();
foreach (var voice in details)
    Console.WriteLine($"{voice.Name} ({voice.Gender}, {voice.Language})");
```

> **💡 On-demand voice download:** Only Carter and Emma are downloaded by default with `EnsureModelAvailableAsync()`. Other voices (Davis, Frank, Grace, Mike) are **automatically downloaded on first use** when you call `GenerateAudioAsync()`. You can also pre-download a specific voice:
> ```csharp
> await tts.EnsureVoiceAvailableAsync("Davis", progress);
> ```

### 4) Track download progress

```csharp
var progress = new Progress<DownloadProgress>(p =>
{
    if (p.Stage == DownloadStage.Downloading)
        Console.Write($"\r⬇️ [{p.CurrentFile}] {p.PercentComplete:F0}%");
    else
        Console.WriteLine($"{p.Stage}: {p.Message}");
});

await tts.EnsureModelAvailableAsync(progress);
```

### 5) Configure options

```csharp
var options = new VibeVoiceOptions
{
    ModelPath = @"D:\models\vibevoice",  // Custom model location (default: OS cache)
    DiffusionSteps = 20,                 // Quality vs speed tradeoff
    CfgScale = 1.5f,                     // Classifier-free guidance scale
    SampleRate = 24000,                  // Output sample rate
    MaxTextLength = 500,                 // Max characters per request (0 disables the limit)
};

using var tts = new VibeVoiceSynthesizer(options);
```

### 6) Stream ordered PCM chunks

```csharp
await foreach (var chunk in tts.GenerateAudioStreamingAsync(
    "Streaming-ready chunk delivery without a second full-audio copy.",
    VibeVoicePreset.Carter))
{
    Console.WriteLine(
        $"Chunk #{chunk.SequenceNumber} | Samples: {chunk.Samples.Length} | Final: {chunk.IsFinal}");
}

Console.WriteLine(tts.StreamingCapabilities);
// -> { SupportsProgressiveGeneration = false, SupportsChunkedDelivery = true }
```

> **💡 Streaming behavior:** The current ONNX pipeline finishes waveform generation before chunk emission starts, so `SupportsProgressiveGeneration` is `false`. Chunks are then exposed in-order from the generated PCM buffer, so `SupportsChunkedDelivery` is `true`.

| Option | Default | Description |
|--------|---------|-------------|
| `ModelPath` | OS cache* | Directory where ONNX models are stored and downloaded |
| `HuggingFaceRepo` | `elbruno/VibeVoice-Realtime-0.5B-ONNX` | HuggingFace repo for model downloads |
| `DiffusionSteps` | `20` | Number of diffusion denoising steps |
| `CfgScale` | `1.5` | Classifier-free guidance scale |
| `SampleRate` | `24000` | Output audio sample rate (Hz) |
| `MaxTextLength` | `500` | Maximum characters accepted per request (`0` disables validation) |
| `Seed` | `42` | Random seed for reproducible output |
| `ExecutionProvider` | `Cpu` | ONNX Runtime execution provider (`Cpu`, `DirectML`, `Cuda`) |
| `GpuDeviceId` | `0` | GPU device index (used with DirectML or CUDA) |

*\*Default model cache: Windows: `%LOCALAPPDATA%\ElBruno\VibeVoice\models` · Linux/macOS: `~/.local/share/elbruno/vibevoice/models`*

### 7) GPU Acceleration

Enable GPU acceleration by setting the execution provider and installing the corresponding NuGet package:

```bash
# For DirectML (any Windows GPU — NVIDIA, AMD, Intel):
dotnet add package Microsoft.ML.OnnxRuntime.DirectML

# For CUDA (NVIDIA only — Windows and Linux):
dotnet add package Microsoft.ML.OnnxRuntime.Gpu
```

```csharp
// DirectML — recommended for Windows desktop apps
var options = new VibeVoiceOptions
{
    ExecutionProvider = ExecutionProvider.DirectML,
    GpuDeviceId = 0   // optional, selects which GPU
};
using var tts = new VibeVoiceSynthesizer(options);

// CUDA — for NVIDIA GPUs with CUDA drivers
var options = new VibeVoiceOptions
{
    ExecutionProvider = ExecutionProvider.Cuda,
    GpuDeviceId = 0
};
using var tts = new VibeVoiceSynthesizer(options);
```

> **💡 Note:** If the selected GPU provider is unavailable (missing NuGet package or no compatible GPU), the library automatically falls back to CPU inference. When using DirectML, models with dynamic tensor shapes (LM models, acoustic decoder) run on CPU while fixed-shape models (prediction head, connector, EOS classifier) use GPU — this works around known DirectML limitations with dynamic Reshape and ConvTranspose operations.

### 8) Dependency Injection

```csharp
builder.Services.AddVibeVoice(options =>
{
    options.DiffusionSteps = 20;
});

// Then inject IVibeVoiceSynthesizer in your services
```

### 9) Microsoft.Extensions.AI `ITextToSpeechClient`

```csharp
using ElBruno.VibeVoiceTTS.Extensions;
using Microsoft.Extensions.AI;

builder.Services.AddVibeVoice(
    configure: options =>
    {
        options.SampleRate = 24000;
    },
    configureTextToSpeech: options =>
    {
        options.DefaultAudioFormat = VibeVoiceTextToSpeechOptions.WavMediaType;
    });

var client = app.Services.GetRequiredService<ITextToSpeechClient>();

TextToSpeechResponse response = await client.GetAudioAsync(
    "Hello from Microsoft.Extensions.AI!",
    new TextToSpeechOptions
    {
        VoiceId = "Emma",
        AudioFormat = "audio/pcm;format=s16le"
    });

var audio = response.Contents.OfType<DataContent>().Single();
Console.WriteLine(audio.MediaType);
// -> audio/pcm;rate=24000;channels=1;format=s16le
```

Supported adapter output types:

- `audio/wav`
- `audio/pcm;rate=24000;channels=1;format=f32le`
- `audio/pcm;rate=24000;channels=1;format=s16le`

`GetService(typeof(TextToSpeechClientMetadata))` returns provider metadata, `TextToSpeechOptions.VoiceId` maps to the existing VibeVoice voice presets/internal names, and `AdditionalProperties["sampleRate"]` is validated against the synthesizer's configured sample rate.

### 10) Cancellation, metrics, streaming, and concurrent use

```csharp
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

tts.GenerationMetricReported += (_, metric) =>
{
    Console.WriteLine($"{metric.Stage}: {metric.Elapsed.TotalMilliseconds:F0} ms ({metric.FramesGenerated} frames)");
};

float[] audio = await tts.GenerateAudioAsync(
    "This request can be cancelled while queued or during generation.",
    "Carter",
    cts.Token);
```

> **💡 Concurrency behavior:** A single `VibeVoiceSynthesizer` instance serializes `GenerateAudioAsync()` and `EnsureVoiceAvailableAsync()` calls so the shared ONNX pipeline cannot be mutated mid-request. If a caller is waiting for that capacity, cancelling its `CancellationToken` aborts the wait. Create multiple synthesizer instances if you need true parallel generation.
>
> **💡 Metrics behavior:** `GenerationMetricReported` emits `FirstAudioReady` when the first latent audio frame is ready and `Completed` when the final waveform has been decoded.
>
> **💡 Streaming behavior:** `GenerateAudioStreamingAsync()` uses the same cancellation rules. If cancellation happens while enumerating chunks, no further audio chunks are emitted and exactly one yielded chunk is marked `IsFinal = true`.

> **💡 Tip:** For best results, keep sentences short (~10 words). Longer text may produce artifacts due to model limitations. Consider splitting long text into sentences.
> **💡 Tip:** The `MaxTextLength` option defaults to 500 characters. Raise it for long passages, or set it to `0` to disable the guard entirely.

## 🗣️ Voices & Languages

| Voice | Gender | Preset Enum | Internal Name |
|-------|--------|-------------|---------------|
| Carter | Male | `VibeVoicePreset.Carter` | `en-Carter_man` |
| Davis | Male | `VibeVoicePreset.Davis` | `en-Davis_man` |
| Emma | Female | `VibeVoicePreset.Emma` | `en-Emma_woman` |
| Frank | Male | `VibeVoicePreset.Frank` | `en-Frank_man` |
| Grace | Female | `VibeVoicePreset.Grace` | `en-Grace_woman` |
| Mike | Male | `VibeVoicePreset.Mike` | `en-Mike_man` |

All 6 voice presets are available on [HuggingFace](https://huggingface.co/elbruno/VibeVoice-Realtime-0.5B-ONNX/tree/main/voices) and are downloaded on-demand when first used.

> **⚡ Migration note:** In versions prior to 0.2.0, `GetAvailableVoices()` returned all 6 voices regardless of download status. Starting with 0.2.0, it returns only voices **actually downloaded on disk**. Use `GetSupportedVoices()` to see all 6 known presets. Voices are auto-downloaded on first use with `GenerateAudioAsync()`, or pre-download with `EnsureVoiceAvailableAsync("Davis")`.

**Language support:** The model is primarily trained on **English**, with experimental multilingual capabilities (e.g., Spanish, French, German). Results may vary for non-English text.

📖 For full details on the model, supported languages, and voice characteristics, see the official [VibeVoice documentation on HuggingFace](https://huggingface.co/microsoft/VibeVoice-Realtime-0.5B) and the [VibeVoice GitHub repository](https://github.com/microsoft/VibeVoice).

For the complete API reference and advanced usage, see the [Getting Started Guide](docs/GETTING_STARTED.md).

## Scenarios

This repository includes example projects demonstrating different ways to use VibeVoice:

| # | Status | Scenario | Stack | Level | Description |
|---|--------|----------|-------|-------|-------------|
| 1 | ✅ | [Simple Python Script](src/scenario-01-simple/) | Python | Beginner | Minimal TTS demo — useful for model export and testing |
| 2 | ✅ | [Full-Stack App](src/scenario-02-fullstack/) | Python + Blazor + Aspire | Intermediate | Web app with FastAPI backend and Blazor frontend |
| 3 | ✅ | [**C# Console App**](src/scenario-03-csharp-simple/) | **C# (.NET 8)** | **Beginner** | **Recommended starting point** — pure C# with `ElBruno.VibeVoiceTTS` |
| 4 | ✅ | [Full C# with Aspire](src/scenario-04-meai/) | C# + Blazor + Aspire | Intermediate | Full-stack C# app with WebAPI + Blazor frontend |
| 5 | ✅ | [Batch Processing](src/scenario-05-batch-processing/) | Python | Intermediate | CLI to convert folders of .txt to .wav |
| 6 | ✅ | [Real-Time Streaming](src/scenario-06-streaming-realtime/) | Python | Intermediate | Chunked audio playback for low-latency |
| 7 | ✅ | [MAUI Mobile](src/scenario-07-maui-mobile/) | C# (.NET 10 MAUI) | Advanced | Cross-platform app with in-process ONNX TTS via `ElBruno.VibeVoiceTTS` NuGet package |
| 8 | ✅ | [ONNX Export](src/scenario-08-onnx-native/) | Python → C# | Advanced | ONNX model export tools and pipeline docs |

> **Note:** Python scenarios (1, 2, 5, 6) are primarily for ONNX model export, testing, and reference. The C# scenarios (3, 4) run entirely in .NET with no Python dependency. See the [Scenarios Guide](docs/scenarios.md) for details.

## ONNX Models on HuggingFace

Pre-exported ONNX models are available on HuggingFace — the C# library downloads them automatically:

**🤗 [elbruno/VibeVoice-Realtime-0.5B-ONNX](https://huggingface.co/elbruno/VibeVoice-Realtime-0.5B-ONNX)**

The model includes 9 ONNX files (autoregressive pipeline with KV-cache) and 6 voice presets. See [Scenario 8](src/scenario-08-onnx-native/) for export details.

## Documentation

| Topic | Description |
|-------|-------------|
| [Getting Started](docs/GETTING_STARTED.md) | Prerequisites, setup, and first steps |
| [Scenarios Guide](docs/scenarios.md) | Detailed descriptions of all 8 scenarios |
| [Architecture](docs/ARCHITECTURE.md) | System design, ONNX pipeline, and data flow |
| [Project Structure](docs/project-structure.md) | Repository layout and file organization |
| [API Reference](docs/API_REFERENCE.md) | REST API documentation (for web scenarios) |
| [User Manual](docs/USER_MANUAL.md) | End-user guide for web interfaces |
| [Publishing](docs/publishing.md) | NuGet publishing with GitHub Actions |

## Tech Stack

| Layer | Technology | Purpose |
|-------|------------|---------|
| **C# TTS Library** | [ElBruno.VibeVoiceTTS](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS) | Reusable .NET library with HuggingFace auto-download |
| **TTS Model** | [VibeVoice-Realtime-0.5B](https://huggingface.co/microsoft/VibeVoice-Realtime-0.5B) | Microsoft's text-to-speech model |
| **Inference** | [ONNX Runtime](https://onnxruntime.ai/) | Native C# model inference |
| **Frontend** | [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) (.NET 10) | Interactive web UI |
| **Orchestration** | [.NET Aspire](https://learn.microsoft.com/en-us/dotnet/aspire/) | Service discovery & health checks |

## Building from Source

```bash
git clone https://github.com/elbruno/ElBruno.VibeVoiceTTS.git
cd ElBruno.VibeVoiceTTS
dotnet build src/ElBruno.VibeVoiceTTS/ElBruno.VibeVoiceTTS.csproj
dotnet test src/ElBruno.VibeVoiceTTS.Tests/ElBruno.VibeVoiceTTS.Tests.csproj
```

### Requirements

- .NET 8.0 SDK or later
- ONNX Runtime compatible platform (Windows, Linux, macOS)
- Python 3.11+ (only needed for ONNX model export — not for runtime use)

## 🔗 Related Projects

- <a href="https://github.com/elbruno/ElBruno.PersonaPlex">ElBruno.PersonaPlex</a> — C# wrapper for NVIDIA's PersonaPlex-7B-v1 full-duplex speech-to-speech model, using ONNX Runtime for local inference. Pre-exported ONNX models: <a href="https://huggingface.co/elbruno/personaplex-7b-v1-onnx">elbruno/personaplex-7b-v1-onnx</a>

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## 👋 About the Author

Hi! I'm **ElBruno** 🧡, a passionate developer and content creator exploring AI, .NET, and modern development practices.

**Made with ❤️ by [ElBruno](https://github.com/elbruno)**

If you like this project, consider following my work across platforms:

- 📻 **Podcast**: [No Tienen Nombre](https://notienenombre.com) — Spanish-language episodes on AI, development, and tech culture
- 💻 **Blog**: [ElBruno.com](https://elbruno.com) — Deep dives on embeddings, RAG, .NET, and local AI
- 📺 **YouTube**: [youtube.com/elbruno](https://www.youtube.com/elbruno) — Demos, tutorials, and live coding
- 🔗 **LinkedIn**: [@elbruno](https://www.linkedin.com/in/elbruno/) — Professional updates and insights
- 𝕏 **Twitter**: [@elbruno](https://www.x.com/elbruno/) — Quick tips, releases, and tech news