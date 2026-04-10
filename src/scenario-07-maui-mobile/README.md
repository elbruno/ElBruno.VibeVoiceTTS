# Scenario 7 — .NET MAUI Cross-Platform TTS App

A .NET MAUI application that provides a clean, modern UI for text-to-speech using **in-process ONNX inference** via the [`ElBruno.VibeVoiceTTS`](https://www.nuget.org/packages/ElBruno.VibeVoiceTTS) NuGet package. Works on **Windows, macOS, Android, and iOS** — no Python backend required.

## What This Shows

- Cross-platform native app with a single C# / XAML codebase
- In-process TTS inference using ONNX Runtime (no external server)
- Automatic model download from HuggingFace on first run
- 6 voice presets: Carter, Davis, Emma, Frank, Grace, Mike
- Audio playback on all platforms via `Plugin.Maui.Audio`
- Modern dark-themed UI with .NET MAUI controls

## Architecture

```
┌────────────────────────────────────────┐
│  .NET MAUI App                         │
│                                        │
│  ┌──────────────┐  ┌────────────────┐  │
│  │  UI Layer    │  │  VibeVoiceTTS  │  │
│  │  • Text input│──│  (ONNX)       │  │
│  │  • Voice pick│  │               │  │
│  │  • Playback  │  │  • In-process │  │
│  └──────────────┘  │  • Auto-DL    │  │
│                    │  • 6 voices   │  │
│                    └────────────────┘  │
└────────────────────────────────────────┘
```

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/) with MAUI workload
- ~1.5 GB disk space for ONNX models (auto-downloaded on first run)

### Install the MAUI workload

```bash
dotnet workload install maui
```

## How to Run

```bash
# Windows
dotnet build -t:Run -f net10.0-windows10.0.19041.0

# Android
dotnet build -t:Run -f net10.0-android

# macOS (Mac Catalyst)
dotnet build -t:Run -f net10.0-maccatalyst

# iOS (requires Mac with Xcode)
dotnet build -t:Run -f net10.0-ios
```

> **First run:** The app will download ONNX models (~1.5 GB) from HuggingFace automatically. A progress indicator shows download status. Subsequent runs start instantly from cache.

## Screenshots

<!-- TODO: Add screenshots after first build -->

| Windows | Android | macOS |
|---------|---------|-------|
| _coming soon_ | _coming soon_ | _coming soon_ |

## Key Files

| File | Purpose |
|------|---------|
| `MauiProgram.cs` | App setup, DI with `AddVibeVoice()` |
| `Services/VibeVoiceTtsService.cs` | Wraps `IVibeVoiceSynthesizer` for TTS + WAV conversion |
| `MainPage.xaml` | UI layout (text input, voice picker, playback) |
| `MainPage.xaml.cs` | Event handlers, model init, and audio playback logic |

## NuGet Packages

- **ElBruno.VibeVoiceTTS** — In-process ONNX TTS with auto-download from HuggingFace
- **CommunityToolkit.Maui** — Enhanced MAUI controls and helpers
- **Plugin.Maui.Audio** — Cross-platform audio playback
