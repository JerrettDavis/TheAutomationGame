# ADR-0008: Engine-Neutral Incident Lifecycle

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S020 requires authored process delays, capacity loss, bad sensor observations, blocked resources, worker absence, and demand spikes to produce reproducible simulation consequences. YAML-only labels would be unauthoritative; embedding YAML concepts in `Automation.Simulation` would reverse the content boundary. Six concrete families justify one bounded incident lifecycle, but not a general scripting system.

## Decision

Define a closed, engine-neutral `DishStationIncidentEffect` union in `Automation.Domain`. An incident carries a stable ID, scope, immediate observable, discoverable evidence, recovery description, positive duration, and exactly one typed effect. Consequential activation enters `DishStationWorld` through `TriggerDishStationIncidentCommand` and is replay-serialized like other commands.

The simulation owns tick boundaries, active effects, recovery, and snapshot traces. Effects modify existing authoritative rules only: washer completion timing, effective rack capacity, reported readiness, washer availability, delegated-worker cadence, or service demand cadence. Content compiles YAML into immutable definitions and adapts them to scheduled domain incidents; simulation never parses YAML or references `Automation.Content`.

The union is intentionally closed to the six S020 families. Additional families require an explicit domain effect and observable/recovery semantics rather than arbitrary property names or CLR type construction.

## Alternatives considered

- Presentation-only incident labels. Rejected because traces would not correspond to world consequences.
- Direct mutable scenario-configuration overrides. Rejected because they cannot express bounded trigger/recovery timelines and weaken replay evidence.
- A generic condition/effect scripting language. Rejected as premature; S023 owns a later automation IR.
- Letting content construct simulation commands. Rejected to preserve the engine-neutral content boundary.

## Consequences

### Positive

- authored incidents have deterministic authoritative start and recovery ticks;
- replay reconstructs active effects and lifecycle traces;
- evidence and observable truth travel with the definition;
- domain and simulation remain independent of Stride and YAML;
- effects cannot silently mutate arbitrary simulation state.

### Negative

- each genuinely new effect requires domain and simulation review;
- overlapping incidents of the same family are rejected in v1;
- the prototype retains the latest 48 lifecycle trace entries;
- S020 has no conditional triggers or recovery predicates.

## Validation

- compile all six checked-in templates to typed effects;
- prove each effect changes an authoritative rule and recovers;
- prove a fixed seed reproduces trigger selection, world timeline, trace, and replay state;
- keep the unchanged first-shift 250-tick headless reference passing.

## Revisit when

- an incident needs a trigger other than an authored tick;
- recovery depends on player/world conditions rather than duration;
- cross-industry reuse disproves dish-station-specific assumptions;
- S023 automation IR can safely express part of the lifecycle without conflating incidents and player-authored rules.
