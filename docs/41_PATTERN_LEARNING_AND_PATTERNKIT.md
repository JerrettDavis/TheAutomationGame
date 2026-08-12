# Pattern Learning and PatternKit Integration

> Design patterns are names for structures the player has already experienced, not tutorial subjects to memorize.

## Core learning loop

```text
Experience a problem
    ↓
Solve it somehow
    ↓
Encounter variation
    ↓
Feel duplication / coupling / ambiguity / failure
    ↓
Recognize a reusable shape
    ↓
Apply an abstraction
    ↓
Learn the conventional name
    ↓
Transfer to another domain
    ↓
Experience tradeoffs / misuse
    ↓
Compose with other patterns
    ↓
Express in code
```

The ideal reaction is:

> "Oh. That's what this pattern is called. I've already used it."

## Mastery lifecycle

Patterns have independent player knowledge state:

```text
Encountered
Applied
Recognized
Named
Transferred
Stress-Tested
Composed
Expressed
Mastered
```

These are evidence-backed milestones, not XP purchases.

### Encountered

Player experienced a qualifying problem signature.

### Applied

Player used a structurally similar solution, whether or not the game offered it explicitly.

### Recognized

Enough evidence exists to call attention to the recurring shape.

### Named

Codex reveals conventional terminology.

### Transferred

Player applies/re-encounters the concept in another domain.

### Stress-Tested

Player sees when the pattern creates costs or fails under pressure.

### Composed

Player uses it alongside another pattern deliberately.

### Expressed

Player encounters/creates a code representation.

### Mastered

Player has enough varied evidence that the game treats the concept as part of their working vocabulary.

## Pattern evidence

Conceptual runtime/progression model:

```text
PatternEvidence
  PatternId
  SourceQuestId?
  ScenarioId
  IndustryId
  ProblemSignature
  PlayerSolutionShape?
  Tick / ReplayRef
  Consequence
  EvidenceTags[]
```

```text
PatternKnowledge
  PatternId
  EncounterCount
  ApplicationCount
  TransferDomains[]
  Evidence[]
  NamedAt?
  StressTests[]
  Compositions[]
  CodeRepresentations[]
```

The simulation does not need to understand "Strategy Pattern." Pattern knowledge belongs to progression/content interpretation.

## Multiple entry points

A pattern may be discovered through:

- **construction:** player deliberately builds the shape;
- **refactoring:** duplication/coupling creates pressure;
- **incident:** failure exposes a structural need;
- **scale:** a local solution stops working;
- **integration:** independently valid systems mismatch;
- **delegation:** another actor needs an explicit contract/process;
- **observation:** player recognizes a structure already present;
- **acquisition:** vendor/tool arrives implementing the shape;
- **code:** player independently writes it later;
- **Codex challenge:** optional deliberate practice after naming.

Quest logic must not pretend a concept is new if prior evidence proves otherwise. Use alternate dialogue such as "You've seen this shape before."

## Reinforcement rule

For major GoF patterns, target a **1–2–4** exposure shape where practical:

- 1 primary discovery;
- 2 deliberate cross-domain reinforcements;
- 4+ incidental appearances.

Do not force meaningless repetitions solely to hit counts. The coverage validator should flag gaps for review, not mechanically certify pedagogy.

## GoF main-campaign spine

All classic 23 patterns receive a main-story path.

| Chapter | Patterns |
|---|---|
| Restaurant | State, Strategy, Template Method, Observer |
| Warehouse | Command, Chain of Responsibility, Iterator, Composite, Factory Method |
| Retail | Adapter, Decorator, Facade, Proxy, Memento |
| Factory | Builder, Prototype, Abstract Factory, Bridge, Visitor |
| Logistics | Mediator, Singleton, Flyweight |
| Financial | Interpreter |
| Software / Platform | all 23 revisited in code |

### Restaurant

**State** — dish/machine lifecycle, valid transitions, reported vs real state.

**Strategy** — normal/rush routing policies become interchangeable behavior.

**Template Method** — multiple station procedures share stable skeleton with variable steps.

**Observer** — stop polling for readiness/inventory; interested consumers react to events. Later stress-test notification overload.

### Warehouse

**Command** — scanner actions need to be captured, queued, retried, audited, or executed later.

**Chain of Responsibility** — exception ownership escalates through bounded handlers.

**Iterator** — inventory traversed by physical order, FIFO, expiration, priority without consumer knowing storage representation.

**Composite** — item/case/pallet/shipment hierarchy supports recursive operations.

**Factory Method** — handling implementation created based on incoming work type through a creation seam.

### Retail

**Adapter** — old POS and new provider/device interfaces mismatch.

**Decorator** — pricing/sale behavior gains composable rules.

**Facade** — sale completion hides coordination of many subsystems behind a useful boundary.

**Proxy** — sensitive/remote refund/provider access adds authorization, caching, logging, throttling, or remote access.

**Memento** — suspend/restore transaction/configuration state.

### Factory

**Builder** — progressively construct valid complex cell configuration.

**Prototype** — clone a proven line/configuration and modify selected differences.

**Abstract Factory** — choose compatible equipment/system families.

**Bridge** — process/job abstraction varies independently from machine implementation.

**Visitor** — new inspections/reports operate across heterogeneous equipment.

### Logistics

**Mediator** — control/dispatch coordination replaces exploding pairwise relationships.

**Singleton** — one authoritative bounded instance; later expose global-state and distributed-system limitations.

**Flyweight** — share repeated package/product/routing metadata across huge object counts.

### Financial

**Interpreter** — business policy becomes a grammar of composable expressions.

## Pattern Codex

The first section is personal history:

```text
STRATEGY

First encountered
Rossi's Restaurant / Dish Station / Shift 4

Problem you faced
Rush conditions needed a different routing priority.

First applied
Glass Rush Policy

Reused
Warehouse storage routing

Stress tested
Too many tiny policies made routing hard to explain.

Conventional name
Strategy Pattern
```

Then:

1. intent/problem;
2. conceptual structure;
3. tradeoffs;
4. related/confusable patterns;
5. places the player has used it;
6. conventional code;
7. PatternKit form.

Completionists may see catalog progress, but collecting patterns must never become the primary game victory condition.

## PatternDefinition

Game-owned pattern overlay example:

```yaml
id: pattern.strategy
catalog: gof
category: behavioral
external_catalog_id: strategy

problem_signatures:
  - varying_algorithm
  - interchangeable_policy
  - repeated_conditional_policy
  - runtime_behavior_selection

recognition:
  minimum_exposures: 2
  requires_application: true

primary_encounters:
  - quest.restaurant.two-stations-one-problem
alternate_encounters:
  - quest.warehouse.storage-routing
  - quest.logistics.carrier-selection
reinforcements:
  - scenario.finance.fraud-policy
counterexamples:
  - incident.restaurant.policy-fragmentation

related:
  - pattern.state
  - pattern.factory-method
  - pattern.template-method

patternkit:
  namespace: PatternKit.Behavioral.Strategy
```

## PatternKit integration boundary

PatternKit and The Automation Game remain separate products/codebases.

### PatternKit owns

- canonical pattern ID/name;
- category/catalog membership;
- intent/summary where exportable;
- relationships/tags;
- implementation metadata;
- C# / PatternKit representation metadata.

### The Automation Game owns

- quests/stories;
- industries;
- problem signatures as game mechanics;
- evidence recognition;
- player mastery;
- Codex discovery history;
- reinforcement/counterexamples;
- presentation/localized educational prose.

### Integration

Preferred build-time/data contract:

```text
PatternKit
  ↓ exports/generates
pattern-catalog.v1.json
  ↓
Automation Game content validation/import
  ↓
Game-specific PatternDefinition overlay
```

Do not reference PatternKit runtime code from core simulation merely to teach its catalog.

## Coverage validation

A build/content report should eventually show:

```text
PatternKit catalog entries      121
Game overlays                   121 / 121

GoF main-story primary          23 / 23
GoF named                       23 / 23 planned
GoF transfer exposure           xx / 23
GoF stress-test exposure        xx / 23
GoF code representation         23 / 23

Optional catalog primary/side   xx / 98
```

The exact PatternKit total is sourced from the imported catalog, not hard-coded forever.

## Optional specialization trees

### Reliability Specialist

Natural incident sequence:

```text
intermittent failure → Retry
retry storm → Circuit Breaker
resource exhaustion → Bulkhead
producer overload → Backpressure
request duplication → Idempotent Receiver
DB/event split-brain → Outbox
consumer processing durability → Inbox
```

Also expose rate limiting, priority queue, queue-based load leveling, health endpoints, timeout management, cache stampede protection where the system creates the need.

### Integration Specialist

Enterprise-integration concepts emerge after systems begin exchanging messages:

```text
Message Channel
→ Envelope / Correlation
→ Translator / Canonical Model
→ Router / Filter / Recipient List
→ Publish-Subscribe
→ Splitter / Aggregator / Resequencer
→ Dead Letter / Guaranteed Delivery / Store
→ Competing Consumers / Scatter-Gather
→ Saga / Process Manager
→ Gateway / Bridge / Bus / Control Bus
```

### Architecture Specialist

Optional/late organizational/software progression:

```text
Value Object
Aggregate Root
Repository
Unit of Work
Service Layer / Domain Service
Specification
Domain Event
Bounded Context / Context Map
Anti-Corruption Layer
Ports and Adapters
CQRS
Event Sourcing
Materialized View
Workflow Orchestration
```

### Cloud/Platform Specialist

Late distributed-system problems:

```text
Sidecar / Ambassador
Gateway Routing / Aggregation / BFF
External Configuration Store
Leader Election
Distributed Lock / Lease
Cache patterns
Scheduler Agent Supervisor
Strangler Fig
```

## Pattern quest storyboard

Every primary pattern encounter should answer these ten beats:

1. **Normal:** what currently works?
2. **Pressure:** what changed?
3. **Pain:** what coupling/duplication/ambiguity/failure becomes visible?
4. **Player response:** what valid solutions are possible?
5. **Abstraction:** what reusable structural shape can emerge?
6. **Reveal:** when is the conventional name appropriate?
7. **Transfer:** where does the same shape appear in another domain?
8. **Counterexample:** when can it make things worse?
9. **Composition:** what patterns naturally interact with it?
10. **Code:** how is it represented after programming unlocks?

Use `templates/QUEST_STORYBOARD_TEMPLATE.md` for the actual quest and include these fields under pattern metadata.

## Anti-pattern teaching rule

Do not create a second collectible catalog of memorized anti-pattern names in the main campaign. Let misuse be felt first:

- Singleton as global dependency;
- Observer storm;
- Facade hiding critical detail;
- decorator/policy explosion;
- excessive abstraction;
- distributed lock misuse;
- retry amplification;
- eventual consistency without user-facing reconciliation.

Names can appear in optional Codex notes later.
