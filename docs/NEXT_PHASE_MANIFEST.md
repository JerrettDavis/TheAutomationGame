# Next Phase Documentation Manifest

This suite is intended to be copied into `TheAutomationGame/docs/`.

## Core operating documents

- `31_NEXT_PHASE_INDEX.md` — progressive-disclosure entrypoint.
- `32_CURRENT_STATE_AND_GAP_AUDIT.md` — product gaps across controls, UX, art, authoring, sandbox, story, architecture, validation, and platform.
- `33_PRODUCT_ROADMAP.md` — capability gates from current greybox through 1.0.
- `34_SESSION_DELIVERY_MODEL.md` — fixed-session execution contract.
- `35_SESSION_BACKLOG.md` — first 43 bounded sessions through the warehouse reuse proof.

## Specialist plans

- `36_GAMEPLAY_INPUT_CAMERA_SANDBOX.md`
- `37_UI_UX_AND_GAME_FEEL.md`
- `38_CONTENT_AUTHORING_AND_PROCEDURAL_GENERATION.md`
- `39_ASSET_PRESENTATION_AND_AUDIO.md`
- `40_CAMPAIGN_STORY_CHARACTERS_PERSONAS.md`
- `41_PATTERN_LEARNING_AND_PATTERNKIT.md`
- `41A_PATTERNKIT_COVERAGE_MATRIX.md`
- `42_ARCHITECTURE_EVOLUTION.md`
- `43_QUALITY_PLAYTEST_RELEASE_GATES.md`
- `44_GOAL_PROMPT.md`
- `45_DECISION_LOG_AND_OPEN_SPIKES.md`
- `47_FEATURE_SCREENSHOT_GALLERY.md` — reviewer-facing visual evidence mapped to delivered sessions.

## Templates

- `templates/SESSION_GOAL_TEMPLATE.md`
- `templates/QUEST_STORYBOARD_TEMPLATE.md`
- `templates/CHARACTER_PERSONA_TEMPLATE.md`
- `templates/CONTENT_TEMPLATE.md`
- `templates/SPIKE_TEMPLATE.md`

## Slash-command source

- `commands/goal.md`

Different agent harnesses install slash commands in different locations. Keep this file in docs as the canonical source and copy/link it into the harness-specific command directory when appropriate. For example, a harness that loads repository commands from `.claude/commands/` can use this file as the source for `.claude/commands/goal.md`.

## Integration notes

1. Copy the files without deleting the repository's existing `docs/01...30` set.
2. `31_NEXT_PHASE_INDEX.md` intentionally treats the new suite as a continuation/superseding next-phase layer rather than rewriting historical design docs.
3. Reconcile any session marked TODO against repository truth on first `/goal`; if code has already implemented it, record evidence and advance.
4. Keep later roadmap gates broad until the warehouse reuse audit. That audit is the deliberate point where the next fixed tranche should be generated.
