# Current State and Gap Audit

> Living audit of the gap between the current greybox and the intended product.

This document should be updated whenever a gap is closed, invalidated, split, or newly discovered. It is not a backlog. `35_SESSION_BACKLOG.md` converts selected gaps into fixed delivery sessions.

## Severity vocabulary

- **P0**: blocks meaningful player validation or the next architectural proof.
- **P1**: materially weakens play, authoring speed, or extensibility.
- **P2**: important before campaign alpha.
- **P3**: polish, scale, or post-alpha concern.

---

## 1. Input, movement, and interaction

### G-IN-001: No conventional direct movement model — P0

The current slice is primarily hotkey and click driven. Conventional WASD locomotion is not yet the default embodied control scheme, and `W` is currently consumed by scenario behavior.

**Target:** semantic input actions with WASD movement, context interaction, click-to-move retained, and no quest logic directly bound to production keys.

### G-IN-002: Scenario-specific key soup — P0

Several current keys expose quest/scenario actions directly. This is useful for prototyping but does not scale to a discoverable game.

**Target:** world interaction and UI affordances invoke actions; scenario shortcuts move behind developer mode.

### G-IN-003: No rebindable action map — P1

**Target:** logical actions such as `MoveUp`, `Interact`, `Inspect`, `OpenJournal`, `ToggleLens`, `BuildMode`, `Pause` map to devices independently of gameplay systems.

### G-IN-004: Camera controls compete with gameplay keys — P1

**Target:** mouse-first camera pan/zoom plus keyboard alternatives. Movement should not share the same physical keys as camera translation.

### G-IN-005: Limited walkability/path model — P1

The current facility proof is a small fixed floor with simplified movement.

**Target:** explicit traversable cells/nav topology, obstacle occupancy, interaction ports, click-route generation, and deterministic movement commands.

### G-IN-006: Gamepad support absent — P2

Defer until mouse/keyboard action semantics are stable.

---

## 2. Game feel and embodiment

### G-FEEL-001: Player and workers read as debug pawns — P0

**Target:** recognizable character silhouettes, facing, motion, work animation cues, and clear selection/interaction states.

### G-FEEL-002: Weak action feedback — P0

Work completion, queue movement, machine transitions, failures, blocked actions, and successful automation need layered visual/audio feedback.

### G-FEEL-003: Little sense of place — P1

A restaurant should communicate room function before labels are read.

**Target:** walls, doors, counters, service openings, racks, floor materials, lighting, props, background activity, and ambient sound.

### G-FEEL-004: Time pressure lacks strong presentation vocabulary — P1

Queues and deadlines should be perceived in-world and through compact HUD feedback without requiring lens inspection.

---

## 3. UI and UX

### G-UX-001: Prototype UI is too implementation-centric — P0

The current custom drawing/pixel-text approach proves functionality but is not yet a scalable production UI shell.

**Target:** stable screen/router/modal/HUD abstractions before choosing how deeply to adopt a specific Stride UI implementation.

### G-UX-002: Discoverability depends on instructions/hotkeys — P0

**Target:** context prompts, hover/selection affordances, disabled-state explanation, progressive onboarding, and a compact input hint system.

### G-UX-003: No production process editor — P0

The player needs to capture, compare, and modify process definitions through gameplay.

### G-UX-004: No production automation editor — P0

Automation should be authored as rules/graphs/policies with traceability, not triggered via scenario shortcuts.

### G-UX-005: Pattern Codex is conceptual, not implemented — P1

### G-UX-006: Missing settings/accessibility baseline — P1

Needs at minimum:

- master/music/effects volume;
- UI scale;
- text size where practical;
- camera sensitivity;
- key rebinding;
- edge-scroll toggle if added;
- fullscreen/windowed;
- color-independent state cues;
- reduce motion / flash options;
- captions for information-bearing audio.

### G-UX-007: No clear controller/focus-navigation contract — P2

### G-UX-008: Localization-safe layout not yet designed — P2

---

## 4. World presentation and camera

### G-WORLD-001: Intended world is 3D; current world is procedural 2D isometric — P0

This is an acceptable prototype seam, not a product failure. The next step should prove incremental replacement rather than rewriting simulation or content.

### G-WORLD-002: Facility geometry is too fixed — P1

**Target:** room/facility definitions independent of renderer and reusable across authored/procedural layouts.

### G-WORLD-003: No presentation catalog — P0

Simulation/content IDs should resolve to presentation assets through a replaceable catalog with guaranteed fallbacks.

### G-WORLD-004: Camera framing rules are under-specified — P1

Need near/mid/far readability requirements, zoom limits, target-follow/recenter behavior, and visibility rules for labels/icons.

### G-WORLD-005: Occlusion strategy absent — P2

Walls/equipment must not hide relevant workers or interactables without a clear fade/cutaway strategy.

---

## 5. Assets, animation, VFX, and audio

### G-ASSET-001: Production asset library effectively absent — P0

The current repo should be assumed to have placeholders and a very small imported asset footprint.

Required production categories:

- modular restaurant environment;
- equipment/workstations;
- item families and state variants;
- characters and shared rig;
- reusable animations;
- UI icon set;
- effects;
- ambience and interaction audio.

### G-ASSET-002: Asset provenance/license manifest must scale — P0

Every imported third-party asset needs source, license, attribution requirement, modification status, and intended shipping status.

### G-ASSET-003: No deterministic visual-variant system — P1

Facilities, workers, props, and items need controlled variation without hand-authoring every instance.

### G-ASSET-004: No shared animation-state projection contract — P1

Simulation state should map to presentation states without animation owning truth.

### G-ASSET-005: Audio design largely missing — P1

Audio should communicate machine state, work, queue pressure, warnings, completion, failures, environment, and UI events.

---

## 6. Content authoring

### G-CONTENT-001: Content is still code-centric — P0

The first episode is useful as a reference implementation but cannot remain the authoring model for a large campaign.

**Target decision:** human-authored YAML compiled/validated into immutable runtime definitions. JSON remains appropriate for saves/replays/diagnostic interchange.

### G-CONTENT-002: Root content taxonomy is aspirational rather than operational — P0

Need an actual compiler/loader for industries, facilities, workstations, items, processes, scenarios, quests, characters, incidents, patterns, dialogue, and templates.

### G-CONTENT-003: No schema/version/reference validation pipeline — P0

### G-CONTENT-004: No dedicated content-validation test project — P0

### G-CONTENT-005: No deterministic template expansion — P0

Hand-authored content must be able to instantiate parameterized families of workstations, queues, processes, incidents, workers, quests, and visual variants.

### G-CONTENT-006: Hot reload/editor workflow not proven — P2

Do not build a custom content IDE until YAML authoring friction is observed in real use.

---

## 7. Procedural/template generation

The game benefits from **bounded procedural generation**, not infinite random-world generation.

Missing reusable template families:

- facility shells and room kits;
- workstation types: manual, batch, buffer, inspection, service, transport;
- process topology: linear, branch, merge, parallel, batch, inspect/rework, queue/server;
- item families with shared state machines;
- demand profiles and arrival schedules;
- worker/persona/role variants;
- knowledge distributions;
- incidents and failure injections;
- quest skeletons;
- pattern exposure/reinforcement templates;
- visual variants.

All generated gameplay-affecting output must be reproducible from explicit template version + parameters + seed/content hash.

---

## 8. Player-authored sandbox systems

### G-SANDBOX-001: Player cannot yet author a real process model — P0

Need capture/edit/compare/apply workflow.

### G-SANDBOX-002: Player cannot yet author real automation rules — P0

Need small deterministic IR, rule editor, trace, validation, and failure behavior.

### G-SANDBOX-003: No A/B comparison/presets — P1

A core learning loop is "baseline → change → replay → compare". The UX should make this first-class.

### G-SANDBOX-004: Costs and constraints are too thin — P1

Automation requires meaningful tradeoffs: money, capacity, maintenance, training, permissions, reliability, complexity, coordination, or vendor dependence.

### G-SANDBOX-005: No later free-build facility sandbox — P2

Defer until authored facilities prove the ontology.

---

## 9. NPCs, characters, dialogue, and story

### G-STORY-001: Stable named cast is not yet a content system — P0

Need character definitions with roles, motivations, knowledge, authority, relationships, schedules, and dialogue hooks.

### G-STORY-002: NPC knowledge should matter mechanically — P1

NPCs should act on what they know, not global simulation truth.

### G-STORY-003: Dialogue/barks are not yet context-driven — P1

Need bounded, authored lines triggered by state, incident, relationship, quest beat, and observed outcomes.

### G-STORY-004: Vendor/integrator breadth remains limited — PARTIAL / P1

S033 makes Sam Rivera systemic in one restaurant side arc: authored contract/SLA/boundary terms drive deterministic cost, availability, observability, fallback, knowledge-ownership, support, replay, persistence, and client consequences. Cross-industry recurrence, negotiation, renewal, lock-in exit, and vendor portfolio behavior remain open.

---

## 10. Economy, authority, and organizations

### G-ORG-001: Decision tradeoffs need lightweight economics — P1

At minimum track wages/labor time, equipment purchase/lease, software/vendor cost, downtime, waste, throughput value, training, and maintenance.

### G-ORG-002: Player authority needs progression — P1

The player should not begin with the ability to redesign an entire company. Authority, budget, reputation, and organizational scope should expand through demonstrated competence.

### G-ORG-003: Coordination costs are under-modeled — P2

Teams, handoffs, approvals, conflicting goals, and local optimization become essential later.

---

## 11. Pattern-learning system

### G-PATTERN-001: Pattern recognition is not yet a runtime/progression system — P1

Need `PatternDefinition`, `PatternKnowledge`, and evidence history.

### G-PATTERN-002: PatternKit catalog is not imported through a stable metadata contract — P1

### G-PATTERN-003: Reinforcement/misuse planning needs explicit coverage gates — P1

All GoF patterns need main-story exposure. Broader PatternKit patterns can appear through optional specializations and incidents.

---

## 12. Second industry

### G-INDUSTRY-001: No warehouse reuse proof — P0 after restaurant authoring seam

The warehouse is the first test that the domain model is genuinely about work systems rather than dishwashing.

It must reuse:

- work items;
- queues/buffers;
- workstations/services;
- workers;
- roles/knowledge;
- process definitions;
- incidents;
- metrics;
- quests;
- automation primitives.

Any restaurant-specific concept leaking into those abstractions is a refactoring signal.

---

## 13. Architecture and maintainability

### G-ARCH-001: Stride client responsibilities are concentrated — P0

The client proof should be decomposed by responsibility while preserving a thin adapter to the simulation.

Likely seams:

- `InputRouter`
- `CameraController`
- `InteractionController`
- `ScreenRouter`
- `ModalStack`
- `HudPresenter`
- `PresentationWorld`
- `DeveloperToolsController`

Do not introduce empty framework layers with no concrete second use.

### G-ARCH-002: Scene/presentation code is too dish-station-specific — P1

Generalize only as the warehouse provides a second concrete example.

### G-ARCH-003: Presentation identifiers need an explicit compatibility boundary — P0

Saved simulation state must not depend on transient asset paths.

### G-ARCH-004: Save migrations/versioning need a long-term plan — P2

---

## 14. Validation

### G-TEST-001: Automated correctness exists, comprehension evidence is weak — P0

The product requires human first-hours playtests, not only UI automation.

Minimum questions:

- Can a new player move/interact without coaching?
- Can they describe the bottleneck in their own words?
- Can they explain reported state versus real state?
- Can they change a process and predict an outcome?
- Do they understand why an automation failed?

### G-TEST-002: Content lint/coverage tests absent — P0

### G-TEST-003: Real-asset performance budgets not yet proven — P1

---

## 15. Packaging, platform, and operations

### G-PLAT-001: Production settings/input persistence incomplete — P1

### G-PLAT-002: Build/release packaging needs alpha gate — P2

### G-PLAT-003: Crash reporting/telemetry policy not yet defined — P2

Telemetry should be opt-in/appropriate and never become a prerequisite for deterministic diagnostics.

### G-PLAT-004: Multiplayer remains intentionally deferred — P3

Do not let networking constrain the simulation or content architecture before the single-player campaign is proven.

---

## Immediate priority stack

1. Comfortable player navigation and interaction.
2. UI/input seams that remove scenario hotkeys from the production path.
3. Presentation catalog and first real 3D room proof.
4. YAML content compiler + validation.
5. Deterministic template expansion.
6. Player-authored process and automation tools.
7. Restaurant cast/narrative polish + light economy.
8. Human first-hours readiness gate.
9. Warehouse second-industry proof.
10. Pattern knowledge/Codex system and broader campaign expansion.
