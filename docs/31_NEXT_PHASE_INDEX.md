# Next Phase Index

> Progressive-disclosure entrypoint for the next implementation phase of **The Automation Game**.

## Why this document exists

The project has moved beyond proving that the core simulation and first greybox quest can work. The next phase is about turning that proof into a game that is comfortable to play, cheap to extend, visually legible, narratively coherent, and structurally ready for the second industry.

Do **not** start a session by reading every design document. Start here, read the session contract, then open only the documents linked for the selected session.

## Current baseline

Treat the following as existing product capabilities unless the repository proves otherwise during the session:

- deterministic, engine-independent simulation;
- playable dish-station vertical slice;
- first-shift quest arc through the reliability trial;
- save/resume and progression shell;
- sandbox placement;
- seven system lenses;
- native UI smoke automation;
- headless deterministic execution;
- architecture/performance proof at large actor counts.

The next phase should therefore **not** rebuild the vertical slice. It should improve the interaction and presentation seam, establish scalable content authoring, give the player richer tools, and then prove reuse by shipping the warehouse chapter.

## Read order

For any implementation session:

1. `../AGENTS.md`
2. `31_NEXT_PHASE_INDEX.md`
3. `34_SESSION_DELIVERY_MODEL.md`
4. `35_SESSION_BACKLOG.md`
5. Only the specialist document(s) referenced by the selected session
6. Existing repository docs/code directly relevant to that session

## Specialist documents

| Need | Open |
|---|---|
| Full product order and gates | `33_PRODUCT_ROADMAP.md` |
| Current product gaps | `32_CURRENT_STATE_AND_GAP_AUDIT.md` |
| Input, movement, interaction, camera, sandbox | `36_GAMEPLAY_INPUT_CAMERA_SANDBOX.md` |
| UI, UX, accessibility, game feel | `37_UI_UX_AND_GAME_FEEL.md` |
| Content schemas and procedural templates | `38_CONTENT_AUTHORING_AND_PROCEDURAL_GENERATION.md` |
| Art, presentation, animation, VFX, audio | `39_ASSET_PRESENTATION_AND_AUDIO.md` |
| Campaign, characters, player personas | `40_CAMPAIGN_STORY_CHARACTERS_PERSONAS.md` |
| Design-pattern curriculum and PatternKit | `41_PATTERN_LEARNING_AND_PATTERNKIT.md` |
| PatternKit catalog placement | `41A_PATTERNKIT_COVERAGE_MATRIX.md` |
| Architecture seams and refactoring rules | `42_ARCHITECTURE_EVOLUTION.md` |
| Testing, playtesting, release gates | `43_QUALITY_PLAYTEST_RELEASE_GATES.md` |
| Canonical agent `/goal` behavior | `44_GOAL_PROMPT.md` |
| Decisions and bounded spikes | `45_DECISION_LOG_AND_OPEN_SPIKES.md` |

Templates live in `templates/`.

## The next-phase invariant

Every session must leave behind something a human can verify immediately.

Good session outcomes:

- "The player can now walk with WASD and interact with the washer using E."
- "A dish-station scenario loads from YAML and produces the exact same deterministic run as the old C# definition."
- "The first authored 3D washer can replace its placeholder without touching simulation code."
- "The warehouse receiving scenario runs headlessly and proves the same queue/workstation ontology supports a second industry."

Bad session outcomes:

- "Created a future abstraction layer."
- "Started refactoring the client."
- "Added scaffolding for content generation."
- "Made progress on the warehouse."

## Product direction

The product should progress through these capabilities in order:

```text
PROVEN GREYBOX
    ↓
COMFORTABLE PLAY
    ↓
PRESENTATION SEAM
    ↓
DATA-DRIVEN CONTENT
    ↓
PLAYER-AUTHORED PROCESS + AUTOMATION
    ↓
PRODUCTION-QUALITY RESTAURANT CHAPTER
    ↓
SECOND-INDUSTRY REUSE PROOF
    ↓
PATTERN LEARNING SYSTEM
    ↓
MULTI-INDUSTRY CAMPAIGN
    ↓
SOFTWARE / PLATFORM PAYOFF
    ↓
ORGANIZATION-SCALE SANDBOX
```

This order is intentional. The project should not accumulate dozens of quests on top of controls, authoring, or presentation systems that are expensive to change.

## Session selection rule

When invoked with `/goal`:

- if a session ID is provided, execute that session if its prerequisites are satisfied;
- otherwise select the **first incomplete, unblocked session** in `35_SESSION_BACKLOG.md`;
- do not silently expand scope;
- if the selected task reveals a prerequisite, either fix it inside the stated scope if small or stop with a bounded spike/proposed prerequisite session;
- update the backlog evidence before ending.

## Definition of next-phase success

The next phase is successful when:

1. a first-time player can navigate and interact without a keyboard legend;
2. the restaurant feels like a place rather than a debug visualization;
3. quests, characters, facilities, incidents, and pattern exposures can be authored without changing simulation source;
4. process capture and automation are player-facing tools rather than scenario-only hotkeys;
5. the warehouse chapter reuses the same primitives with no restaurant-specific leakage;
6. Pattern Codex progress emerges from play and records evidence from multiple domains;
7. a session agent can reliably select one bounded unit of work and finish it with a reproducible proof.
