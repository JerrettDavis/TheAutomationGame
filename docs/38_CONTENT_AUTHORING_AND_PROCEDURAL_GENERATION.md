# Content Authoring and Procedural Generation

> Data-driven authoring plan for scaling from one hard-coded episode to a campaign without sacrificing determinism.

## Firm format decision

Use:

- **YAML** for human-authored game content;
- immutable, validated **C# runtime definitions** after compilation/loading;
- **JSON** for saves, replays, diagnostics, exported traces, and interchange where appropriate.

Why YAML for authored content:

- readable diffs;
- comments;
- concise repeated structures;
- friendly to agents/designers;
- schema validation can still be strict.

Do not allow arbitrary YAML object graphs to leak into simulation. Content is compiled through a controlled boundary.

## Operative content tree

```text
content/
  industries/
  facilities/
  items/
  workstations/
  processes/
  scenarios/
  quests/
  characters/
  dialogue/
  incidents/
  patterns/
  economy/
  presentations/
  templates/
    facilities/
    workstations/
    processes/
    incidents/
    quests/
    characters/
```

Subfolders may be organized by industry while IDs remain globally stable.

## Compilation pipeline

```text
YAML files
  ↓ parse
Raw DTOs
  ↓ schema/version validation
Reference graph
  ↓ ID/reference validation
Template expansion
  ↓ deterministic normalization
Semantic validation
  ↓
Immutable runtime definitions
  ↓
Content manifest + content hash
```

Diagnostics must include source file and useful path/line information when parser support permits it.

## Stable IDs

Use semantic IDs, not asset paths or display strings.

Examples:

```text
industry.restaurant
facility.restaurant.rossis.back-of-house
item.restaurant.dish.glass
workstation.restaurant.washer.standard
process.restaurant.dishwashing.standard
scenario.restaurant.first-shift
quest.restaurant.first-shift.clock-in
character.restaurant.avery-chen
incident.restaurant.washer.sticky-ready
pattern.strategy
presentation.workstation.dish-washer.standard
```

IDs are persisted. Display names may change freely.

## Core definition shapes

### Implemented S014 schema/compiler boundary

Schema v1 is implemented as a strict YAML bundle headed by `schema_version: 1`. YamlDotNet is confined to `Automation.Content`: source data enters private mutable DTOs, validates, and exits as public immutable records and `ImmutableArray` collections. YAML types never enter simulation or save/replay contracts.

The first compiler covers exactly the eight S014 definition kinds: industry, facility, item, workstation, process, scenario, quest, and character. Stable definition IDs use required type prefixes and one global namespace. Role and presentation IDs are checked for semantic syntax/prefix but remain opaque until their own catalogs migrate.

Compilation performs:

1. strict YAML parsing, unknown-key and duplicate-key rejection;
2. explicit schema-version compatibility rejection;
3. required-field, semantic-ID, state-token, and step-token checks;
4. global duplicate-ID and typed reference-graph validation;
5. workstation item-state and process step/route validation;
6. cycle rejection unless `allow_cycles: true` is explicitly authored;
7. supported quest metric/operator validation;
8. deterministic sorting/canonical encoding and SHA-256 manifest generation.

Diagnostics contain source and semantic path; parser diagnostics also contain line/column. The CLI entrypoint is `Automation.Headless --compile-content <path>`. The contract and one nine-definition proof fixture live under `content/`.

### Implemented S015 first-shift narrative boundary

The production first-shift questline is authored in `content/restaurant/first-shift.yaml` and embedded into `Automation.Content` as a deployable compiled resource. `DishStationFirstHoursContent` loads it through `ContentCompilerV1`; the client no longer owns quest titles, situations, observable outcomes, discoveries, unlock rationale, visible rewards, ordering, or guided tutorial-stage sentences.

Schema-v1 quests may carry an optional complete `narrative` block with:

- a stable runtime quest token and positive authored sequence;
- player-facing title, situation, discovery, and unlock rationale (the quest `objective` remains the observable outcome);
- positive experience and semantic capability reward descriptors;
- ordered tutorial steps with stable IDs, text, and optional logical input-action tokens.

The generic compiler validates narrative shape, step uniqueness within a quest, token syntax, and `{binding}` placeholder contracts. The first-shift adapter additionally requires one-to-one runtime quest coverage, sequences `1..8`, globally unique stage IDs, known capabilities, and exact reward coherence with authoritative progression rules. The client resolves authored logical input-action tokens through the active binding profile, so remapped keys remain truthful without physical-key text entering content.

The simulation's existing outcome transitions remain authoritative and unchanged in S015. Scenario configuration was deferred to S016 below; generalized quest evaluation, localization, and hot reload remain later sessions. Automated coverage proves that a YAML-only guidance edit reaches the exact client presentation method while the deterministic quest run remains unchanged.

### Implemented S016 dish-scenario boundary

The first-shift scenario's concrete operating inputs are now authored in the production YAML `dish_station` block: dirty and available inventories, arrival timing/mix, rack capacity, washer timing, linear/flow-cell worker cadence, sticky-ready threshold and deterministic risk, demand kind/rate, initial rush and worker state, transferred knowledge, automation policy, and layout.

`ContentCompilerV1` validates the complete block and emits an engine-neutral `DishStationScenarioConfiguration`. That value moved to `Automation.Domain`; `Automation.Simulation` still owns execution, accepts only an explicit validated configuration, and remains unaware of YAML or compiler types. The production client and headless composition roots use `DishStationFirstHoursContent.ScenarioConfiguration`. Headless tuning flags apply `with` overrides over the compiled value, so there is no second production copy of first-shift defaults.

The resolved configuration is still recorded in `DishStationReplaySave`, preserving save/replay determinism. A fixed-seed equivalence test independently reconstructs the pre-S016 reference values, executes the same 250-tick command schedule through both configurations, and compares snapshots, notifications, replay data, restored configuration, and future state. A content-only capacity change also proves that the compiler produces a changed configuration and manifest without simulation edits.

S016 does not externalize dish transition algorithms, topology/pathfinding, new workstation primitives, generalized quest evaluation, or template expansion. Those remain authoritative code or later roadmap sessions.

### Implemented S017 content-validation gate

`Automation.Content.Tests` is a dedicated solution-level test project for the authoring boundary. Compiler-only schema tests moved out of the broad integration suite, and checked-in production/fixture bundles are compiled on every solution test run with pinned deterministic manifests.

The compiler now also enforces two cross-field semantics that syntax/reference checks cannot prove:

- every workstation carries an explicit semantic `presentation_fallback` ID;
- every process route connects workstations accepting a common item and the source output state equals the destination input state.

The first-shift adapter validates authored tutorial beats against the authoritative engine-neutral `DishTutorialStage` enum, which now lives in `Automation.Domain`. Missing stages and authored stage IDs that runtime can never reach both fail before client presentation.

Durable mutation seeds in `content/fixtures/schema-v1/invalid/cases.json` cover malformed IDs, global duplicates, unknown and wrong-type references, invalid process transitions, invalid scenario configuration, missing presentation fallback, and unreachable quest beats. Each case asserts the exact diagnostic source, semantic path, and targeted message. This provides a build-breaking content lint gate without introducing an editor, asset loader, or generalized quest engine.

### Implemented S018 deterministic template expansion

`ContentTemplateCompilerV1` implements a bounded `template_schema_version: 1` YAML envelope with a stable `template.*` ID, positive template version, typed parameter declarations, finite seeded variant declarations, and one ordinary schema-v1 content body. `IContentTemplateV1` is the narrow expansion seam; `ContentTemplateExpansionResultV1` returns normalized expanded YAML, the compiled immutable catalog, immutable provenance, and a deterministic expansion hash.

Parameters use explicit `{{parameter:name}}` placeholders and must match the declaration map exactly. Variants use `{{variant:name}}`, require a named seed, and select only from their declared normalized options using SHA-256 over template ID/version, seed, and field name. Fixed templates reject unnecessary seeds. Parameter maps and provenance are ordinally sorted, so caller dictionary order does not affect bytes or hashes.

Expansion never bypasses validation: generated YAML enters `ContentCompilerV1`, and the expansion hash covers template identity/version, named seed, normalized parameters, selected variants, and the resulting content hash. The proof template under `content/templates/proofs/` demonstrates a parameterized facility/capacity and one explicitly variable demand-kind field. Fixed seeds select known distinct values while all nondeclared catalog fields remain logically identical.

S018 does not provide general-purpose scripting, loops, conditions, includes, nested templates, or concrete workstation/incident families. Those constraints keep generated content auditable; S019 begins the first concrete template family.

### Implemented S019 workstation template family

Schema-v1 workstations may now carry exactly one immutable behavior block: `manual`, `batch`, `buffer`, `inspection`, or `service`. The compiler validates each family's tokens and ranges and also checks its input/output states against current dish-station semantics. Behavior data is included in catalog hashes while catalogs authored before S019 retain their existing hashes when no behavior is present.

Five fixed, auditable family templates live under `content/templates/workstations/`. They expose only identity plus settings the current behavior actually supports: batch cycle ticks, FIFO buffer capacity, and service demand kind/interval. The authoritative washer currently holds one dish, so batch capacity is fixed to one instead of advertising a setting the simulation would ignore. Manual scrape, non-mutating state-count inspection, FIFO ordering, and dish-state transitions are likewise explicit rather than implied by a generic script.

`DishStationWorkstationTemplateAdapter` is deliberately narrow. It maps batch timing, rack capacity, and service demand into `DishStationScenarioConfiguration`; the existing `DishStationWorld` commands and rules still perform every consequential transition. Tests instantiate the batch and buffer templates together and prove cycle timing and capacity rejection in the authoritative world, then cover manual work, inspection, and service consumption through their existing command paths.

Transport is not a supported S019 family. The current topology records walking/handling distance but does not model queued work items moving between workstation queues. Adding a transport template now would create unauthoritative metadata, so the family registry returns an explicit unsupported reason until a later ontology-reviewed movement primitive exists.

S019 does not add a generic workstation executor, new dish states/actions, transport/pathfinding behavior, incident templates, recursive composition, or a production rebinding/editor surface. Incident families begin at S020.

### Implemented S020 incident template family

Schema v1 now has a ninth definition kind, `incident.*`. Each incident declares its industry, display name, nonnegative trigger tick, dish-station scope, immediately observable symptom, evidence that reveals the underlying truth, recovery observation, positive duration, and exactly one typed effect. The closed S020 effect set is process delay, rack-capacity loss, sticky reported-ready sensor, blocked washer, new-hire absence, and accelerated service demand.

Six checked-in templates under `content/templates/incidents/` expose only family-relevant typed parameters. Each declares a finite seeded `trigger-tick` variant; identical template/version/parameter/seed input therefore reproduces expanded bytes, content and expansion hashes, trigger selection, authoritative timeline, and trace. A changed seed can change only that declared trigger field.

The runtime seam follows [ADR-0008](adr/0008-engine-neutral-incident-lifecycle.md). A closed `DishStationIncidentEffect` union lives in `Automation.Domain`; content adapts compiled definitions to scheduled domain incidents; and `TriggerDishStationIncidentCommand` is the only consequential entry point. `DishStationWorld` owns activation, bounded duration, recovery, and trace snapshots. Replay serialization reconstructs incidents that are scheduled, active, or recovered without any YAML dependency in simulation.

Effects alter existing authoritative semantics rather than presentation: washer completion ticks, effective rack capacity, reported-versus-physical readiness, washer availability, delegated-worker cadence, or demand cadence/kind. The headless `--run-incident` expansion path prints the deterministic start/recovery trace for a checked-in template.

S020 does not add arbitrary conditions/effects, conditional recovery, overlapping instances of one family, an incident editor, message duplication, provider outages, quality/rework, or resource-shortage families. Those require later concrete sessions and ontology review; S021 begins process capture rather than expanding this incident union.

### IndustryDefinition

```yaml
id: industry.restaurant
display_name: Restaurant
unlocks: []
default_economy: economy.restaurant.standard
presentation_theme: presentation.theme.restaurant
```

### ItemDefinition

```yaml
id: item.restaurant.dish.glass
display_name: Glass
states: [dirty, scraped, racked, washing, drying, clean, in_service]
traits: [dishware, fragile]
presentation: presentation.item.glass.standard
```

### WorkstationDefinition

```yaml
id: workstation.restaurant.washer.standard
template: template.workstation.batch
capacity: 1
cycle_time: 30s
inputs:
  - item_trait: dishware
    state: racked
outputs:
  - state: drying
interaction_ports: [load, unload, inspect]
presentation: presentation.workstation.dish-washer.standard
```

### ProcessDefinition

```yaml
id: process.restaurant.dishwashing.standard
steps:
  - id: scrape
    workstation: workstation.restaurant.scrape-table.standard
  - id: wash
    workstation: workstation.restaurant.washer.standard
  - id: dry
    workstation: workstation.restaurant.clean-rack.standard
  - id: service
    workstation: workstation.restaurant.service-pass.standard
routes:
  - from: scrape
    to: wash
  - from: wash
    to: dry
  - from: dry
    to: service
```

### ScenarioDefinition

Owns bounded initial conditions and deterministic pressure:

```yaml
id: scenario.restaurant.first-shift
seed: first-shift-v1
facility: facility.restaurant.rossis.back-of-house
processes: [process.restaurant.dishwashing.standard]
demand_profile: demand.restaurant.dinner-intro
workers:
  - character: character.restaurant.ray-morales
incidents:
  - incident.restaurant.washer.sticky-ready
```

### QuestDefinition

Quests specify conditions and evidence, not one mandatory implementation:

```yaml
id: quest.restaurant.glasses.where-did-they-go
scenario: scenario.restaurant.first-shift
objective: "Reduce service starvation for clean glasses during the rush."
completion:
  all:
    - metric: service.glass_starvation_seconds
      op: less_than
      value: 45
hints:
  - after: 120s
    text: "Watch where clean glasses wait between the rack and service."
```

### CharacterDefinition

```yaml
id: character.restaurant.avery-chen
display_name: Avery Chen
role: role.restaurant.shift-manager
traits: [results_oriented, pragmatic]
knowledge_profile: knowledge.restaurant.manager.standard
authority: [assign_worker, approve_small_purchase]
presentation: presentation.character.avery-chen
```

## Template system

Templates reduce authoring repetition. They do **not** invent unconstrained gameplay.

Every template has:

```text
TemplateId
TemplateVersion
Parameters
NamedSeed (only if variable output exists)
ExpansionResult
SourceProvenance
```

Generated definitions are normalized before hashing/validation.

### Workstation template families

Start with concrete recurring forms:

- `manual`: worker performs one or more actions;
- `batch`: collects N inputs, cycles, releases outputs;
- `buffer`: holds items with capacity/order policy;
- `inspection`: observes/classifies and routes;
- `service`: consumes/releases work to external demand;
- `transport`: moves items between nodes with capacity/time.

Do not make one mega-template containing dozens of optional fields. Compose smaller traits/components only after actual repeated needs appear.

### Process topology templates

Useful bounded shapes:

```text
Linear
Branch
Merge
Parallel
Batch
Inspect → Pass/Fail
Rework Loop
Queue → Server
Fan-out / Fan-in
```

Parameters identify actual workstation/item definitions.

### Item-family templates

Use for repeated state-machine families such as:

- dirty → processing → clean;
- received → inspected → stored → picked → shipped;
- cart → pending → authorized → completed/refunded;
- raw → processed → inspected → finished.

Never force every industry into one universal state enum.

### Demand templates

Examples:

- constant;
- ramp;
- lunch/dinner rush;
- burst;
- scheduled arrivals;
- random-but-seeded arrivals;
- correlated item mix.

### Worker/persona templates

Parameterizable facts:

- role;
- skill bands;
- walking/work speed modifiers within bounded ranges;
- knowledge coverage;
- schedule;
- communication style tags;
- training state.

Characters with narrative importance remain authored. Template output is best for background staff and scenario variations.

### Knowledge templates

Useful because the game distinguishes reality from belief:

- complete local knowledge;
- delayed dashboard knowledge;
- stale procedure knowledge;
- tribal workaround knowledge;
- partial cross-team knowledge;
- sensor-derived knowledge.

### Incident templates

Start with:

- demand spike;
- worker absent;
- equipment unavailable;
- reduced capacity;
- process delay;
- incorrect/sticky sensor;
- message/event delayed or duplicated;
- external provider unavailable;
- quality defect/rework;
- resource shortage.

Each incident declares:

```text
Trigger
Scope
Injected change
What is observable immediately
What evidence can reveal hidden truth
Recovery/end conditions
Deterministic schedule/seed
```

### Quest templates

Quest templates define **story structure**, not canned text.

Examples:

- Observe → hypothesize → improve → prove;
- Delegate → ambiguity → capture specification → retry;
- Automate → hidden assumption → incident → trace → repair;
- Buy/vendor → integrate → SLA failure → reassess boundary;
- Scale → bottleneck moves → local optimization fails;
- Pattern transfer → familiar problem in new domain.

### Pattern exposure templates

A pattern exposure can be generated from:

```text
Problem signature
Available abstractions
Recognition evidence
Transfer domain
Counterexample pressure
```

The pattern system never forces the solution. It records structural evidence when player behavior qualifies.

## Facility generation

Prefer **bounded semi-procedural facilities**:

1. authored shell or room graph;
2. zones with constraints;
3. workstation slots/utility constraints;
4. deterministic fixture/prop variation;
5. validation for walkability and required process connectivity.

This gives replay/scenario variation without turning the game into a random map generator.

### Validation

Generated facilities must prove:

- spawn can reach required interaction ports;
- required process stations exist;
- no mandatory port is permanently blocked;
- safety/clearance constraints are satisfied if enabled;
- quest-critical entities can be addressed by stable IDs or tags;
- deterministic expansion is reproducible.

## Content validation rules

At minimum detect:

- duplicate IDs;
- unknown references;
- wrong reference type;
- unsupported schema version;
- invalid state transition references;
- process cycles where cycles are disallowed;
- unreachable required quest conditions where statically evident;
- unknown metrics/actions;
- conflicting template parameters;
- missing presentation fallback for shipping-facing content;
- malformed localization keys later;
- PatternKit catalog mismatches for imported pattern IDs.

## Content tests

Create a dedicated test surface that can:

- compile all shipping content;
- snapshot normalized definitions where useful;
- run representative scenarios headlessly;
- verify quest reachability/outcomes;
- check pattern coverage matrix;
- check asset/presentation references;
- detect ID drift.

## Hot reload

Hot reload is desirable for authoring but not a prerequisite for schema v1.

Safe progression:

1. command-line/content-test compile;
2. client reload from menu/dev action;
3. automatic file-watch reload only after state replacement semantics are clear.

Never hot-reload simulation definitions into a live deterministic run without explicit reset/migration behavior.

## Custom editor decision

Defer a bespoke content editor until at least two industries have been authored in YAML.

Build one only when observed problems justify it, such as:

- reference discovery;
- graph editing;
- facility layout;
- dialogue branching;
- visual incident timelines.

The YAML format remains a portable source of truth even if editors arrive later.
