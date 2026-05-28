# Decision: Issue #26 — Document MaxTextLength

## Summary
Issue #26 is a documentation-focused patch for `VibeVoiceOptions.MaxTextLength`.

## Decision
- Document `MaxTextLength` in the public XML comments.
- Surface the option in `README.md` because it is the NuGet package readme.
- Add a short note in `docs/GETTING_STARTED.md` for the C# samples.
- Add regression tests to lock in the default value and disabled behavior.

## Release Impact
- Package version bumped to `0.5.1`.

