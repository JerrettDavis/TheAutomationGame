# Product Roadmap

> Capability-gated roadmap from the current greybox to a complete product.

This roadmap is ordered by **risk retirement and player value**, not by subsystem purity. Every gate should be demonstrable in a playable build.

## N0 — Proven Greybox

**Status:** treat as achieved unless the current repository contradicts it.

Proof:

- deterministic simulation runs without Stride;
- dish-station slice playable;
- first-shift quest/reliability arc works;
- save/resume works;
- lenses and placement exist;
- build/test/headless/UI-smoke paths exist.

Exit condition: next work no longer treats the vertical slice itself as the missing proof.

---

## N1 — Comfortable Interaction

**Player promise:** "I can enter the game and naturally move, inspect, and work."

Deliverables:

1. semantic input action map;
2. WASD direct movement;
3. click-to-move retained through the same authoritative movement commands;
4. `E` context interaction and `F` inspect;
5. mouse camera pan/zoom/recenter;
6. context prompt and interaction highlight;
7. scenario/debug shortcuts moved behind developer tools;
8. basic input/settings persistence.

Proof:

- new player completes the opening manual work without being told hotkeys;
- deterministic replay of the same world commands remains stable.

---

## N2 — Presentation Seam

**Player promise:** "This looks and reads like a place, not a visualization."

Deliverables:

1. presentation catalog resolving stable IDs to visual/audio assets;
2. fallback presentation for every required entity;
3. first authored 3D restaurant room;
4. washer/counter/rack/service-area 3D replacements;
5. character rig/pawn replacement proof;
6. walkability/obstacle projection;
7. near/mid/far camera readability rules;
8. first animation and audio feedback set.

Proof:

- one room can switch from placeholder to authored presentation without simulation changes;
- headless tests remain identical.

---

## N3 — Content Platform

**Designer promise:** "A quest/scenario can be changed without recompiling simulation code."

Deliverables:

1. YAML content schema v1;
2. compiler/loader into immutable runtime definitions;
3. stable IDs and reference validation;
4. content hash/versioning;
5. first-shift quest externalized;
6. dish-station scenario externalized;
7. deterministic template expansion;
8. dedicated content tests;
9. useful validation diagnostics with file/line context.

Proof:

- a content-only change adjusts a quest/scenario;
- invalid content fails before gameplay with actionable diagnostics;
- the externalized dish scenario reproduces its reference deterministic outcome.

---

## N4 — Player Tools

**Player promise:** "I can model work, change it, automate it, and prove what happened."

Deliverables:

1. process capture model;
2. process editor v1;
3. baseline/change comparison;
4. small deterministic automation IR;
5. automation editor v1;
6. automation trace and failure explanation;
7. presets/checkpoints;
8. replay/A-B comparison UX.

Proof:

- player records a manual process, changes one step, replays it, then automates one decision and can explain the resulting behavior.

---

## N5 — Restaurant Production Slice

**Player promise:** "The first chapter feels like a coherent game and teaches systems thinking without announcing a lesson."

Deliverables:

1. stable restaurant cast;
2. contextual dialogue/barks;
3. authored restaurant art/audio pass;
4. lightweight shift economics;
5. two-station expansion;
6. Strategy discovery and Codex tease;
7. vendor/outsourcing side arc;
8. revised onboarding;
9. at least five first-hours human playtests;
10. issue closure/retest pass from observation.

Proof:

- first-time players can complete the chapter and describe the core lessons in their own language.

---

## N6 — Warehouse Reuse Proof

**Architecture promise:** "The game models work systems, not a restaurant special case."

Deliverables:

1. receiving facility definition;
2. packages/pallets/cases as new item family;
3. receiving/storage/inspection/hold workstations;
4. walkable warehouse slice;
5. warehouse workers/roles/knowledge;
6. failure/exception flow;
7. Command exposure;
8. Chain of Responsibility exposure;
9. Iterator + Composite exposure;
10. Factory Method exposure;
11. reuse audit and only-then generalization.

Proof:

- warehouse scenario uses common domain/content primitives with no dish-specific simulation concepts;
- restaurant tests remain green.

---

## N7 — Pattern Learning System

**Learning promise:** "The game notices reusable structures I have experienced and helps me name and transfer them."

Deliverables:

1. `PatternDefinition`;
2. `PatternEvidence`;
3. `PatternKnowledge` mastery lifecycle;
4. Codex page driven by player history;
5. PatternKit metadata importer;
6. pattern coverage validation;
7. first Strategy full discovery/reinforcement arc;
8. warehouse pattern evidence.

Proof:

- the same player profile can show when/where a pattern was encountered, applied, named, transferred, and stress-tested.

---

## N8 — Retail: Systems Need Interfaces

Primary GoF spine:

- Adapter;
- Decorator;
- Facade;
- Proxy;
- Memento.

Product capabilities added:

- external providers;
- money/transactions;
- permissions;
- rollback/suspend/resume;
- loyalty/promotions;
- vendor compatibility.

---

## N9 — Factory: Systems Become Families

Primary GoF spine:

- Builder;
- Prototype;
- Abstract Factory;
- Bridge;
- Visitor.

Capabilities added:

- commissioning/configuration;
- machine families;
- recipes;
- inspections;
- maintenance;
- richer physical constraints.

---

## N10 — Logistics: Systems Become Networks

Primary GoF spine:

- Mediator;
- Singleton;
- Flyweight.

Optional pattern specialization expands strongly into:

- enterprise integration;
- messaging;
- distributed reliability;
- cloud patterns.

Capabilities added:

- multiple sites;
- asynchronous messaging;
- schedules/routes;
- external partners;
- network failures.

---

## N11 — Safety-Critical Operations: Architecture Becomes Responsibility

No requirement to introduce new GoF patterns. Instead stress-test prior knowledge.

Capabilities added:

- safety constraints;
- human override/manual task gates;
- audits;
- alarms;
- incident command;
- consequences of hidden complexity;
- recovery drills.

Patterns should begin earning **Stress-Tested** mastery here.

---

## N12 — Financial/Transactional: Preserve Truth

Primary GoF spine:

- Interpreter.

Strong optional architecture/reliability curriculum:

- Unit of Work;
- CQRS;
- Event Sourcing;
- Audit Log;
- Materialized View;
- Idempotent Receiver;
- Inbox/Outbox;
- Compensating Transaction.

Capabilities added:

- rule language;
- transaction boundaries;
- consistency models;
- reconciliation;
- compliance/audit.

---

## N13 — Software/Platform: Rosetta

**Payoff:** programming becomes another representation of systems the player already understands.

Deliverables:

- code lens/editor;
- safe restricted execution model;
- domain/process/automation/code correspondence;
- conventional C# examples;
- PatternKit representations;
- all 23 GoF patterns revisited in software;
- architecture refactor missions;
- test/CI/release concepts.

Core Rosetta view:

```text
REALITY
  ↓
PROCESS
  ↓
STATE / INTERACTION
  ↓
AUTOMATION
  ↓
CODE
  ↓
PATTERNKIT
```

---

## N14 — Organization Scale

Capabilities:

- multiple teams/facilities;
- budgets and portfolio choices;
- hiring/training;
- delegation;
- governance;
- standards;
- vendor portfolio;
- architecture boundaries;
- coordination cost;
- local versus global optimization.

Player perspective may become increasingly managerial, but embodied local play remains available where useful.

---

## N15 — Campaign Alpha

Exit criteria:

- complete main campaign playable end to end;
- all 23 GoF patterns have planned and validated main-story exposure;
- optional specialization content sufficiently representative;
- no progression dead ends;
- save migration policy established;
- accessibility/settings baseline complete;
- performance with production art inside budget;
- telemetry/crash policy decided;
- external playtest cohort can complete campaign.

---

## N16 — Beta

Focus:

- comprehension and pacing;
- balance/economy;
- narrative continuity;
- tutorial removal where players no longer need it;
- content volume/polish;
- performance/compatibility;
- localization readiness;
- achievements/completion hooks if desired;
- packaging/distribution.

No large unproven architecture changes should begin here.

---

## N17 — 1.0

1.0 is a **coherent, replayable systems-and-automation game**, not a promise to implement every imaginable industry or PatternKit side quest.

Ship when:

- main campaign is complete and understandable;
- sandbox/replay provides meaningful replay value;
- production assets/audio/settings are complete;
- critical bugs and progression blockers are gone;
- performance targets are met on minimum hardware;
- content authoring is stable enough for post-launch expansion.
