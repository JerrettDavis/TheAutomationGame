# Initial Epics, Stories, and Tasks

This backlog is intentionally implementation-oriented enough to start work while preserving the design hierarchy.

## Epic 1 — Engine-independent simulation kernel

### Feature 1.1 — Deterministic world clock

**Story:** As a simulation system, I can schedule work against deterministic ticks so that results can be replayed.

Tasks:

- define `SimulationTick` value type;
- implement clock;
- implement scheduler/phase registry;
- add deterministic test;
- benchmark empty tick overhead.

### Feature 1.2 — Typed simulation identity

Stories:

- create typed actor/resource/process/facility IDs;
- implement allocation/reuse policy;
- prevent client engine IDs from entering domain APIs.

### Feature 1.3 — Seeded random streams

Stories:

- register named random streams;
- snapshot/restore stream state;
- deterministic replay test.

## Epic 2 — Work/process kernel

### Feature 2.1 — Process definitions

Stories:

- define process state/transition model;
- load one hard-coded dish process;
- validate illegal transitions;
- serialize definition from YAML.

### Feature 2.2 — Work items and queues

Stories:

- create work-item store;
- create queue capability;
- enqueue/dequeue with priority;
- expose queue metrics.

### Feature 2.3 — Actor work execution

Stories:

- actor accepts assignment;
- actor reserves required resource;
- actor spends modeled work duration;
- completion transitions work item;
- actor releases resources.

## Epic 3 — Headless runner

Stories:

- load scenario from command line;
- run N ticks or simulated duration;
- output summary metrics;
- accept seed;
- write snapshot;
- replay commands;
- benchmark 100k actor synthetic scenario.

## Epic 4 — Stride presentation shell

### Feature 4.1 — Bootstrap

Tasks:

- create Stride 4.3 .NET 10 solution;
- reference simulation libraries;
- create bootstrap service;
- verify no reverse dependency.

### Feature 4.2 — Render snapshots

Stories:

- simulation emits actor/object presentation state;
- client pools presentation objects;
- interpolate actor movement;
- benchmark visible-count tiers.

### Feature 4.3 — Camera/selection

Stories:

- orthographic pan/zoom;
- rotate camera;
- select actor/machine;
- focus selection;
- inspect simulation ID.

## Epic 5 — Dish station first playable

Stories:

- dirty dish arrivals;
- scrape station;
- rack capacity;
- dishwasher cycle;
- drying/return station;
- clean-glass consumption;
- dinner-rush demand;
- manual player work;
- NPC worker work;
- metrics panel.

## Epic 6 — Process lens

Stories:

- show active process stage;
- show queue arrows;
- show blocked stage;
- display stage timing;
- identify bottleneck candidate.

## Epic 7 — Knowledge and specification

Stories:

- record observation;
- represent worker-local fact;
- represent documented fact;
- teach worker from process specification;
- worker improvises based on tribal knowledge;
- knowledge loss when worker leaves.

## Epic 8 — Automation

Stories:

- sensor input model;
- rule/decision definition;
- effect request;
- automatic machine control;
- manual fallback;
- stuck sensor failure;
- assumption record linked to rule.

## Epic 9 — Incident/replay

Stories:

- capture meaningful event timeline;
- expected story view;
- observed story view;
- locate first divergence;
- inject failure;
- save regression scenario.

## Epic 10 — Quest/scenario framework

Stories:

- load scenario content;
- evaluate condition-based objectives;
- support hidden conditions;
- support discovery triggers;
- track multi-dimensional completion outcome;
- author first seven dish-station quests.

## Epic 11 — Asset/content pipeline

Stories:

- establish asset source folders;
- Git LFS configuration;
- asset provenance manifest;
- placeholder environment kit;
- modular worker model/rig spike;
- Stride import conventions;
- content compiler and schema validation.

## Epic 12 — Performance foundation

Stories:

- BenchmarkDotNet microbenchmarks;
- headless macro benchmark harness;
- allocation counters;
- benchmark result history;
- first large-visible-object Stride benchmark.

## Spike backlog

- Stride instancing/presentation strategy at 10k+ visible simple objects;
- Stride UI framework suitability vs custom/third-party approach;
- navigation/pathfinding approach for hundreds/thousands of workers;
- large-world coordinate strategy;
- content hot reload;
- animation system for modular stylized workers;
- scripting sandbox options;
- persistence binary format after prototype.
