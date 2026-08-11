# Simulation Ontology

The ontology is the most important long-term design artifact. Industries should be composed from a relatively small set of reusable primitives rather than implemented as unrelated minigames.

## Principle

Prefer a small number of composable concepts with industry-specific data over bespoke code for every scenario.

## Identity primitives

Use compact typed identifiers in the simulation layer.

```csharp
public readonly record struct ActorId(int Value);
public readonly record struct FacilityId(int Value);
public readonly record struct ResourceId(int Value);
public readonly record struct ProcessId(int Value);
public readonly record struct WorkItemId(long Value);
```

No engine entity identifiers may leak into domain identity.

## Core nouns

### Actor

An entity capable of participating in interactions or performing work.

Examples: employee, customer, contractor, robot, external organization, software agent.

Key properties:

- capabilities;
- role assignments;
- location;
- availability;
- knowledge;
- incentives;
- fatigue/stress where relevant;
- authority;
- trust/reputation.

### Resource

Something consumed, transformed, occupied, reserved, or produced.

Examples: dish, ingredient, package, money, electricity, machine time, human attention, network capacity.

### Asset

A persistent owned or controlled thing with condition and capabilities.

Examples: dishwasher, truck, register, server, conveyor, building.

### Facility

A spatial and organizational container.

Examples: restaurant, warehouse, store, factory, office, data center.

### Layout

The spatial arrangement of assets and work handoffs inside a facility. Layout changes the cost and timing of interactions without changing the legal process states themselves. The dish-station slice models concrete `DishStationFixture`, `FloorCell`, and `DishStationPlacements` values. `Linear` and `UShapedCell` are presets; player-authored arrangements become `Custom` while retaining explicit fixture cells. Handoff distance affects travel evidence and delegated work frequency. This remains dish-station-specific; a generic spatial-cost abstraction waits for a second recurring use.

Player location is authoritative because walking distance is consequential evidence. Camera location, animation interpolation, hover state, and placement previews are presentation state. A preview becomes reality only through `PlaceDishStationFixtureCommand`; movement enters through `MovePlayerCommand` or a work action.

### Process

A reusable definition of an episode or flow of work.

A process contains:

- entry conditions;
- actors/roles;
- states;
- interactions;
- decisions;
- effects;
- expected outcomes;
- failure paths;
- measurements.

### Work Item

A concrete instance moving through a process.

Examples: dirty dish batch, customer order, refund request, package, deployment, support ticket.

### State

A meaningful condition that constrains legal interactions.

Prefer explicit state machines where transitions matter.

### Interaction

An exchange or action among actors, resources, assets, or systems.

### Decision

A choice derived from state, inputs, policies, knowledge, or judgment.

### Effect

A request to change the outside world: move a resource, charge payment, print receipt, send message, start machine.

### Policy

A rule that constrains or guides decisions.

### Contract

An explicit boundary agreement between capabilities or organizations.

### Capability

A named ability the organization can provide independently of a particular process.

Examples: authorize payment, identify package, schedule employee, send notification.

### Automation

An executable mechanism that takes responsibility for some interaction, decision, or effect previously performed by a human or another mechanism.

An automation input is knowledge about reality, not reality itself. The concrete dish-station controller therefore retains both `WasherReportedReady` and `WasherPhysicalReady`; policy decides whether one report is sufficient evidence for action. This distinction is intentionally concrete and does not introduce a generic sensor abstraction before another simulation requires it.

## Player progression

First-hours progression is knowledge and organizational capability, not a multiplier on physical work. Quest outcomes are facts derived from authoritative dish-station consequences; experience summarizes accumulated outcomes; levels communicate career growth; capabilities unlock ways of observing or changing the system. Guidance preference changes presentation only. Onboarding completion is nevertheless recorded as an explicit command so save/replay state remains coherent.

The first implementation deliberately uses concrete dish-station quest IDs and outcome checks. A generic quest-condition ontology is deferred until another scenario reveals a recurring shape.

The final dish-station reliability window is likewise a concrete scenario assessment, not a generic contract or daily-quest primitive. `StartShiftTrialCommand` records the player's decision to begin; the simulation compares subsequent demand checks, shortages, and automation incidents with that attempt's baselines. Failure returns to preparation with a causal explanation, while three supplied checks without a new unsafe request prove the composed station outcome. A reusable assessment abstraction remains deferred until a second scenario requires the same lifecycle.

### Observation

Evidence gathered about actual behavior.

### Knowledge

Facts or models known by an actor or organization. Knowledge can be explicit, tribal, uncertain, stale, or wrong.

### Assumption

A proposition treated as true for planning or automation but not guaranteed by the model.

Assumptions should be first-class because many educational failures originate here.

### Invariant

A condition intended to remain true across valid system behavior.

### Incident

An observed deviation requiring explanation or recovery.

### Metric

A measured property over time.

### Organization

A collection of actors, capabilities, ownership boundaries, policies, incentives, and resources.

### Role

A bundle of responsibilities, permissions, expectations, and capabilities within an organization.

### Job

A player's or NPC's concrete assignment to perform or own responsibilities.

## Core verbs

The simulation should be able to express at least:

```text
observe
move
wait
queue
reserve
consume
produce
transform
inspect
classify
route
approve
reject
assign
schedule
communicate
request
respond
retry
cancel
compensate
reconcile
measure
delegate
automate
maintain
repair
learn
forget
```

## Process representation

A process should be serializable independently from code where practical:

```yaml
id: restaurant.dishwashing.standard
entry:
  - dirty_dishes_available
roles:
  - busser
  - dishwasher
states:
  - dirty
  - scraped
  - racked
  - washing
  - clean
  - dry
  - returned
outcome:
  - clean_dishes_available_at_service_station
```

The file above should not attempt to encode every complex rule. Content definitions compose with registered decision/effect capabilities.

## Reality versus knowledge

The simulation owns complete world state, but actors and the player do not automatically know it.

```text
WorldState
  != PlayerKnowledge
  != EmployeeKnowledge
  != AutomationInputs
```

This separation is required for observation, tribal knowledge, sensor uncertainty, misinformation, and outsourcing mechanics.

## Granularity

Not every physical object needs full simulation at all distances. The ontology supports multiple fidelity levels:

- active/visible detailed simulation;
- facility-level aggregated simulation;
- organization/economy summary simulation.

State transitions between fidelity levels must preserve important invariants and observable outcomes.

## Ontology review gate

Before adding a new primitive type, ask:

1. Can this be represented as an existing primitive plus data?
2. Does the new concept have distinct behavior or lifecycle?
3. Will at least two industries plausibly reuse it?
4. Would modeling it as an existing primitive make the domain misleading?

If not, prefer content over new engine concepts.
