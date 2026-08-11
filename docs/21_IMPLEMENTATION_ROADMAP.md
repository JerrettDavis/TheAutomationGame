# Implementation Roadmap

## Delivery gates

Work proceeds as reviewable vertical slices. Each gate must keep the prior headless path working.

1. **Runnable shell and authoritative clock** — a Stride window and headless executable consume the same deterministic simulation; commands and presentation snapshots cross the engine boundary.
2. **Manual dish episode** — the player can move plates and glasses through scrape, rack, wash, unload, dry, and restock while queues and service shortages remain visible.
3. **Spatial greybox** — replace the code-only process display with a selectable orthographic room without moving gameplay truth into Stride entities.
4. **Observation and process lens** — capture actual work, expose timing/queues, and let the player identify the glass bottleneck.
5. **Delegation and knowledge** — introduce a new worker whose behavior differs when tray and priority knowledge is omitted.
6. **Bounded automation and incident** — automate washer start, inject the sticky-ready signal, and support diagnosis plus a validated fix.
7. **Incident replay and regression** — retain the automation decision trace, replay captured inputs against both policies, and drive the complete episode through the native client.
8. **Unified system lenses** — project the same authoritative world through reality, process, state, knowledge, automation, runtime, and responsibility views.
9. **Measured layout improvement** — compare a baseline route with a U-shaped flow cell and carry the reduced travel cost into delegated cadence.
10. **Scenario configuration** — make rates, mix, capacities, worker timing, fault profile, knowledge, automation, layout, and demand reproducible headlessly.
11. **Architecture proof** — execute 100k synthetic actors headlessly, render a bounded representative subset, and restore a versioned deterministic JSON checkpoint.
12. **First-hours progression shell** — replay onboarding preference, evaluate eight consequence-based quests including a retryable composed-system reliability window, award capability-oriented XP/levels, and expose the arc through a tracker and journal.

Gate 12 is implemented alongside the existing greybox: onboarding pauses the native client until a replayable guidance choice is committed, and the complete quest/level arc is validated headlessly and through native UI automation.

Gates 1 through 10 are implemented as a code-native Stride greybox. The room is currently an asset-free orthographic floor plan: the player selects workstations and issues contextual commands while dish tokens and queues project authoritative simulation state. The process lens supports an evidence-based bottleneck hypothesis and demand validation. The player measures a 22-step baseline route, arranges a U-shaped flow cell, and validates the same state sequence at 10 steps; the layout also shortens delegated action delay. A deterministic new hire follows an explicit process specification; tests prove their priority behavior differs when the rush glass rule is omitted or transferred. An uncommon tray produces observable rework when its orientation knowledge is absent and succeeds after that fact is documented. The bounded washer controller exposes reported and physical readiness separately, halts after a sticky-ready incident, and validates a corroborated-state interlock. A bounded authoritative trace preserves the first divergence; the same recorded inputs reproduce the unsafe decision under the original policy and become a passing regression under the corrected policy. The state lens uses an authoritative transition/cause trace, while knowledge, automation, runtime, and responsibility lenses reveal distinct projections of the same snapshot. The validated scenario value and CLI now vary all documented headless inputs without introducing presentation dependencies. A native Windows UI driver completes the episode and verifies every lens through the real rendered client.

Scenario proof: focused tests and a CLI run vary initial dish mix, arrivals, rack capacity, washer timing, worker timing, deterministic fault risk, demand, initial knowledge, automation, and layout while preserving repeatability for a fixed seed.

Architecture proof: the synthetic runner executes 100k actors for 100 ticks with a deterministic checksum; god mode batch-renders 10k sampled states. A versioned JSON replay checkpoint restores seed, configuration, command chronology, pending future commands, random outcomes, and gameplay-significant state at a midpoint, then produces an identical continuation.

### Implemented player/system episode

Starting state: six dirty plates and two dirty glasses wait at the station; the washer is idle and dinner-rush demand is off.

Observable terminal outcome: the player explicitly moves a selected dish through every station state and returns it to service. Turning on the rush consumes glasses on a schedule and produces a causally explained shortage when no clean glass is available. Tutorial notifications identify each state consequence without introducing programming terminology.

Headless proof: the scripted runner performs the same command episode, reports every queue, and records tutorial/shortage notifications for a fixed seed.

Layout proof: the runner records baseline and validated route steps, while a focused test proves the U-shaped cell produces more delegated action opportunities with less accumulated travel.

Automation proof: the runner enables the unsafe reported-ready rule, records the first physical-state divergence, inspects the incident, installs the corroborated-ready policy, and proves that the repeated false signal is blocked without disabling recovery.

God/setup proof: setup changes enter through explicit configure/reset commands. The client can inject dirty work, provision clean service supply, reset the episode, pause time, and advance one deterministic tick so later features and incidents can be staged without editing simulation internals.

## Phase 0 — Repository and architecture spike

Goal: prove Stride + engine-independent .NET simulation structure.

Deliverables:

- solution/repository layout;
- CI build/test;
- pinned Stride version;
- headless executable;
- typed IDs and world container;
- command scheduler;
- deterministic random streams;
- simple actor/work simulation;
- Stride client consuming render snapshots;
- synthetic performance benchmark.

Exit gate:

> 100k simple headless actors can perform a deterministic synthetic work loop, and Stride can render a representative subset without owning simulation state.

## Phase 1 — Dish station greybox

Goal: first end-to-end manual process.

Implement:

- small restaurant room;
- player selection/camera;
- dishes/racks/workstations;
- worker actors;
- work queues;
- manual player actions;
- dirty -> clean process state;
- process lens;
- basic metrics.

Exit gate:

> Player can perform and observe a complete dishwashing episode and identify a real bottleneck.

Current presentation gate: the episode is playable on a selectable isometric sandbox floor with authoritative player movement, player-authored fixture placement, collision/safety validation, undo and preset reset, measured route consequences, depth-correct custom layouts, projected dish/actor state, Reality and Process views, pan/zoom/reset controls, save/replay coverage, and resolution-aware native UI validation through a 4K viewport. The next Phase 1 fidelity work is authored 3D art/navigation and richer workstation interaction animation; it does not require changing these simulation contracts.

## Phase 2 — Process definition and knowledge

Implement:

- process authoring UI;
- worker training/delegation;
- actor/org knowledge;
- assumptions;
- observation actions;
- expected story representation;
- new-hire scenario.

Exit gate:

> Player can make a process explicit, teach it to another worker, and see behavior differ when important tribal knowledge is omitted.

## Phase 3 — First automation

Implement:

- sensors;
- simple rule editor;
- machine control;
- decision/effect model;
- automation lens;
- failure injection;
- first incomplete-assumption incident.

Exit gate:

> Player can automate one bounded decision and experience, diagnose, and correct a failure caused by incomplete understanding.

## Phase 4 — Incident debugging and validation

Implement:

- command/event history;
- trace timeline;
- expected vs observed comparison;
- replay;
- scenario runner;
- validation cases;
- failure injection controls.

Exit gate:

> A player can recover the first divergence in a failed automated process and create a regression scenario.

## Phase 5 — Delegation and outsourcing

Implement:

- contractor/vendor entities;
- request/specification packet;
- assumptions/questions;
- delivery review;
- organizational understanding;
- automation debt.

Exit gate:

> Two players can give the same vendor different-quality definitions and receive materially different long-term outcomes despite similar implementation capability.

## Phase 6 — Warehouse expansion

Goal: prove ontology reuse at larger scale.

Implement:

- receiving;
- inventory;
- routing;
- conveyors;
- scanning;
- exception handling;
- larger spatial simulation;
- abstraction/pattern discovery.

Exit gate:

> Warehouse content reuses core systems without a bespoke second game architecture.

## Phase 7 — Programming layer

Implement:

- automation intermediate representation;
- code representation;
- editor/debugger;
- generated tests;
- pattern codex code examples.

Exit gate:

> Player can convert an existing visual automation into readable code, modify supported behavior, and see the same simulation outcome.

## Phase 8 — Multi-facility architecture

Implement:

- organizations;
- capabilities/contracts;
- service dependencies;
- distributed failure modes;
- ownership/coordination;
- architecture lens.

## Phase 9 — Content production and sandbox

- additional industries;
- scenario authoring tools;
- mod-friendly data content;
- balancing;
- onboarding;
- accessibility;
- production art/audio.

## Development rule

Every phase must leave behind a headless scenario/benchmark that proves the capability independently of presentation.
