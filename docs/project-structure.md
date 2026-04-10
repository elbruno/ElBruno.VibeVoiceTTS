# Project Structure

This document provides an overview of the VibeVoiceTTS repository organization, directory layout, and the purpose of each component.

## Overview

VibeVoiceTTS is a comprehensive voice synthesis framework built on C# and ONNX, with Python support for advanced scenarios. The repository is organized into a core NuGet library, test suites, and eight scenario-based implementations that demonstrate different use cases and technologies.

---

## Repository Directory Tree

```
ElBruno.VibeVoiceTTS/
├── .copilot/                          # GitHub Copilot CLI configuration
├── .github/                           # GitHub workflows and CI/CD
├── .squad/                            # AI team orchestration metadata
│   ├── agents/                        # Individual agent configurations
│   ├── decisions/                     # Decision logs and inbox
│   ├── skills/                        # Skill definitions
│   ├── ceremonies.md                  # Team ceremonies and meetings
│   ├── identity/                      # Team identity and roles
│   ├── routing.md                     # Request routing rules
│   └── team.md                        # Team structure and members
├── .squad-templates/                  # Squad template definitions
├── docs/                              # Project documentation
│   ├── API_REFERENCE.md               # Public API documentation
│   ├── ARCHITECTURE.md                # System architecture overview
│   ├── GETTING_STARTED.md             # Setup and quickstart guide
│   ├── USER_MANUAL.md                 # User guide and examples
│   ├── publishing.md                  # NuGet publishing guidelines
│   └── project-structure.md           # This file
├── images/                            # Documentation images
├── src/                               # Source code
│   ├── ElBruno.VibeVoiceTTS/          # Core NuGet library
│   ├── ElBruno.VibeVoiceTTS.Tests/    # Test project
│   ├── scenario-01-simple/            # Simple Python usage
│   ├── scenario-02-fullstack/         # Fullstack C# with Python backend
│   ├── scenario-03-csharp-simple/     # Simple C# console app
│   ├── scenario-04-meai/              # Multi-service architecture with Aspire
│   ├── scenario-05-batch-processing/  # Batch TTS processing
│   ├── scenario-06-streaming-realtime/# Real-time streaming
│   ├── scenario-07-maui-mobile/       # Cross-platform mobile (MAUI)
│   └── scenario-08-onnx-native/       # Native ONNX implementation
├── requirements.txt                   # Python dependencies (shared)
├── README.md                          # Main project readme
├── LICENSE                            # License file
├── COMPLETE_SYSTEM_DELIVERY.md        # Delivery documentation
├── READINESS_SYSTEM_GUIDE.md          # System readiness guide
├── TEST_RESULTS_AND_STATUS.md         # Test results and metrics
└── .gitignore, .gitattributes         # Git configuration

```

---

## Top-Level Directory Reference

| Directory | Purpose |
|-----------|---------|
| **`.copilot/`** | GitHub Copilot CLI configuration and local settings |
| **`.github/`** | GitHub workflows, CI/CD pipelines, issue templates, and actions |
| **`.squad/`** | AI team orchestration: agents, decisions, skills, team structure |
| **`.squad-templates/`** | Reusable Squad configuration templates |
| **`docs/`** | Project documentation including guides, API reference, and architecture |
| **`images/`** | Visual assets used in documentation |
| **`src/`** | All source code: core library, tests, and scenario implementations |

---

## Core Library: `src/ElBruno.VibeVoiceTTS/`

The main NuGet package providing the voice synthesis pipeline.

```
ElBruno.VibeVoiceTTS/
├── ElBruno.VibeVoiceTTS.csproj         # Project file and NuGet metadata
├── IVibeVoiceSynthesizer.cs            # Interface defining synthesis contract
├── VibeVoiceSynthesizer.cs             # Main synthesis engine
├── VibeVoiceOptions.cs                 # Configuration options
├── VibeVoicePreset.cs                  # Voice preset definitions
├── VoiceInfo.cs                        # Voice metadata and information
├── ModelManager.cs                     # Model lifecycle management
├── DownloadProgress.cs                 # Progress tracking for model downloads
├── ExecutionProvider.cs                # ONNX execution provider selection (CPU/GPU)
├── Extensions/
│   └── ServiceCollectionExtensions.cs  # Dependency injection setup
├── Pipeline/
│   ├── OnnxInferencePipeline.cs        # ONNX model inference
│   ├── BpeTokenizer.cs                 # Text tokenization
│   ├── VoicePresetLoader.cs            # Load and parse voice presets
│   └── DiffusionScheduler.cs           # Diffusion noise scheduling
└── Utils/
    ├── AudioWriter.cs                  # WAV/audio file generation
    └── TensorHelpers.cs                # Tensor manipulation utilities
```

**Key Components:**
- **Pipeline**: Orchestrates text-to-speech inference using ONNX Runtime
- **Utils**: Helper functions for audio processing and tensor operations
- **Extensions**: Dependency injection integration with .NET Core

---

## Test Project: `src/ElBruno.VibeVoiceTTS.Tests/`

Comprehensive test suite for the core library.

```
ElBruno.VibeVoiceTTS.Tests/
├── ElBruno.VibeVoiceTTS.Tests.csproj   # Test project file
├── VibeVoiceSynthesizerTests.cs        # Main synthesizer tests
├── VibeVoiceOptionsTests.cs            # Configuration validation
├── VibeVoicePresetTests.cs             # Preset loading and parsing
├── IntegrationTests.cs                 # End-to-end integration tests
├── ExecutionProviderTests.cs           # Execution provider selection
├── TensorHelpersTests.cs               # Tensor utility tests
├── AudioWriterTests.cs                 # Audio output generation
├── DiffusionSchedulerTests.cs          # Diffusion scheduler tests
├── DownloadProgressTests.cs            # Progress tracking tests
├── OptionsValidationTests.cs           # Option validation tests
└── PathTraversalTests.cs               # Security path validation tests
```

**Test Categories:**
- **Unit Tests**: Individual component functionality
- **Integration Tests**: Component interaction and end-to-end workflows
- **Security Tests**: Path traversal and input validation

---

## Scenario-Based Implementations

The repository includes 8 demonstration scenarios showcasing different technologies and patterns:

### Scenario 1: Simple Python
**Path:** `src/scenario-01-simple/`
- Basic Python usage of the core library
- Text-to-speech generation scripts
- Entry point for users new to VibeVoiceTTS

### Scenario 2: Full-Stack Web Application
**Path:** `src/scenario-02-fullstack/`
- Blazor frontend (C#)
- Python backend API
- End-to-end TTS web service
- Service orchestration patterns

### Scenario 3: Simple C# Console
**Path:** `src/scenario-03-csharp-simple/`
- Minimal C# console application
- Direct library usage
- Quick .NET integration example

### Scenario 4: Multi-Service Architecture (MEAI)
**Path:** `src/scenario-04-meai/`
- Microsoft .NET Aspire orchestration
- Conversation hosting service
- Multiple interconnected services
- Enterprise-grade architecture patterns

### Scenario 5: Batch Processing
**Path:** `src/scenario-05-batch-processing/`
- Process multiple text files
- Parallel synthesis
- Production batch workflows
- Sample texts in multiple languages

### Scenario 6: Streaming & Real-Time
**Path:** `src/scenario-06-streaming-realtime/`
- Real-time audio streaming
- Low-latency synthesis
- Live speech generation

### Scenario 7: Mobile (MAUI)
**Path:** `src/scenario-07-maui-mobile/`
- .NET MAUI cross-platform mobile app
- iOS, Android, Windows, macOS support
- Mobile voice synthesis integration

### Scenario 8: Native ONNX
**Path:** `src/scenario-08-onnx-native/`
- Direct ONNX Runtime usage (no Python)
- Model export and validation
- Native C# implementation
- Voice preset export utilities

---

## Documentation Structure: `docs/`

| File | Purpose |
|------|---------|
| **GETTING_STARTED.md** | Installation, prerequisites, and setup guide |
| **API_REFERENCE.md** | Complete public API documentation |
| **ARCHITECTURE.md** | System design, pipeline flow, and technical decisions |
| **USER_MANUAL.md** | Usage examples, best practices, and troubleshooting |
| **publishing.md** | NuGet package publishing workflow |
| **project-structure.md** | This file: repository organization |

---

## Squad AI Team Orchestration: `.squad/`

The `.squad/` directory contains the configuration and state for the VibeVoice Labs AI team (orchestrated by GitHub Copilot):

```
.squad/
├── agents/                             # Agent role configurations
│   ├── bruno/                          # Bruno's agent configuration
│   ├── holden/                         # Holden's agent configuration
│   └── [other agents]/
├── decisions/                          # Decision records
│   ├── inbox/                          # Pending decision files
│   └── [archived decisions]/
├── skills/                             # Reusable skill definitions
├── identity/                           # Team identity settings
├── team.md                             # Team members and roles
├── ceremonies.md                       # Meeting schedules and ceremonies
├── routing.md                          # Request routing configuration
└── orchestration-log                   # Orchestration activity log
```

**Purpose:** Facilitates team-based development with GitHub Copilot CLI, enabling role-specific workflows and decision tracking.

---

## Dependencies & Configuration

- **`requirements.txt`** - Shared Python dependencies for all Python scenarios
- **`ElBruno.VibeVoiceTTS.csproj`** - NuGet package metadata and dependencies
- **`.gitignore`** - Git exclusion rules (model caches, build artifacts, etc.)

---

## Key Architectural Patterns

### Library Structure
- **Service-based**: Core functionality exposed through `IVibeVoiceSynthesizer` interface
- **Dependency Injection**: Integrated with .NET Core DI via extensions
- **Model Management**: Automatic model downloading and caching

### Scenario Organization
- Each scenario is self-contained with its own dependencies
- Scenarios demonstrate different architectural patterns and technologies
- Shared Python virtual environment (`.venv`) for all Python scenarios

### Testing Strategy
- Comprehensive test coverage in dedicated test project
- Unit and integration tests
- Security validation tests

---

## Getting Started

For setup instructions, see [GETTING_STARTED.md](GETTING_STARTED.md).

For API usage, see [API_REFERENCE.md](API_REFERENCE.md).

For system design details, see [ARCHITECTURE.md](ARCHITECTURE.md).

---

Fixes #20
