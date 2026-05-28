# Drummer — History

## Joined
- **Date:** 2026-02-22
- **Reason:** Added to manage GitHub issues and coordinate fixes across the team

## Learnings

### Issue #26 — Max text length documentation (2026-05-28)
- **Files updated:** `src/ElBruno.VibeVoiceTTS/VibeVoiceOptions.cs`, `README.md`, `docs/GETTING_STARTED.md`
- **Tests added:** `VibeVoiceOptionsTests` and `VibeVoiceSynthesizerTests` now cover the default `MaxTextLength` value and the disabled-path behavior.
- **Release note:** Bumped package version to `0.5.1` for the documentation patch release.
- **Key pattern:** Public option docs should be reflected in both XML comments and the package README because `README.md` is the NuGet package readme.
