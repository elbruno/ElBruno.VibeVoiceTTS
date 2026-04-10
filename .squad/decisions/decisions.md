# Decisions

## Decision: Comprehensive Project Structure Documentation (Fixes #20)

**Decision Owner:** Holden  
**Date:** 2026-02-19  
**Status:** Complete  
**Issue:** #20

### Problem Statement

The root README.md references `docs/project-structure.md` in its Documentation table:
```
| [Project Structure](docs/project-structure.md) | Repository layout and file organization |
```

However, this file did not exist, creating a broken reference and leaving developers without clear understanding of the repository organization.

### Decision

Created comprehensive `docs/project-structure.md` documentation that provides:

1. **Overview intro** — explains the role of VibeVoiceTTS as a comprehensive voice synthesis framework
2. **Complete repository tree** — visual directory structure with all major paths and components
3. **Top-level directory reference** — table explaining purpose of each root-level directory (11 directories)
4. **Core library structure** (`src/ElBruno.VibeVoiceTTS/`) — detailed breakdown:
   - Main classes: synthesizer, options, presets, voice info, model manager
   - Pipeline components: ONNX inference, tokenizer, preset loader, diffusion scheduler
   - Utilities: audio writing, tensor helpers
   - Extensions: dependency injection setup
5. **Test project structure** (`src/ElBruno.VibeVoiceTTS.Tests/`) — 13 test files organized by concern
6. **8 Scenario implementations** — clear descriptions of each scenario's purpose:
   - Scenario 1: Simple Python usage
   - Scenario 2: Full-stack Blazor + Python backend
   - Scenario 3: C# console application
   - Scenario 4: Multi-service with Aspire
   - Scenario 5: Batch processing
   - Scenario 6: Streaming & real-time
   - Scenario 7: MAUI mobile (cross-platform)
   - Scenario 8: Native ONNX implementation
7. **Documentation structure** — reference table for all doc files
8. **Squad orchestration** — brief overview of `.squad/` directory structure and purpose

### Rationale

**Completeness:** The documentation captures the full repository structure by examining actual directory contents, ensuring accuracy and preventing drift.

**Consistency:** Style matches existing docs (ARCHITECTURE.md, GETTING_STARTED.md) using:
- Clear markdown headers and organization
- Tables for reference information
- Code blocks for file paths
- Cross-references to related documentation

**Developer Experience:** Provides multiple entry points:
- Visual tree for quick orientation
- Directory reference table for specific directories
- Scenario descriptions help developers choose which implementation to learn from
- Links to related documentation (API_REFERENCE, ARCHITECTURE, GETTING_STARTED)

### Impact

- ✅ Fixes broken reference in README.md
- ✅ Enables developers to understand repository organization
- ✅ Provides context for choosing which scenario/component to explore
- ✅ Facilitates onboarding for new team members
- ✅ Documents Squad AI team orchestration patterns

### Verification

- [x] File created at correct path: `docs/project-structure.md`
- [x] Git committed with "Fixes #20"
- [x] Style consistent with existing documentation
- [x] All 8 scenarios documented
- [x] Core library components listed with descriptions
- [x] Test suite organization explained
- [x] Squad orchestration structure documented

---

## Decision: MAUI Scenario 7 Migration Approach

**Decision Owner:** Naomi (Backend Dev)  
**Date:** 2025-07-24  
**Issue:** #19  

### What

Migrated MAUI Scenario 7 from Python HTTP backend to in-process ONNX inference using the `ElBruno.VibeVoiceTTS` NuGet package. Key decisions:

1. **NuGet package reference** (not project reference) — keeps the MAUI app decoupled from the library source, matching the real-world consumption pattern
2. **In-memory WAV conversion** — built a `ConvertToWavBytes()` helper that writes RIFF/WAVE header + PCM data to a MemoryStream, avoiding temp files
3. **VoiceDisplayItem wrapper** — created a local display model since the library's `VoiceInfo` record lacks a `DisplayName` property needed for XAML picker binding
4. **GetSupportedVoiceDetails()** over `GetAvailableVoices()` — shows all 6 voices in the picker; voices auto-download on first use
5. **Singleton VibeVoiceTtsService** — wraps `IVibeVoiceSynthesizer` (also singleton) to keep model in memory across the app lifecycle

### Why

Eliminates the Python backend dependency entirely. Users no longer need Python, pip, or uvicorn — just `dotnet build` and run. Models auto-download from HuggingFace on first launch. This makes the MAUI scenario self-contained and cross-platform ready.

### Trade-offs

- First launch requires ~1.5 GB model download (mitigated with progress UI)
- App binary doesn't include models (they're cached in the OS app data directory)
- The pre-existing CommunityToolkit.Maui version conflict with .NET 10 remains unfixed (out of scope)
