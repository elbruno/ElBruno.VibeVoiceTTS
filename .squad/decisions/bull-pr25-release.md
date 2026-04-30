# Bull Release Decision: PR #25 → v0.2.1-preview

**Date:** 2026-04-30  
**Release Manager:** Bull  
**PR:** #25 — Added MaxTextLength as a configurable property for Speech Synthesizer

## Decision Summary

✅ **APPROVED FOR RELEASE** — PR #25 successfully merged and released as **v0.2.1-preview**

## Context

PR #25 adds a configurable `MaxTextLength` property to the VibeVoice synthesizer:
- Default: 500 characters (existing behavior)
- Configurable: users can set custom limits
- Disableable: set to 0 or Int32.MaxValue to disable validation

## Actions Taken

1. **Merged PR #25** using `gh pr merge 25 --merge --delete-branch`
2. **Fixed Compilation Errors** — `ValidateTextLength` was still marked as static; changed to instance method
3. **Verified Tests** — All 109 tests pass (104 passed, 5 skipped due to missing ONNX models)
4. **Bumped Version** — 0.1.5-preview → 0.2.1-preview (patch increment)
5. **Created Git Tag** — v0.2.1-preview
6. **Pushed to GitHub** — Commits and tags
7. **Triggered Publish Workflow** — Manual workflow dispatch to ensure NuGet publish

## Test Results

```
Passed:   104
Skipped:  5 (missing model files)
Failed:   0
Duration: 46 ms
```

## Version Notes

- Previous stable release: v0.2.0
- This release: v0.2.1-preview (preview mode)
- Next release will increment patch: v0.2.2-preview (unless major/minor bump requested)

## Workflow Status

- Publish workflow triggered: https://github.com/elbruno/ElBruno.VibeVoiceTTS/actions/runs/25166158595
- Expected outcome: Package published to NuGet within minutes

## Key Learning

During merge, the PR's changes included a compilation error where `ValidateTextLength` was transitioning from static to instance method but the implementation wasn't fully updated. This required a post-merge fix before tests could run. Future releases should include pre-merge validation of compilation status.

---

**Status:** ✅ Complete  
**NuGet Package:** https://www.nuget.org/packages/ElBruno.VibeVoiceTTS (v0.2.1-preview)
