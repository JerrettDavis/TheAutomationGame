# Architecture Evolution Plan

> Refactor only at seams required by the next playable capability, and use the warehouse as the second concrete proof before broad generalization.

## Non-negotiables

1. `Automation.Simulation` remains authoritative and engine-independent.
2. The Stride client renders state, captures intent, and submits commands. It does not own gameplay truth.
3. Determinism is preserved for all gameplay-affecting systems.
4. Stable domain/content IDs are independent of presentation asset paths.
5. New abstractions require an immediate concrete use.
6. Genericization across industries waits for at least two concrete examples unless the abstraction is already mathematically/domain-obviously generic.

## Current architectural pressure

The original client proved the vertical slice quickly, which is the correct prototype choice. The next phase creates enough interaction surfaces that one game/controller class and one scene renderer will become expensive to extend safely.

Do not perform a "clean architecture rewrite." Extract responsibilities one at a time while delivering N1/N2 sessions.

## Near-term seams

### InputRouter

Owns:

- device state → logical actions;
- input contexts;
- binding lookup;
- repeat/edge semantics where needed;
- developer-action isolation.

Does not own:

- quest state;
- movement legality;
- camera state beyond emitting camera actions.

### CameraController

Owns:

- pan/zoom/recenter intent;
- camera bounds;
- follow target;
- semantic zoom state.

Does not decide simulation visibility/knowledge.

### InteractionController

Owns presentation/application resolution of:

- selected target;
- nearby interactables;
- context action list;
- conversion of chosen action into application/simulation commands.

The command handler remains authoritative about whether the action is valid.

### ScreenRouter + ModalStack

Owns:

- which top-level screen is active;
- modal ordering;
- back/cancel semantics;
- input-context activation for screens.

Migrate existing briefing/journal/settings/editor surfaces incrementally.

### HudPresenter

Projects simulation/progression/application state into compact view models.

Do not let HUD widgets query arbitrary globals.

### PresentationWorld

Owns projection from world snapshots/domain IDs to scene objects.

Sub-responsibilities can emerge later:

- entity view registry;
- presentation catalog;
- interpolation;
- selection/highlight;
- animation-state projection;
- LOD.

### DeveloperToolsController

Moves prototype conveniences out of production input:

- force quest step;
- inject incident;
- toggle raw diagnostics;
- jump time;
- dump snapshot;
- scenario-specific shortcuts.

These remain valuable, just intentionally separated.

## Do not create yet

Do not add assemblies/types merely because they sound architecturally clean:

- generic workflow engine with no player-authored process yet;
- universal industry abstraction before warehouse;
- plugin framework before a plugin/provider use case;
- ECS rewrite because actor counts are large;
- multiplayer synchronization layer;
- universal script runtime;
- custom UI framework before the UI spike.

## Implemented player-owned process capture boundary

S021 introduces the first concrete player-authored process artifact without creating a generic workflow engine. Engine-neutral capture IDs, artifact IDs, ordered steps, provenance, ownership, immutable baseline/current versions, and lifecycle events live in `Automation.Domain`. Explicit start/complete commands enter `DishStationWorld`; only successful `PlayerWork` actions are captured after authoritative validation.

Machine cycles, service demand, automation, new-hire work, and rejected attempts remain execution evidence rather than authored manual steps. A completed capture produces baseline v1 and current v1 from the same immutable ordered sequence. Replay reconstructs active and completed capture state. See [ADR-0009](adr/0009-player-owned-process-capture.md).

S021 deliberately stops before graph inference, routing/assignment edits, applying changed versions, UI, and cross-industry abstractions. Those begin with S022 and later reuse proof.

## Implemented process-editor boundary

S022 extends an owned process through explicit draft commands rather than mutable client state. A draft is copied from current version `N`; stable steps may be reordered or assigned to player/new hire, and a closed captured-order/plates-first/glasses-first routing policy may be selected. Apply validates sequence and state compatibility, derives immutable current `N+1`, preserves baseline, and marks the artifact as the active delegated-work definition. See [ADR-0010](adr/0010-versioned-process-drafts-and-application.md).

The new-hire executor consults the applied version for supported assignments and routing. This is a concrete process tool, not the generic workflow engine warned against below: S022 cannot invent/delete actions, branch, loop, or define arbitrary roles. The client editor is a paused modal projection over snapshots and replay-serialized commands.

## Content compilation boundary

Preferred dependency direction:

```text
YAML / PatternKit catalog
        ↓
Automation.Content.Compiler (or equivalent tooling)
        ↓
validated immutable content definitions
        ↓
Automation.Content runtime catalog
        ↓
Simulation / progression scenario construction
```

Whether compiler and runtime remain in one assembly initially is less important than preserving the conceptual boundary and tests.

The simulation should not parse YAML.

## Presentation boundary

```text
Domain/Content entity ID
   +
Presentation ID
   +
Authoritative state snapshot
        ↓
Presentation Catalog
        ↓
Stride assets / scene / audio / VFX
```

Save files persist stable IDs and semantic state. They must not require the same model/material path to load.

## Automation IR

Build the smallest semantics that support real current play.

Initial conceptual algebra:

```text
ValueRef
  Constant
  MetricRef
  EntityPropertyRef
  KnowledgeRef

Predicate
  Compare(ValueRef, Operator, ValueRef)
  And
  Or
  Not

Effect
  IssueCommand
  SetPolicy
  Notify

Rule
  Id
  Enabled
  Condition
  Effect(s)
  Priority/order if required

RuleEvaluationTrace
  observed inputs
  predicate results
  chosen effects
  command outcomes
```

Do not begin with arbitrary C# or Lua scripting. The restricted IR is easier to:

- validate;
- visualize;
- trace;
- replay;
- save;
- teach;
- later translate to/from code.

When code unlocks, code may compile to/drive the same safe capability surface rather than receive unrestricted simulation access.

S023 implements the first closed slice of this algebra in `Automation.Domain`: typed Boolean/integer values, constants and named observables, comparisons, `all` / `any` / `not`, stable enabled rules, ordered effects, validation diagnostics, and immutable evaluation traces. The initial capability surface is deliberately narrower than the conceptual algebra: four dish-station observables and one `IssueDishAction` effect.

Both reported-ready and corroborated-ready washer policies compile to stable rules. The pure evaluator selects effects; `Automation.Simulation` executes them through its existing authoritative `Perform` boundary and attaches command outcomes to the trace. Captured-incident replay calls the same evaluator, and the world exposes only a bounded trace history. See ADR-0011.

S024 adds one concrete player authoring path rather than a generic graph framework. Replay-serialized commands mutate a simulation-owned draft for the stable player washer rule; validation requires rack presence, at least one readiness signal, and the closed Start Washer effect. Apply compiles the draft to IR and makes it the active live/replay rule. The paused client modal projects draft diagnostics and the latest authoritative trace but owns no rule state. The canonical first shift now creates and refines that artifact instead of selecting policy shortcuts. Presets, comparison, multiple rules, and priority remain later work. See ADR-0012.

S025 makes paired experimentation authoritative. Baseline and variant slots preserve immutable applied rules. A comparison command creates two isolated worlds with the same scenario, seed, horizon, demand, and deterministic support work, installs one preset in each through the S024 capability, and records throughput, shortages, starts, incidents, prevented requests, and first-divergence evaluator evidence. Reliability is ranked before shortage and throughput gains, and the live world is not used as either trial. The client presents this evidence without calculating it. See ADR-0013.

## Process model

Separate three concepts:

1. **Process definition:** intended routing/work structure.
2. **Runtime execution:** actual items/workers/stations and events.
3. **Captured evidence:** what the player observed/recorded about execution.

This supports mismatches between procedure and reality.

## Pattern knowledge boundary

Do not put design-pattern names into simulation entities.

```text
Simulation event/history
      ↓
Pattern evidence recognizer / quest logic
      ↓
Player PatternKnowledge
      ↓
Codex/progression
```

A Strategy-like routing policy is mechanically a policy whether or not the player has named `pattern.strategy`.

## NPC architecture

NPC behavior should be bounded by:

- role/authority;
- knowledge state;
- current assigned work;
- local goals/priorities;
- deterministic decision policies where gameplay-affecting.

Presentation animation/dialogue can vary aesthetically, but decisions used in replay/scoring must remain deterministic from authoritative inputs.

S026 establishes the content identity boundary before NPC behavior exists. A character owns a stable engine-neutral ID plus authored motivation, fact/authority IDs, directional typed relationships, and primary/fallback presentation references. Scenarios own their cast roster; quests reference an explicit nonempty subset of that roster. Runtime adapters preserve IDs, and clients resolve display names/roles from the catalog, so future dialogue or presentation changes cannot silently change authoritative quest involvement.

S027 adds a narrow causal dialogue seam. The simulation emits typed, engine-neutral narrative events at real world transitions; it does not own dialogue text or speaker selection. Character content binds short lines to an event and participating quest, with priority and cooldown metadata. A deterministic content router chooses the line, while presentation resolves the stable speaker ID and renders it. This is deliberately not a generic event bus, conversation graph, or NPC behavior system.

S028 completes the first-shift narrative ownership boundary. Scenario content owns chapter title, workplace briefing pages, shift summary, and debrief questions; quest content owns its situation/outcome/discovery arc; character content owns contextual lines. The existing client surfaces consume these compiled definitions. Simulation notifications still describe immediate authoritative consequences, but use workplace language and named participants rather than explaining engine/client implementation. The deterministic reference run uses the same production commands as play and contains no supply, reset, fault-injection, or legacy automation-policy shortcut.

S029 adds one bounded first-shift economy projection rather than a general ledger. Scenario content owns engine-neutral integer value/cost rates. `DishStationWorld` alone accumulates successful player and worker actions, enabled-worker ticks, rework, shortages, unsafe automation incidents, completed dishes, and the flow-cell purchase; live snapshots calculate an explainable summary and the completed shift report freezes that same value. Replay/save reconstructs it from the existing scenario, command journal, and ticks. The client and headless runner display the snapshot but cannot post transactions or invent costs. A same-seed 120-tick staffed linear/flow-cell comparison is a concrete validation episode, not a reusable business-accounting framework.

S030 adds a separate, bounded two-station routing episode after the first shift. Content authors exactly the main dish room and patio service station, their local dish demand, initial inventory, initial routing choice, and a shared trial horizon. `TwoStationRoutingWorld` authoritatively owns policy selection, copy actions, deterministic trials, metrics, and replay; each explicit trial runs two validated `DishStationWorld` instances and reports their real shortage/economy consequences. The Stride board pauses the first-shift projection while open and sends only set/copy/run commands. This is intentionally not a generic policy registry, experiment framework, career-save extension, or named Strategy concept; those abstractions wait for S031/S032 evidence and another recurring use.

## Save evolution

Before campaign alpha, saves should include:

```text
SaveSchemaVersion
GameBuild/ContentVersion
ContentHash or compatible manifest reference
World state
Progression state
Player-authored process/automation artifacts
PatternKnowledge
Economy/organization state
Presentation-independent identifiers
```

Migration rules:

- schema versions are explicit;
- migrations are deterministic and tested;
- content incompatibility fails clearly rather than corrupting state;
- disposable pre-alpha save compatibility can be intentionally broken only with an explicit decision log entry.

## Warehouse two-use rule

When implementing warehouse:

1. build using the current most honest common abstractions;
2. tolerate some duplication while the second shape becomes visible;
3. record dish-specific naming/assumptions encountered;
4. only after both flows work, extract the common concept;
5. run restaurant + warehouse tests after each extraction;
6. document what remains industry-specific.

Likely genuinely shared concepts:

- work item/entity identity;
- state machines;
- queues/buffers;
- workstation/service capacity;
- workers/roles;
- commands/events;
- metrics;
- process graphs;
- knowledge/observation;
- incidents;
- quest conditions;
- automation policies.

Likely industry-specific concepts should stay in content/extensions until proven otherwise:

- dish racks;
- pallets;
- payment/refund semantics;
- machine recipes;
- financial transaction rules.

## Architecture fitness tests

As seams stabilize, add tests asserting properties rather than internal class layouts:

- simulation project has no Stride dependency;
- content compilation is deterministic;
- presentation replacement does not alter scenario outcome;
- same seed/content produces same authoritative event digest;
- save round-trip retains player-authored automation/process state;
- both industries execute through shared queue/workstation primitives where intended.
