# Data and Schemas

## Data classes

Separate:

1. **Definitions** — immutable content authored by designers.
2. **Runtime state** — mutable simulation state.
3. **Knowledge state** — what actors/player know about runtime reality.
4. **Presentation state** — transient render/UI projection.
5. **Telemetry** — derived observations/history.
6. **Save metadata** — versioning and restore information.

## Stable IDs

Content uses stable string IDs:

```text
restaurant.process.dishwashing.standard
warehouse.capability.identify_package
pattern.strategy
```

Runtime high-count entities use compact numeric typed IDs with mapping to definition IDs.

## Definitions

Prefer records/immutable structures:

```csharp
public sealed record ProcessDefinition(
    string Id,
    IReadOnlyList<string> States,
    IReadOnlyList<TransitionDefinition> Transitions);
```

## Runtime stores

High-count mutable state should avoid object graphs.

Possible shape:

```text
ActorStore
WorkItemStore
MachineStore
QueueStore
InventoryStore
```

Stores may begin as arrays/lists and evolve toward chunked/SoA representation after profiling.

## Knowledge representation

Knowledge items should track:

- proposition/fact ID;
- confidence;
- source;
- observed timestamp;
- owner(s);
- documented status;
- expiration/staleness.

## Assumptions

Assumptions have:

- statement;
- scope;
- provenance;
- confidence;
- dependencies;
- validation status;
- consequence if false.

This supports gameplay around hidden assumptions and research.

## Serialization

Do not serialize arbitrary CLR object graphs as the save contract. Use explicit versioned DTOs or binary schemas that we control.

First prototype may use JSON snapshots for debuggability; production can move hot/high-volume data to a compact binary format once requirements are known.

The dish-station replay journal serializes fixture placement and player movement as commands, not as client scene state. `FloorCell` and `DishStationFixture` are engine-neutral values. Restoring a checkpoint replays custom layout, route consequences, player location, future movement, and future placement in the same chronology as other consequential changes.

The opt-in first-hours playtest evidence is a separate, explicit JSON DTO rather than a career checkpoint. Schema version 2 records anonymous session identity, UTC and active-simulation duration, onboarding choices, progression and quest snapshots, frozen reliability/report evidence, and concrete Shift Handbook open counts with first/last simulation ticks per tutorial stage. Handbook use is presentation telemetry and never enters the authoritative replay. The file is emitted atomically only after the final outcome; unsupported versions fail explicitly, and an export failure does not interrupt or replace the career save.

## Schema evolution

Every persistent format must define:

- version;
- migration path;
- compatibility policy;
- failure behavior when unsupported.

## Content compilation

Hand-authored YAML is validated and compiled into a normalized representation. Runtime should not repeatedly parse complex YAML.
