# Campaign, Story, Characters, and Player Personas

> Narrative spine for teaching systems, automation, architecture, and programming through lived operational problems.

## Product fantasy

The player begins by doing ordinary work inside an imperfect organization. They become useful because they observe carefully, improve systems, prove outcomes, and automate responsibly. Their scope grows from one workstation to teams, facilities, organizations, distributed systems, and eventually software itself.

The game is not about becoming a magical automation wizard. It is about learning when to standardize, delegate, automate, observe, integrate, buy, build, recover, and sometimes **leave a human in the loop**.

## Player personas

These are design/test personas, not forced in-fiction biographies.

### P1 — Curious Improver

**Background:** little/no programming knowledge.

**Wants:** satisfying simulation, visible improvements, understandable tools.

**Risk:** jargon makes the game feel like homework.

**Design test:** can discover State/Strategy concepts before seeing the names.

### P2 — Automation Tinkerer

**Background:** spreadsheets, home automation, scripts, low-code, factory/game automation.

**Wants:** knobs, rules, experimentation, replay, emergent consequences.

**Risk:** tools feel fake or overly scripted.

**Design test:** automation editor has real semantics and inspectable trace.

### P3 — Software Engineer

**Background:** professional or hobby programming.

**Wants:** recognizes architectural depth; enjoys mapping physical systems to software.

**Risk:** intro feels trivial or patterns feel didactic.

**Design test:** deeper lenses and optional pattern content reward expertise without bypassing play.

### P4 — Operations / Industrial Thinker

**Background:** manufacturing, logistics, service operations, process improvement.

**Wants:** realistic constraints, bottlenecks, variability, human factors.

**Risk:** software metaphors distort operations.

**Design test:** operational concepts remain valid before code is introduced.

### P5 — Educator / Facilitator

**Background:** teacher, trainer, manager, workshop facilitator.

**Wants:** scenarios that trigger discussion, replayable experiments, debrief evidence.

**Risk:** learning outcomes are impossible to inspect or reproduce.

**Design test:** scenarios can export/compare traces and preserve deterministic setups.

## Player perspective progression

```text
Worker
  ↓
Trusted improver
  ↓
Lead / local system owner
  ↓
Cross-team operator
  ↓
Facility/process designer
  ↓
Manager / architect
  ↓
Organization/platform owner
```

The embodied avatar remains meaningful even after higher-level views unlock.

---

# Recurring character bible

Names are working canon and should receive stable IDs now so quests can reference people rather than tutorial functions. They can be renamed later through display/localization data without changing IDs if necessary.

## Avery Chen

**ID:** `character.restaurant.avery-chen`

**Restaurant role:** shift manager.

**Motivation:** keep service moving without blowing labor, quality, or safety targets.

**Strength:** knows business outcomes and staffing reality.

**Blind spot:** may accept a dashboard or procedure as truth until contradictory evidence is shown.

**Narrative use:** gives outcomes, budget/authority, and pressure without prescribing implementation.

**Pattern function:** management policies, Strategy reinforcement, later delegation/governance.

## Ray Morales

**ID:** `character.restaurant.ray-morales`

**Role:** veteran back-of-house worker.

**Motivation:** survive the shift and avoid "improvements" that make the job worse.

**Strength:** deep tribal knowledge and physical workflow intuition.

**Blind spot:** workarounds live in his head and may not transfer to new staff.

**Narrative use:** teaches the value and danger of tacit knowledge.

**Arc:** skeptical collaborator → respected domain expert → partner in making knowledge explicit.

## Jules Martin

**ID:** `character.restaurant.jules-martin`

**Role:** new hire.

**Motivation:** do the job correctly without constantly asking for rescue.

**Strength:** exposes assumptions in supposedly "obvious" processes.

**Blind spot:** limited local context.

**Narrative use:** delegation/specification/observability test. If a process only works when Ray is present, it is not actually captured.

## Tessa Brooks

**ID:** `character.restaurant.tessa-brooks`

**Role:** service/front-of-house liaison.

**Motivation:** have clean, correct items when customers need them.

**Strength:** represents downstream demand and timing.

**Blind spot:** sees starvation and service symptoms, not necessarily backstage causes.

**Narrative use:** creates pull/demand feedback and exposes local optimization.

## Devon Price

**ID:** `character.restaurant.devon-price`

**Role:** maintenance/facilities support.

**Motivation:** keep equipment reliable and avoid unnecessary parts swapping.

**Strength:** distinguishes physical machine state from reported/control state.

**Blind spot:** may focus narrowly on equipment rather than process context.

**Narrative use:** key ally during sticky-ready incident; later reinforces instrumentation, maintenance, failure modes.

## Sam Rivera

**ID:** `character.recurring.sam-rivera`

**Role:** vendor/integrator representative who can recur across industries.

**Motivation:** solve customer problems, close work, preserve a supportable contract boundary.

**Strength:** can provide mature external capability quickly.

**Blind spot/conflict:** vendor incentives, abstractions, SLAs, and lock-in differ from player incentives.

**Narrative use:** outsourcing is neither good nor bad by default; contracts and observability matter.

## Rowan Hale

**ID:** `character.recurring.rowan-hale`

**Role:** reliability/safety reviewer introduced after the player has some success.

**Motivation:** ask what happens when assumptions fail.

**Strength:** adversarial scenario thinking, recovery, evidence.

**Risk:** can become an exposition machine. Use sparingly and make questions arise from real incidents.

**Narrative use:** recurring stress-test side quests.

## Morgan Pike

**ID:** `character.platform.morgan-pike`

**Role:** software/platform architect late in the campaign.

**Motivation:** make distributed organizational systems understandable and evolvable.

**Narrative function:** helps reveal that the structures the player has used physically are the same structures software engineers name and encode.

Morgan should not "teach programming from zero". The player has already been programming conceptually for the whole game.

---

# Campaign spine

## Chapter 1 — Restaurant: Work Becomes Process

### Core promise

The player experiences work before receiving abstract tools.

### Existing/target quest sequence

1. **Clock In**
   - learn movement/work/inspect;
   - feel local queues and demand.
2. **Where Did the Glasses Go?**
   - service starves despite apparent activity;
   - observe actual flow.
3. **Find the Bottleneck**
   - compare waiting/processing, not worker busyness.
4. **Make the Flow Better**
   - player chooses an improvement; success defined by outcome.
5. **The New Hire**
   - Jules cannot execute Ray's undocumented assumptions;
   - capture/specify process.
6. **It Said It Was Ready**
   - reported machine state disagrees with physical reality;
   - automation trusts the wrong signal.
7. **Prove the Fix**
   - replay under equivalent pressure;
   - evidence over intuition.
8. **Own the Shift**
   - operate without step-by-step coaching.
9. **Two Stations, One Problem**
   - different routing policies fit different conditions.
10. **Name the Pattern**
   - Strategy recognized after lived use.

### Side arc — Buy the Box

Sam offers a vendor package that promises automated dish routing/monitoring.

Beats:

- demo looks excellent;
- player chooses boundary/configuration/SLA;
- integration hides an assumption;
- support incident exposes observability/ownership gap;
- player may keep, modify boundary, supplement, or replace service.

Learning: outsourcing work also outsources some control and creates new coordination/contract work.

### Side arc — Ray's Shortcut

Ray's workaround improves throughput but violates the captured process under specific conditions.

Player can:

- ban it;
- document it as an exception;
- redesign process so it is unnecessary;
- automate safe detection/routing.

Learning: workers are not "noncompliant noise"; exceptions can contain domain knowledge.

---

## Chapter 2 — Warehouse: Process Becomes System

### Narrative setup

A growing distributor has inconsistent receiving, inventory exceptions, delayed scanner synchronization, and too much knowledge concentrated in leads.

### Main quests

1. **First Truck** — manually receive, inspect, stage, store.
2. **The Scanner Went Quiet** — actions must survive disconnected/delayed execution. *Command exposure.*
3. **Who Owns This Exception?** — damaged/restricted/unknown goods need bounded escalation. *Chain of Responsibility.*
4. **Count It Three Ways** — traverse inventory by location, FIFO, expiration/priority. *Iterator.*
5. **Hold the Shipment** — item/case/pallet/shipment recursive operations. *Composite.*
6. **Different Box, Different Rules** — creation/handling selection for different inbound types. *Factory Method.*
7. **The Duplicate Receipt** — optional idempotency/reliability incident.
8. **Receiving Without Heroes** — prove normal operations survive lead absence.

### Characters

Add warehouse-specific cast only when chapter work begins, using `templates/CHARACTER_PERSONA_TEMPLATE.md`. Reuse Sam/Rowan where their organizational role makes sense.

---

## Chapter 3 — Retail: Systems Need Interfaces

Primary GoF patterns:

### Adapter — **New Terminal, Old Store**

A replacement payment/scan/loyalty device speaks a different interface. Replacing the whole POS is expensive; translating at the boundary becomes attractive.

### Decorator — **One More Promotion**

Discounts, loyalty, employee rules, tax behavior, audit annotations, and promotions compose around a core sale.

Stress test: excessive decorator chains become hard to reason about.

### Facade — **One Button, Seven Systems**

Completing a sale coordinates inventory, payment, tax, loyalty, promotion, receipt, audit. Consumers need a simpler boundary.

Stress test: a facade can hide operationally important detail.

### Proxy — **Refund Authority**

Remote/sensitive refund service introduces authorization, caching, remote access, logging, or throttling concerns.

### Memento — **Suspend This Cart**

Crash/suspend/undo requires restoring a valid transaction snapshot without exposing every internal detail.

---

## Chapter 4 — Factory: Systems Become Families

### Builder — **Commissioning Day**

Configure a production cell progressively while preventing invalid combinations.

### Prototype — **Line Two**

Clone a proven line then vary selected configuration. Later reveal shallow/deep copy implications in software.

### Abstract Factory — **The Vendor War**

Choose compatible families of controllers, sensors, tooling, diagnostics.

### Bridge — **Same Job, Different Machine**

Process abstraction varies independently from equipment implementation.

### Visitor — **Inspection Week**

New safety/maintenance/energy/reporting operations must work across heterogeneous equipment.

---

## Chapter 5 — Logistics: Systems Become Networks

### Mediator — **Nobody Call Anyone**

Driver/dock/dispatch/warehouse/customer pairwise coordination explodes. A coordination service mediates.

Stress test: central mediator outage and overload.

### Singleton — **One Registry**

A bounded process needs one authoritative registry/controller/config instance.

Stress test: global reachability creates coupling; distribution breaks naïve singleton assumptions.

### Flyweight — **Ten Million Packages**

Separate shared metadata/rules from per-package state to handle huge object counts.

### Optional specialization opens

This chapter is the natural home for enterprise integration, messaging, and cloud reliability side quests.

---

## Chapter 6 — Safety-Critical Operations: Architecture Becomes Responsibility

Do not force new GoF patterns. Revisit existing ones under consequence.

Quest themes:

- alarm fatigue from Observer-like notification;
- unsafe hidden state;
- manual task gates;
- audits;
- maintenance bypass;
- recovery drill;
- over-abstracted Facade hiding critical truth;
- long responsibility chain losing ownership;
- automation requiring human override.

Goal: teach that a recognized pattern is not automatically a good design.

---

## Chapter 7 — Financial / Transactional: Architecture Must Preserve Truth

### Interpreter — **Policy Is Becoming a Language**

Hundreds of rules become a small grammar rather than hard-coded condition chains.

Side curriculum:

- Unit of Work;
- Audit Log;
- CQRS;
- Event Sourcing;
- Materialized View;
- Inbox/Outbox;
- Idempotent Receiver;
- Compensating Transaction;
- reconciliation and eventual consistency.

---

## Chapter 8 — Software / Platform: You Have Been Programming

### Rosetta questline

The player receives software problems structurally identical to prior physical/organizational systems.

```text
Physical routing     → Strategy
Machine lifecycle    → State
Notifications        → Observer
Vendor boundary      → Adapter
Action queue         → Command
Exception escalation → Chain of Responsibility
Transaction restore  → Memento
```

The game then exposes:

- conventional code;
- tests;
- safe automation/programming environment;
- PatternKit representations;
- architecture/refactoring missions.

The reveal is: **code is another representation of the systems the player already understands.**

---

# Narrative rules

1. Problems appear before terminology.
2. NPCs have incentives and bounded knowledge. They do not exist solely to deliver lessons.
3. Quests specify outcomes/evidence, not the expected implementation.
4. A "wrong" player design should usually fail through consequences rather than a red X from the tutorial.
5. Good automation creates new work: monitoring, maintenance, governance, exceptions, capacity planning, contracts.
6. Humans are not framed as defects to remove.
7. Domain experts can know things dashboards do not.
8. Evidence can contradict confidence.
9. Later chapters reuse earlier mechanics in new domains.
10. Programming terminology is delayed until concepts are familiar.

# Storyboard authoring

Every major quest uses `templates/QUEST_STORYBOARD_TEMPLATE.md` and identifies:

- normal state;
- pressure/change;
- characters and incentives;
- visible symptoms;
- hidden truth;
- player freedoms;
- success evidence;
- likely failure modes;
- follow-up reinforcement;
- pattern exposure, if any;
- replay variation.
