# Content schema v1

`schema_version: 1` YAML bundles compile through `Automation.Content.ContentCompilerV1` into immutable runtime definitions. YAML never deserializes into simulation objects.

The reference fixture is [`fixtures/schema-v1/minimal-restaurant.yaml`](fixtures/schema-v1/minimal-restaurant.yaml). Compile it from the repository root with:

```powershell
dotnet run --project src/Automation.Headless -c Release -- --compile-content content/fixtures/schema-v1/minimal-restaurant.yaml
```

## Stable IDs

IDs are lowercase semantic paths. Each definition type owns a required prefix:

| Definition | Prefix |
|---|---|
| Industry | `industry.` |
| Facility | `facility.` |
| Item | `item.` |
| Workstation | `workstation.` |
| Process | `process.` |
| Scenario | `scenario.` |
| Quest | `quest.` |
| Character | `character.` |
| Incident | `incident.` |

Role and presentation references use `role.` and `presentation.` respectively; those catalogs remain opaque stable IDs here. All nine definition kinds share one global ID namespace, so duplicates are rejected even across types.

## Required shapes

- `industries`: `id`, `display_name`.
- `facilities`: `id`, `industry`, `display_name`, nonempty `workstations`.
- `items`: `id`, `industry`, `display_name`, ordered nonempty unique `states`.
- `workstations`: `id`, `industry`, `display_name`, nonempty `accepted_items`, `input_state`, `output_state`, `presentation`, and `presentation_fallback`.
- `processes`: `id`, `industry`, nonempty unique `steps`, `routes`, optional `allow_cycles` (default `false`).
- `characters`: `id`, `industry`, `display_name`, `role`, nonempty `motivation`, nonempty stable-ID `known_facts`, `blind_spots`, and `authority`, directional `relationships`, `presentation`, and `presentation_fallback`.
- `scenarios`: `id`, `industry`, `facility`, nonempty `processes`, `items`, `characters`, and named deterministic `seed`. A scenario may carry the optional complete `narrative` block and a dish-station scenario may carry the complete `dish_station` runtime block below.
- `quests`: `id`, `scenario`, nonempty unique character `participants`, outcome-oriented `objective`, and one numeric `completion` condition. A quest may also carry the complete optional `narrative` block described below.
- `incidents`: `id`, `industry`, `display_name`, nonnegative `trigger_at_tick`, `scope`, immediate `observable`, discoverable `evidence`, `recovery`, and exactly one typed effect block.

Supported v1 completion metrics are `service.available.count`, `service.shortage.count`, and `process.completed.count`. Operators are `equal`, `greater_than_or_equal`, and `less_than`.

## Character and participant metadata

Character knowledge, blind spots, and authority are opaque stable capability/fact IDs, separate from player-visible prose. Relationships are directional pairs of a target character ID and a stable `relationship.` kind. Relationship targets must exist, cannot point to the source character, must be unique per source, and must belong to the same industry. Empty relationships are allowed for small fixtures; the production restaurant cast authors at least two per character.

Primary presentation references use `presentation.character.` and every character requires a `presentation.fallback.` reference. These are presentation catalog IDs only; character identity and quest involvement do not depend on an asset being available.

Quest participants are explicit stable character IDs. Every participant must exist and belong to the quest scenario's character roster. The runtime first-shift adapter preserves these IDs, while presentation resolves current names and roles from the compiled catalog.

## Scenario chapter narrative

S028 adds an optional, all-or-nothing scenario `narrative` block for chapter-level copy that does not belong to one quest:

```yaml
narrative:
  chapter_title: ROSSI'S / FIRST SHIFT
  briefing:
    - title: SERVICE OPENS SOON
      body: Avery needs the dish station ready before dinner demand arrives.
  debrief_summary: Avery gives you ownership of the station after the live shift holds.
  debrief_questions:
    - What constrained glass service, and which visible evidence changed your mind?
```

The generic schema requires nonempty title, briefing pages, summary, and questions. The production first-shift adapter requires exactly three workplace briefing pages and three debrief questions because the existing five-page start flow reserves its final two pages for guidance and accessibility settings. The client consumes this compiled copy directly; briefing/debrief changes therefore alter the manifest and require no client source edit.

### Contextual character barks

S027 adds optional character-owned `barks`. Each entry requires a globally unique `dialogue.` ID, a quest in which that character participates, one closed semantic trigger (`queue-pressure`, `automation-incident`, or `shift-succeeded`), priority (`ambient`, `important`, or `critical`), a non-negative `cooldown_ticks`, and a nonempty line of at most 160 characters:

```yaml
barks:
  - id: dialogue.restaurant.tessa.glass-pressure
    quest: quest.restaurant.first-shift.find-the-constraint
    trigger: queue-pressure
    priority: important
    cooldown_ticks: 60
    line: We are out of clean glasses at service while the dish pit is still busy.
```

The authoritative simulation emits typed narrative events only when the corresponding world transition occurs. Content resolution filters by event and quest, suppresses a bark until its own cooldown expires, then selects the highest priority with stable bark-ID ordering as the deterministic tie-breaker. The client resolves the stable speaker ID to the current display name and role. Dialogue text never causes or substitutes for simulation state changes.

## Quest narrative metadata

S015 adds an optional, all-or-nothing `narrative` block to a quest:

```yaml
narrative:
  runtime_id: clock-in
  sequence: 1
  title: CLOCK IN
  situation: Service needs one clean plate from an unfamiliar station.
  discovery: Work accumulates in stages even before anyone names the process.
  unlock_rationale: You have seen work change state.
  reward:
    experience: 100
    capability: capability.state-lens
  steps:
    - id: restock-first-dish
      text: RESTOCK ONE CLEAN PLATE
    - id: enable-dinner-rush
      text: ENABLE THE DINNER RUSH WITH {binding}
      input_action: toggle-rush
```

`runtime_id`, step IDs, and optional input-action IDs use lowercase kebab-case tokens. Sequence and experience are positive integers. Step IDs must be unique within a quest. A step with `input_action` must contain exactly one `{binding}` placeholder; a step without an input action cannot contain that placeholder.

The production first-shift adapter adds stricter contextual checks: exactly one entry for every runtime quest, contiguous unique sequences, globally unique tutorial-stage steps, known capability rewards, and reward values coherent with authoritative progression behavior. The authored step text is resolved against current logical input bindings by the client. The production bundle is [`restaurant/first-shift.yaml`](restaurant/first-shift.yaml).

## Dish-station scenario runtime data

S016 adds an optional, all-or-nothing `dish_station` block to a scenario. It contains the concrete values required to construct an authoritative `DishStationWorld`:

```yaml
dish_station:
  initial_dirty: { plates: 6, glasses: 2, trays: 0 }
  initial_available: { plates: 0, glasses: 0, trays: 0 }
  arrival_interval_ticks: 30
  glass_every_arrivals: 3
  rack_capacity: 12
  washer_cycle_ticks: 20
  worker_action_interval_ticks: 5
  flow_cell_worker_action_interval_ticks: 4
  sticky_ready_fault_after_automated_starts: 2
  sticky_ready_fault_permille_per_start: 0
  demand_kind: glass
  demand_interval_ticks: 15
  initial_rush_enabled: false
  initial_new_hire_enabled: false
  initial_new_hire_knowledge: none
  initial_automation_policy: off
  initial_layout: linear
  economy:
    completed_dish_value: 120
    labor_ticks_per_work_action: 1
    labor_cost_per_tick: 3
    staffing_cost_per_enabled_tick: 1
    tray_rework_cost: 35
    service_shortage_downtime_cost: 80
    automation_incident_downtime_cost: 120
    flow_cell_investment_cost: 180
```

Counts and the sticky-ready threshold must be non-negative. Timing and capacity values must be positive. Fault probability is integer permille from 0 through 1000. Supported dish kinds are `plate`, `glass`, and `tray`; knowledge profiles are `none`, `happy-path`, `rush-aware`, and `fully-documented`; automation policies are `off`, `reported-ready-only`, and `corroborated-ready`; layouts are `linear` and `u-shaped-cell`.

S029 adds the optional complete `economy` block. `completed_dish_value` and `labor_ticks_per_work_action` are positive integers; all rates and costs are nonnegative integers. If the block is omitted, the engine-neutral first-shift defaults preserve compatibility. If present, every field is required. These authored values are rates only: the authoritative world derives labor actions, staffed ticks, rework, shortages, automation incidents, completed throughput, and flow-cell purchase state from commands and ticks, then applies the rates without reading YAML.

The compiler produces a validated, engine-neutral `DishStationScenarioConfiguration` directly from this block. Simulation never reads YAML. Production composition must provide an explicit compiled configuration when creating a world; resolved configurations remain part of replay/save data. Headless command-line flags are runtime overrides over these authored defaults rather than a second default scenario.

## Incident definitions

S020 adds top-level incident definitions. The first closed family set is:

- `process_delay`: positive `duration_ticks` and `added_cycle_ticks`;
- `capacity_loss`: positive `duration_ticks` and `lost_slots`;
- `bad_sensor`: positive `duration_ticks` and the supported `reported-ready-stuck-true` signal;
- `blocked_resource`: positive `duration_ticks` and the supported `washer` resource;
- `worker_absence`: positive `duration_ticks` and the supported `new-hire` worker;
- `demand_spike`: positive `duration_ticks`, supported `demand_kind`, and positive `interval_ticks`.

All six use `scope: dish-station`. Content adapts a compiled definition to an engine-neutral scheduled incident; activation enters the authoritative world through an explicit replay-serialized command. The simulation records start and recovery ticks plus authored observable/evidence text. Incidents with the same family cannot overlap in v1.

The checked-in family templates live under [`templates/incidents/`](templates/incidents/). Each template declares only its identity/effect parameters and one finite seeded `trigger-tick` variant. The headless runner can expand and execute one directly:

```powershell
dotnet run --project src/Automation.Headless -c Release -- --expand-template content/templates/incidents/demand-spike.template.yaml `
  --named-seed incident-proof-42 --parameter incident-slug=lunch-rush-proof `
  --parameter duration-ticks=3 --parameter demand-kind=plate --parameter interval-ticks=1 `
  --run-incident --ticks 8
```

## Validation and normalization

Compilation rejects malformed YAML, unknown keys, unsupported versions, malformed or duplicate IDs, unknown references, wrong reference types, unknown item states, incomplete scenario narrative, missing workstation or character presentation fallbacks, invalid character relationships, off-roster quest participants or bark speakers, invalid/duplicate bark metadata, duplicate/unknown process steps, state-incompatible process routes, disallowed cycles, and unsupported quest metrics/operators. First-shift adaptation also rejects missing/unreachable tutorial beats or the wrong briefing/debrief cardinality. Diagnostics include the source and semantic path; YAML parse diagnostics also include line and column.

Checked-in valid bundles and durable invalid-case mutations are enforced by the dedicated `Automation.Content.Tests` project. Run it directly with:

```powershell
dotnet test tests/Automation.Content.Tests/Automation.Content.Tests.csproj -c Release
```

Definitions normalize by semantic ID. Unordered reference collections normalize for hashing, process step order and item-state order remain authored semantics, and routes normalize by endpoints. The manifest records counts for all nine kinds and a lowercase SHA-256 over canonical content.

Schema v1 has no implicit migration. S026 deliberately expanded the pre-alpha v1 required character and quest shapes; every checked-in bundle and template was migrated, and older external v1 bundles fail with targeted missing-field diagnostics instead of receiving silent defaults. S028's scenario narrative is optional for generic/minimal bundles but required by the production first-shift adapter, preserving generic schema-v1 compatibility while failing an incomplete first-shift chapter clearly. A bundle with another version is rejected clearly. Future versions must add an explicit compatibility/compiler path rather than silently interpreting changed fields.

## Template expansion v1

Template expansion is a separate strict YAML envelope that produces an ordinary schema-v1 bundle before semantic validation:

```yaml
template_schema_version: 1
template_id: template.content.restaurant.example
template_version: 1
parameters:
  facility-slug: token
  rack-capacity: positive_integer
variants:
  demand-kind:
    kind: token
    options: [plate, glass, tray]
content: |
  # ordinary schema-v1 YAML containing:
  # {{parameter:facility-slug}}
  # {{parameter:rack-capacity}}
  # {{variant:demand-kind}}
```

Supported parameter/variant kinds are `token`, `content_id`, `non_negative_integer`, `positive_integer`, `boolean`, and `text`. Parameters are required exactly: missing, extra, invalid, unused, and undeclared values/placeholders fail. Templates with variants require a nonempty named seed; fixed templates reject one. Variant selection is SHA-256-based over the template ID, template version, named seed, and variant name, so it is independent of process/runtime random state. Options are finite, normalized, and must be unique.

The expansion result contains normalized expanded YAML, its fully compiled immutable catalog and content hash, immutable provenance (source, template ID/version, named seed, normalized sorted parameters, and selected variants), and a deterministic expansion hash covering provenance plus the compiled content hash. Expanded YAML always passes through the ordinary schema-v1 compiler and S017 semantic validation.

The checked-in proof is [`templates/proofs/seeded-scenario.template.yaml`](templates/proofs/seeded-scenario.template.yaml). Expand it with:

```powershell
dotnet run --project src/Automation.Headless -c Release -- --expand-template content/templates/proofs/seeded-scenario.template.yaml `
  --named-seed proof-0 --parameter facility-slug=proof-house --parameter rack-capacity=12
```

Template v1 still provides no scripting, loops, conditionals, recursive templates, or includes. Concrete workstation families begin at S019 and incident families at S020.
