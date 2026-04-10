# Naomi's History

## Session 1: ONNX Pipeline TODO Resolution (Issue #22)

### Task
Annotate and resolve ONNX pipeline TODOs in scenario-08 reference code.

### Work Completed
- Replaced 10 TODO comments in `VibeVoiceOnnxPipeline.cs` with clarifying NOTE comments explaining:
  - These are illustrative placeholder tensor names based on common ONNX TTS patterns
  - Production inference uses the ElBruno.VibeVoiceTTS NuGet library
  - Users should inspect their own exported models with Netron to verify tensor names
- Added comprehensive header comment to `VibeVoiceOnnxPipeline.cs` clarifying it's reference code
- Updated 1 TODO in `example_csharp.md` with guidance on verifying tensor names
- Updated 2 TODOs in `example_inference.py` with similar clarification
- All code logic preserved; only comments changed
- Committed with message linking to issue #22

### Changes Summary
- **VibeVoiceOnnxPipeline.cs**: 10 TODOs → NOTE comments + reference code header
- **example_csharp.md**: 1 TODO → NOTE comment
- **example_inference.py**: 2 TODOs → NOTE comments
- Commit: `4561ee3` on `squad/backlog-sprint`

## Learnings

1. **Reference vs. Production Code**: Clearly distinguish reference/educational code from production implementations in documentation headers. This prevents users from deploying example code expecting production-grade behavior.

2. **Tooling Guidance**: Always direct users to inspection tools (Netron, model introspection APIs) when tensor names may vary across implementations. This is critical for ONNX workflows where model exports can differ.

3. **Comment Migration**: Replacing ambiguous TODOs with concrete NOTEs that explain *why* something is illustrative improves maintainability and reduces confusion about whether something is a bug or design choice.

4. **Multi-file Consistency**: When updating related files (C# code, docs, Python examples), ensure comments follow a consistent pattern so documentation reads as a cohesive whole.

## Session 2: MAUI Scenario 7 NuGet Migration (Issue #19)

### Task
Migrate MAUI Scenario 7 from Python HTTP backend to in-process ONNX via `ElBruno.VibeVoiceTTS` NuGet package.

### Work Completed
- Replaced `TtsService` (HTTP client calling FastAPI on localhost:5100) with `VibeVoiceTtsService` wrapping `IVibeVoiceSynthesizer`
- Added `ElBruno.VibeVoiceTTS` NuGet package reference to csproj
- Updated `MauiProgram.cs` DI: removed HttpClient, added `AddVibeVoice()` + `VibeVoiceTtsService` singleton
- Updated `MainPage.xaml.cs`: model init with download progress instead of backend health check
- Updated `MainPage.xaml`: status text from "Checking backend..." to "Initializing model..."
- Deleted old `TtsService.cs`
- Updated scenario-07 README: new architecture diagram (in-process ONNX), removed Python prerequisites
- Updated root README: scenario 7 status 🚧 → ✅

### Learnings

5. **WAV In-Memory Conversion**: The library's `SaveWav()` only writes to file paths. For MAUI playback via `Plugin.Maui.Audio` (which takes a `Stream`), build WAV bytes in memory: RIFF header (44 bytes) + float→Int16 PCM at 24kHz, mono.

6. **VoiceInfo Wrapping**: The library's `VoiceInfo` is a sealed record without `DisplayName`. Created a local `VoiceDisplayItem` class for XAML `ItemDisplayBinding` compatibility.

7. **GetSupportedVoiceDetails vs GetAvailableVoices**: Use `GetSupportedVoiceDetails()` for UI pickers (shows all 6 voices). `GetAvailableVoices()` only returns downloaded voices. Voices auto-download on first use.

8. **AddVibeVoice Namespace**: The DI extension lives in `ElBruno.VibeVoiceTTS.Extensions`, not the root namespace.
