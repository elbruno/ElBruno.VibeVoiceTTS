# Holden — History

## Project Learnings (from init)
- Project: VibeVoice Labs — showcase for VibeVoice TTS
- User: Bruno Capuano
- Scenario 1: Minimal Python script with step-by-step TTS demo
- Scenario 2: Full stack — Python FastAPI backend + Blazor .NET 10 frontend + Aspire orchestration
- VibeVoice-Realtime-0.5B is the recommended model for real-time TTS (~300ms latency)
- Aspire Python integration uses `AddUvicornApp` for FastAPI apps

## Learnings

### 2026-02-20: Created comprehensive docs/scenarios.md guide (Issue #21)
- Successfully created `docs/scenarios.md` as referenced in root README.md Documentation table
- Document provides complete overview of all 8 scenarios with decision guidance
- Structure: title + intro, quick comparison table, "When to Use" guide, detailed per-scenario sections
- Each scenario section includes: what it demonstrates, prerequisites, key tech, how to run, directory path
- Added Python vs C# usage guide explaining when to choose each language
- Added voice presets reference table (6 English + multilingual support noted)
- Added directory structure map showing all 8 scenario folders
- Added quick reference table with commands for each scenario
- Read all 8 scenario README files to extract accurate technical details
- Matched existing docs style (headers, formatting, cross-references)
- Committed with issue fix reference: `Fixes #21`

### 2026-02-19: Architecture documentation completed for all 7 scenarios
- Updated summary table: Scenario 3 now "CSnakes Embedded Python", Scenario 4 now "Real-Time Conversation"
- Added detailed architecture section for Scenario 3: in-process CPython embedding via CSnakes library, no subprocess overhead, direct function calls from C#, voice presets auto-downloaded
- Added detailed architecture section for Scenario 4: WebSocket-based real-time conversation, STT (Parakeet) → AI (OpenAI) → TTS (VibeVoice) pipeline, push-to-talk microphone capture in Blazor frontend
- Added detailed architecture section for Scenario 5: Python batch CLI with ThreadPoolExecutor parallelization, YAML front-matter voice overrides, progress tracking via tqdm, shared VibeVoice model instance across threads
- Added detailed architecture section for Scenario 6: streaming TTS with time-to-first-chunk ~300ms, audio chunks played immediately via sounddevice, fallback to file-only mode if no audio hardware
- Added detailed architecture section for Scenario 7: .NET MAUI cross-platform app (Windows/Android/iOS/macOS), single XAML codebase, TtsService HttpClient wrapper, Plugin.Maui.Audio for native playback
- Each section includes ASCII architecture diagram, component descriptions, data flow diagram, and technology rationale
- All diagrams maintain visual consistency with Scenario 2 documentation
- Scenario 2 section preserved intact

### 2026-02-19: Complete documentation overhaul for all 7 scenarios
- Updated root README.md to ensure all 7 scenarios are clearly listed with descriptions and links
- Expanded docs/GETTING_STARTED.md to include detailed setup instructions for all 7 scenarios (1-7)
- Added "All Scenarios at a Glance" reference table for quick navigation
- Updated docs/USER_MANUAL.md table of contents to include all 7 scenarios
- Added comprehensive scenario overview table showing language, focus, difficulty, and use cases
- Inserted detailed usage sections for scenarios 3-7 with quick start commands and configuration
- Updated docs/ARCHITECTURE.md with "Scenario Architectures at a Glance" table
- All 7 scenario README.md files verified to exist and contain accurate descriptions
- Documentation now provides complete guide for developers choosing which scenario to explore
- Cross-referenced all docs: README → GETTING_STARTED → USER_MANUAL → ARCHITECTURE → per-scenario READMEs

### 2026-02-19: Scenario 4 restructured into Aspire-orchestrated fullstack app
- Renamed old `Program.cs` → `Program.cs.bak` and `Plugins/SpeechPlugin.cs` → `SpeechPlugin.cs.bak`
- Created `VoiceLabs.ConversationHost/` (Aspire AppHost) with SDK 13.0.0, Aspire.Hosting.Python 13.1.1
- AppHost references `VoiceLabs.ConversationWeb` project and uses `AddUvicornApp` for Python backend
- Copied `VoiceLabs.ServiceDefaults/` from scenario-02 (already existed, verified matching)
- Created `VoiceLabs.slnx` referencing ConversationHost, ServiceDefaults, ConversationWeb
- Created empty `backend/` and `VoiceLabs.ConversationWeb/` directory stubs for Naomi and Alex
- Created `.aspire/settings.json` pointing to ConversationHost
- Pattern matches scenario-02-fullstack exactly (same SDK, packages, ServiceDefaults)

Voice details confirmed: Carter, Davis, Emma, Frank, Grace, Mike (English presets)
Model: microsoft/VibeVoice-Realtime-0.5B
Install: `pip install "vibevoice[streamingtts] @ git+https://github.com/microsoft/VibeVoice.git"`

### 2026-02-19: Project structure and API contract finalized
- Designed directory layout with clear separation between Scenario 1 and Scenario 2
- Defined complete API contract: /api/health, /api/voices, /api/tts endpoints
- Established voice registry with 14 language/voice combinations
- Set constraints: WAV format, 1000-char text limit, snake_case JSON properties
- Specified clear boundaries for Naomi (backend) and Alex (frontend) parallel work

📌 Team update (2026-02-19): Alex implemented Blazor frontend with glassmorphism UI, Aspire orchestration, service discovery via `http://backend`, JSON serialization with `[JsonPropertyName]` for snake_case compatibility — decided by Alex

### 2026-02-19: Comprehensive project documentation created
- Created complete README.md overhaul with badges, feature list, project structure, and quick start guides
- Created docs/ARCHITECTURE.md with ASCII system diagrams, component descriptions, and data flow documentation
- Created docs/GETTING_STARTED.md with step-by-step setup for both scenarios and troubleshooting guide
- Created docs/API_REFERENCE.md with full endpoint documentation, voice reference table, and code examples
- Completed docs/USER_MANUAL.md replacing all TODO placeholders with actual usage instructions

**Documentation structure:**
- Voice registry contains 14 voices: 5 English (US, GB, AU), 7 European (DE, FR, IT, ES, PT, NL, PL), 2 Asian (JP, KR)
- API contract: /api/health, /api/voices, /api/tts with WAV output at 24kHz
- Aspire configuration uses `AddUvicornApp` for Python backend integration
- Frontend uses base64 data URLs for audio playback (no server-side file storage)

### 2026-02-19: Created comprehensive project-structure.md documentation (Fixes #20)
- Analyzed full repository tree including 8 scenarios, core library, and test suites
- Examined directory structure and existing docs for style consistency
- Created docs/project-structure.md with:
  - Overview intro explaining role of framework
  - Complete visual directory tree with all major paths
  - Top-level directory reference table (11 directories documented)
  - Detailed core library structure (.csproj, interfaces, synthesizer, pipeline, utils)
  - Test project organization (13 test files across unit, integration, security)
  - 8 scenario implementations: simple Python → MAUI mobile → native ONNX
  - Documentation structure reference
  - Squad AI team orchestration overview (agents, decisions, skills)
- Committed with "Fixes #20" linking to GitHub issue
- Documentation follows consistent markdown style with headers, tables, code blocks
- Cross-references to related docs (GETTING_STARTED, API_REFERENCE, ARCHITECTURE)
