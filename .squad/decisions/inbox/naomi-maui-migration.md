### Decision: MAUI Scenario 7 Migration Approach
**By:** Naomi (Backend Dev)
**Date:** 2025-07-24
**Issue:** #19

**What:**
Migrated MAUI Scenario 7 from Python HTTP backend to in-process ONNX inference using the `ElBruno.VibeVoiceTTS` NuGet package. Key decisions:

1. **NuGet package reference** (not project reference) — keeps the MAUI app decoupled from the library source, matching the real-world consumption pattern
2. **In-memory WAV conversion** — built a `ConvertToWavBytes()` helper that writes RIFF/WAVE header + PCM data to a MemoryStream, avoiding temp files
3. **VoiceDisplayItem wrapper** — created a local display model since the library's `VoiceInfo` record lacks a `DisplayName` property needed for XAML picker binding
4. **GetSupportedVoiceDetails()** over `GetAvailableVoices()` — shows all 6 voices in the picker; voices auto-download on first use
5. **Singleton VibeVoiceTtsService** — wraps `IVibeVoiceSynthesizer` (also singleton) to keep model in memory across the app lifecycle

**Why:**
Eliminates the Python backend dependency entirely. Users no longer need Python, pip, or uvicorn — just `dotnet build` and run. Models auto-download from HuggingFace on first launch. This makes the MAUI scenario self-contained and cross-platform ready.

**Trade-offs:**
- First launch requires ~1.5 GB model download (mitigated with progress UI)
- App binary doesn't include models (they're cached in the OS app data directory)
- The pre-existing CommunityToolkit.Maui version conflict with .NET 10 remains unfixed (out of scope)
