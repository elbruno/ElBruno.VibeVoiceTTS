# 📚 Scenarios Guide

This document provides a comprehensive overview of all 8 scenarios in the VibeVoiceTTS project. Each scenario demonstrates a specific use case or technology stack for text-to-speech synthesis with VibeVoice-Realtime-0.5B.

## Quick Comparison Table

| # | Stack | Language | Difficulty | Key Feature | Status |
|---|-------|----------|------------|-------------|--------|
| 1 | Python | Python | Beginner | Minimal demo — easiest entry point | ✅ |
| 2 | Python + Blazor + Aspire | Python/C# | Intermediate | Full-stack web with FastAPI backend | ✅ |
| 3 | .NET 8 | C# | Beginner | Pure C# with NuGet — **recommended for C# devs** | ✅ |
| 4 | C# + Blazor + Aspire | C# | Intermediate | Full C# stack — zero Python at runtime | ✅ |
| 5 | Python | Python | Intermediate | Batch file processing with CLI | ✅ |
| 6 | Python | Python | Intermediate | Real-time streaming (~300ms to first chunk) | ✅ |
| 7 | .NET MAUI | C# | Advanced | Cross-platform mobile/desktop (WIP) | 🚧 |
| 8 | Python → C# (ONNX) | Python/C# | Advanced | Export tools + native ONNX pipeline | ✅ |

---

## When to Use Each Scenario

### **Choose Scenario 1** if you want to...
- Get up and running **immediately** with minimal setup
- Learn the VibeVoice API in Python
- Test the model on your system before building something complex
- Experiment with different voices and settings

### **Choose Scenario 2** if you want to...
- Build a **web application** with a frontend UI
- Use Python for the TTS backend (more flexible for prototyping)
- Learn Aspire orchestration and FastAPI integration

### **Choose Scenario 3** if you want to...
- Use **pure C# and .NET** from the start
- Avoid Python altogether
- Deploy with zero Python dependency
- Get started with the **NuGet package** (recommended for C# developers)

### **Choose Scenario 4** if you want to...
- Build a **full-stack C# application**
- Use Blazor + WebAPI for the frontend and backend
- Deploy a production-ready app with zero Python dependency
- Leverage .NET Aspire for service orchestration

### **Choose Scenario 5** if you want to...
- **Batch-process large numbers of text files** to audio
- Use YAML front-matter for per-file voice overrides
- Parallelize file processing for speed

### **Choose Scenario 6** if you want to...
- Demonstrate **real-time, low-latency streaming**
- Stream audio playback while generation is happening
- Optimize for performance metrics (time-to-first-chunk, real-time factor)

### **Choose Scenario 7** if you want to...
- Build a **cross-platform mobile/desktop application**
- Target Windows, Android, iOS, and macOS from one codebase
- Use .NET MAUI with a thin HTTP client calling a backend

### **Choose Scenario 8** if you want to...
- **Export VibeVoice to ONNX format**
- Understand the model architecture and pipeline in detail
- Use the export tools for custom inference optimization
- Reference the complete native C# pipeline with KV-cache

---

## Scenario Details

### Scenario 1: Simple Python Script

**Directory:** `src/scenario-01-simple/`

#### What It Demonstrates
- Minimal Python setup for VibeVoice TTS
- Basic audio generation and saving
- Multilingual support (10 languages)
- How to select different voice presets

#### Prerequisites
- Python 3.10+
- GPU recommended (CUDA), but CPU works too
- Shared Python environment at repo root

#### Key Technologies
- **vibevoice_realtime** — VibeVoice PyTorch model
- **soundfile** — WAV file handling
- **numpy** — Audio processing

#### How to Run
```bash
# Activate shared environment
.\.venv\Scripts\Activate.ps1  # Windows
source .venv/bin/activate      # Linux/macOS

# From repo root
cd src/scenario-01-simple
python main.py                 # English demo
python main_multilingual.py    # Multilingual demo
```

See [scenario-01-simple/README.md](../src/scenario-01-simple/README.md) for full details and customization options.

---

### Scenario 2: Full-Stack Web App (Python + Blazor + Aspire)

**Directory:** `src/scenario-02-fullstack/`

#### What It Demonstrates
- Production-ready full-stack application
- .NET Aspire orchestration and service discovery
- FastAPI Python backend with OpenAI integration
- Blazor frontend with real-time UI updates
- Backend readiness system with error recovery

#### Prerequisites
- .NET 10 SDK with Aspire workload
- Python 3.11+
- GPU recommended for inference

#### Key Technologies
- **.NET Aspire** — Service orchestration and discovery
- **Blazor Server** — Interactive C# frontend
- **FastAPI** — High-performance Python backend
- **VibeVoice TTS** — Speech synthesis
- **OpenAI API** (optional) — Conversation logic

#### How to Run
```bash
cd src/scenario-02-fullstack/VoiceLabs.AppHost
dotnet run
# Open Aspire dashboard → click frontend endpoint
```

See [scenario-02-fullstack](../src/scenario-02-fullstack/) for full details.

---

### Scenario 3: C# Console App (Simple)

**Directory:** `src/scenario-03-csharp-simple/`

#### What It Demonstrates
- Simplest C# entry point using the NuGet library
- Auto-download of ONNX models from HuggingFace
- Pure native C# inference via ONNX Runtime
- **Recommended starting point for C# developers**

#### Prerequisites
- .NET 8.0+ SDK
- Internet connection (for first-time model download ~700 MB)

#### Key Technologies
- **ElBruno.VibeVoiceTTS** — NuGet library
- **ONNX Runtime** — Native inference engine
- **No Python required**

#### How to Run
```bash
cd src/scenario-03-csharp-simple
dotnet run
# output.wav is created in current directory
```

See [scenario-03-csharp-simple/README.md](../src/scenario-03-csharp-simple/README.md) for full details.

---

### Scenario 4: Full C# Stack with Aspire

**Directory:** `src/scenario-04-meai/`

#### What It Demonstrates
- Complete production C# stack from frontend to backend
- Aspire orchestration with service discovery
- C# WebAPI backend using ElBruno.VibeVoiceTTS
- Blazor frontend with audio playback
- **Zero Python dependency at runtime**

#### Prerequisites
- .NET 10 SDK with Aspire workload
- Internet connection (for model auto-download ~2.3 GB on first run)

#### Key Technologies
- **.NET Aspire** — Service orchestration
- **Blazor Server** — Web frontend
- **C# WebAPI** — Backend API
- **ElBruno.VibeVoiceTTS** — Native TTS library
- **ONNX Runtime** — Inference engine

#### How to Run
```bash
cd src/scenario-04-meai/VoiceLabs.ConversationHost
dotnet run
# Open Aspire dashboard → click frontend endpoint
```

See [scenario-04-meai/README.md](../src/scenario-04-meai/README.md) for API endpoints and full details.

---

### Scenario 5: Batch Processing

**Directory:** `src/scenario-05-batch-processing/`

#### What It Demonstrates
- Bulk text-to-audio conversion from a directory
- YAML front-matter for per-file voice overrides
- Optional parallel processing
- Progress tracking and summary reporting

#### Prerequisites
- Python 3.11+
- GPU recommended
- Shared Python environment at repo root

#### Key Technologies
- **vibevoice_realtime** — TTS model
- **pyyaml** — YAML front-matter parsing
- **concurrent.futures** — Parallel processing
- **tqdm** — Progress bars

#### How to Run
```bash
# Activate shared environment
.\.venv\Scripts\Activate.ps1  # Windows
source .venv/bin/activate      # Linux/macOS

cd src/scenario-05-batch-processing
python batch_tts.py [--voice {voice}] [--parallel {n}]
```

See [scenario-05-batch-processing/README.md](../src/scenario-05-batch-processing/README.md) for options and YAML syntax.

---

### Scenario 6: Real-Time Streaming

**Directory:** `src/scenario-06-streaming-realtime/`

#### What It Demonstrates
- Streaming TTS with audio playback as generation happens
- **~300 ms time-to-first-chunk** latency
- Real-time performance metrics
- Fallback to file-only mode if audio device unavailable

#### Prerequisites
- Python 3.11+
- GPU recommended (CUDA-capable) for consistent streaming
- Audio output device (speakers/headphones)
- Shared Python environment at repo root

#### Key Technologies
- **vibevoice_realtime** — Streaming API
- **sounddevice** — Real-time audio playback
- **numpy** — Audio processing
- **tqdm** — Progress visualization

#### How to Run
```bash
# Activate shared environment
.\.venv\Scripts\Activate.ps1  # Windows
source .venv/bin/activate      # Linux/macOS

cd src/scenario-06-streaming-realtime
python stream_tts.py
```

See [scenario-06-streaming-realtime/README.md](../src/scenario-06-streaming-realtime/README.md) for metrics and troubleshooting.

---

### Scenario 7: MAUI Cross-Platform App

**Directory:** `src/scenario-07-maui-mobile/`

#### What It Demonstrates
- Single C# codebase for Windows, Android, iOS, and macOS
- .NET MAUI UI framework
- HTTP client calling Python or C# TTS backend
- Audio playback via Plugin.Maui.Audio
- **Work in progress — some platforms may not be fully tested**

#### Prerequisites
- .NET 10 SDK with MAUI workload
- Platform-specific requirements (Xcode for iOS, Android SDK for Android)
- Running TTS backend (Scenario 2 or 4)

#### Key Technologies
- **.NET MAUI** — Cross-platform UI framework
- **HttpClient** — Backend communication
- **Plugin.Maui.Audio** — Cross-platform audio playback
- **CommunityToolkit.Maui** — Enhanced controls

#### How to Run
```bash
# Start backend first (Scenario 2 or 4)
cd src/scenario-02-fullstack/backend
uvicorn main:app --port 5100

# In another terminal, run MAUI app
cd src/scenario-07-maui-mobile
dotnet build -t:Run -f net10.0-windows10.0.19041.0  # Windows
dotnet build -t:Run -f net10.0-android              # Android
dotnet build -t:Run -f net10.0-ios                  # iOS (Mac only)
```

See [scenario-07-maui-mobile/README.md](../src/scenario-07-maui-mobile/README.md) for full details.

---

### Scenario 8: ONNX Export & Native Pipeline

**Directory:** `src/scenario-08-onnx-native/`

#### What It Demonstrates
- Complete ONNX model export from PyTorch
- Voice preset KV-cache generation
- Native C# inference pipeline with autoregressive loop
- Architecture deep-dive with 8 ONNX models and KV-cache conditioning
- **Production-ready export tools**

#### Prerequisites
- Python 3.10+ (for export only)
- .NET 8.0+ (for C# inference)
- GPU recommended for export step

#### Key Technologies
- **torch2onnx** — PyTorch to ONNX conversion
- **ONNX Runtime** — Inference engine
- **KV-cache models** — Autoregressive conditioning
- **onnxruntime-tools** — Model optimization

#### ONNX Model Pipeline
```
Text → lm_with_kv.onnx
     → tts_lm_prefill.onnx
     → [Autoregressive loop with 7 models]
     → acoustic_decoder.onnx
     → WAV audio
```

#### How to Run

**Option A: Download Pre-Exported Models** (Easiest)
```bash
# Models auto-download from HuggingFace on first use
cd src/scenario-03-csharp-simple
dotnet run  # or any C# scenario
```

**Option B: Export Models Yourself**
```bash
cd src/scenario-08-onnx-native/export
pip install -r requirements_export.txt

python export_model.py --output ../models
python export_voice_presets.py --output ../models/voices
python validate_export.py --models-dir ../models
```

See [scenario-08-onnx-native/README.md](../src/scenario-08-onnx-native/README.md) for technical deep-dive and upload instructions.

---

## Python vs C# Scenarios

### When to Use Python
- **Experimentation & prototyping** — fastest iteration (Scenarios 1, 5, 6)
- **ONNX export & model tools** — Python has better ML tooling (Scenario 8 export step)
- **FastAPI backend** — flexible for REST APIs and OpenAI integration (Scenario 2)
- **Batch processing** — simple CLI scripts (Scenario 5)

### When to Use C#
- **Production deployment** — better performance and cross-platform support
- **Native runtime** — zero Python dependency (Scenarios 3, 4, 8 C# usage)
- **Mobile apps** — MAUI support (Scenario 7)
- **Enterprise environments** — better DevOps and monitoring integration

---

## Voice Presets

All scenarios support these 6 English voice presets:

| Voice | Gender | Description |
|-------|--------|-------------|
| **Carter** | Male | Clear American English (default) |
| **Davis** | Male | American English |
| **Emma** | Female | American English |
| **Frank** | Male | American English |
| **Grace** | Female | American English |
| **Mike** | Male | American English |

**Multilingual voices** available in Python scenarios (Scenarios 1, 2, 5, 6):
- Spanish, German, French, Italian, Portuguese, Japanese, Korean, Dutch, Polish

---

## Directory Structure

```
src/
├── scenario-01-simple/                 # Python: minimal TTS demo
│   ├── main.py
│   └── main_multilingual.py
├── scenario-02-fullstack/              # Python + Blazor + Aspire
│   ├── backend/                        # FastAPI
│   ├── VoiceLabs.AppHost/              # Aspire orchestration
│   └── VoiceLabs.Web/                  # Blazor frontend
├── scenario-03-csharp-simple/          # C#: NuGet library simple demo
│   └── Program.cs
├── scenario-04-meai/                   # C# + Blazor + Aspire (full stack)
│   ├── VoiceLabs.Api/                  # C# WebAPI backend
│   ├── VoiceLabs.ConversationHost/     # Aspire orchestration
│   └── VoiceLabs.ConversationWeb/      # Blazor frontend
├── scenario-05-batch-processing/       # Python: batch conversion CLI
│   ├── batch_tts.py
│   └── sample-texts/
├── scenario-06-streaming-realtime/     # Python: low-latency streaming
│   └── stream_tts.py
├── scenario-07-maui-mobile/            # C#: cross-platform MAUI app
│   ├── VoiceLabs.ConversationHost/
│   └── MauiProgram.cs
└── scenario-08-onnx-native/            # Python export + C# inference
    ├── export/                         # Python export tools
    ├── csharp/                         # C# inference example
    └── models/                         # Exported ONNX files (gitignored)
```

---

## Quick Reference

| Task | Scenario | Command |
|------|----------|---------|
| Learn basics | 1 | `python main.py` |
| Build web app (Python backend) | 2 | `dotnet run` from AppHost |
| Try C# (recommended) | 3 | `dotnet run` |
| Build web app (C# only) | 4 | `dotnet run` from ConversationHost |
| Batch convert text files | 5 | `python batch_tts.py` |
| Test streaming performance | 6 | `python stream_tts.py` |
| Cross-platform mobile app | 7 | `dotnet build -t:Run` |
| Export ONNX models | 8 | `python export_model.py` |

---

## Getting Help

- See each scenario's **README.md** for detailed setup and customization
- Check [GETTING_STARTED.md](./GETTING_STARTED.md) for environment setup
- Read [ARCHITECTURE.md](./ARCHITECTURE.md) for system-level design details
- Refer to [API_REFERENCE.md](./API_REFERENCE.md) for C# library API details
