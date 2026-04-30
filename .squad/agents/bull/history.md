# Bull — History

## Joined
- **Date:** 2026-02-22
- **Reason:** Added to manage releases, version tagging, and NuGet publishing coordination

## Learnings

### Release v0.2.1-preview (2026-04-30)
- **PR Merged:** #25 (MaxTextLength as configurable property)
- **Changes:** 
  - Added `MaxTextLength` property to VibeVoiceOptions (default: 500)
  - Changed `ValidateTextLength` from static to instance method
  - Fixed compilation errors in VibeVoiceSynthesizer.cs
- **Tests:** All 109 tests pass (104 passed, 5 skipped due to missing models)
- **Version Bump:** 0.1.5-preview → 0.2.1-preview
- **Workflow:** Publish workflow triggered (run #25166158595)
- **Status:** ✅ Complete
- **Lesson:** Always verify compilation after PR merge before determining next version
