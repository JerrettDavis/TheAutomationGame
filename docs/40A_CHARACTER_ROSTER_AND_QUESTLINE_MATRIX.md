# Character Roster and Questline Matrix

> Working campaign roster and named quest skeletons. These are design commitments at the level of role and narrative function, not immutable final dialogue or art direction.

Use this file to avoid repeatedly inventing a new cast or pattern quest from scratch. Before implementation, expand a quest with `templates/QUEST_STORYBOARD_TEMPLATE.md` and a new important character with `templates/CHARACTER_PERSONA_TEMPLATE.md`.

## Recurring cast

| ID | Name | Recurring function |
|---|---|---|
| `character.recurring.sam-rivera` | Sam Rivera | vendor/integrator; outsourcing, contracts, SLAs, boundaries, lock-in |
| `character.recurring.rowan-hale` | Rowan Hale | reliability/safety reviewer; failure modes, proof, recovery |
| `character.platform.morgan-pike` | Morgan Pike | late platform architect; connects lived systems to software architecture |

Recurring characters should appear only where their role plausibly brings them into the organization. Do not turn them into omnipresent mascots.

---

# Chapter 1 — Restaurant

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.restaurant.avery-chen` | Avery Chen | shift manager | outcomes, staffing, authority, cost pressure |
| `character.restaurant.ray-morales` | Ray Morales | veteran BOH worker | tacit knowledge, workarounds, domain expertise |
| `character.restaurant.jules-martin` | Jules Martin | new hire | exposes undocumented assumptions and delegation quality |
| `character.restaurant.tessa-brooks` | Tessa Brooks | service liaison | downstream demand, starvation, customer timing |
| `character.restaurant.devon-price` | Devon Price | maintenance | physical vs reported equipment state |

## Main quest sequence

| # | Quest | Primary capability/problem | Pattern relationship |
|---:|---|---|---|
| 1 | Clock In | embodied manual work | State pre-exposure |
| 2 | Where Did the Glasses Go? | observe flow/starvation | Strategy problem signature begins |
| 3 | Find the Bottleneck | waiting vs processing | process reasoning |
| 4 | Make the Flow Better | outcome-defined improvement | optimization/tradeoffs |
| 5 | The New Hire | specification/delegation | Template Method pre-exposure |
| 6 | It Said It Was Ready | reported vs real state | State primary reinforcement |
| 7 | Prove the Fix | replay/evidence | observability/validation |
| 8 | Own the Shift | unassisted operation | composition |
| 9 | Two Stations, One Problem | interchangeable policy | Strategy primary |
| 10 | Name the Pattern | reflection/codex | Strategy named |
| 11 | Stop Asking the Washer | event notification vs polling | Observer primary |
| 12 | Same Shift, Different Station | common procedure skeleton | Template Method primary |

## Side quests

- **Buy the Box** — Sam; vendor/SLA/integration boundary.
- **Ray's Shortcut** — tacit exception versus documented standard.
- **Too Many Alerts** — Observer stress test after event notifications become useful.
- **Policy for Everything** — Strategy fragmentation stress test.

---

# Chapter 2 — Warehouse

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.warehouse.elena-park` | Elena Park | receiving manager | throughput, dock schedule, staffing, authority |
| `character.warehouse.malik-thompson` | Malik Thompson | receiving veteran | practical exception knowledge |
| `character.warehouse.priya-shah` | Priya Shah | inventory control | accuracy, holds, reconciliation |
| `character.warehouse.ben-ortiz` | Ben Ortiz | scanner operator/newer worker | disconnected workflow and training |
| `character.warehouse.nina-wallace` | Nina Wallace | safety/compliance coordinator | restricted/damaged-goods escalation |

## Main quest sequence

| # | Quest | Problem | Pattern relationship |
|---:|---|---|---|
| 1 | First Truck | receive/inspect/stage/store manually | ontology reuse proof |
| 2 | The Scanner Went Quiet | actions cannot execute immediately | Command |
| 3 | Who Owns This Exception? | bounded escalation | Chain of Responsibility |
| 4 | Count It Three Ways | same inventory, multiple traversals | Iterator |
| 5 | Hold the Shipment | recursive containment | Composite |
| 6 | Different Box, Different Rules | specialized handling creation | Factory Method |
| 7 | The Duplicate Receipt | replay/duplicate operation | Idempotent Receiver side exposure |
| 8 | Receiving Without Heroes | remove dependence on one expert | knowledge/process proof |
| 9 | Rush at Door Four | composition under pressure | reinforce Command/Chain/Strategy |
| 10 | The Reuse Review | compare restaurant/warehouse structures | architectural reflection |

## Side quests

- **The Dead Scanner Queue** — retry/dead-letter concepts.
- **Expiry First** — Iterator transfer with priority ordering.
- **One Hold, Four Levels** — Composite reinforcement.
- **The Vendor ASN** — message translation/canonical model pre-exposure.

---

# Chapter 3 — Retail

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.retail.carmen-ruiz` | Carmen Ruiz | store manager | service level, shrink, labor, launch pressure |
| `character.retail.eli-grant` | Eli Grant | cashier | frontline usability and exception feedback |
| `character.retail.grace-lee` | Grace Lee | returns/customer service lead | refund state, policy, abuse controls |
| `character.retail.noor-patel` | Noor Patel | retail systems analyst | interface/integration knowledge without omniscience |
| `character.recurring.sam-rivera` | Sam Rivera | vendor/integrator | terminal/provider transition |

## Main quest sequence

| # | Quest | Problem | Pattern relationship |
|---:|---|---|---|
| 1 | Opening Rush | operate checkout manually and see subsystem boundaries | chapter baseline |
| 2 | New Terminal, Old Store | incompatible interface | Adapter |
| 3 | One More Promotion | behaviors stack around a sale | Decorator |
| 4 | One Button, Seven Systems | callers drown in subsystem coordination | Facade |
| 5 | Refund Authority | remote/sensitive access boundary | Proxy |
| 6 | Hold That Cart | suspend/restore valid transaction state | Memento |
| 7 | Promotion Pileup | excessive composition becomes opaque | Decorator stress test |
| 8 | Black Friday Rehearsal | patterns compose under demand | chapter proof |
| 9 | Close the Books | reconcile operational and transactional outcomes | finance pre-exposure |

## Side quests

- **Printer From 2009** — Adapter reinforcement.
- **The Fast Refund Cache** — Proxy/cache tradeoff.
- **The Friendly Facade** — facade hides detail needed during an incident.
- **Undo That Configuration** — Memento transfer.

---

# Chapter 4 — Factory

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.factory.marcus-bell` | Marcus Bell | production manager | output/changeover/business pressure |
| `character.factory.sofia-alvarez` | Sofia Alvarez | controls engineer | machine behavior/interfaces |
| `character.factory.dae-kim` | Dae Kim | maintenance technician | failure/commissioning reality |
| `character.factory.lena-hoffman` | Lena Hoffman | quality engineer | inspections and cross-cutting operations |
| `character.factory.aaron-price` | Aaron Price | line operator | usability, physical process knowledge |

## Main quest sequence

| # | Quest | Problem | Pattern relationship |
|---:|---|---|---|
| 1 | First Changeover | manually configure/run a cell | baseline |
| 2 | Commissioning Day | complex construction must stay valid | Builder |
| 3 | Line Two | duplicate a proven configuration with variation | Prototype |
| 4 | The Vendor War | compatible families must stay together | Abstract Factory |
| 5 | Same Job, Different Machine | job abstraction and equipment implementation vary independently | Bridge |
| 6 | Inspection Week | add new operations over heterogeneous equipment | Visitor |
| 7 | The Clone Wasn't Independent | shared-copy consequences | Prototype stress test |
| 8 | Changeover Crisis | composition + downtime/economy pressure | chapter proof |
| 9 | Lights-Out Proposal | automation versus maintainability/human recovery | ethics/tradeoff arc |

## Side quests

- **Pooled Tooling** — Object Pool where resource setup is expensive.
- **The Quality Visitor** — Visitor reinforcement with a new audit operation.
- **One Builder Too Many** — builder complexity/misuse.
- **Mixed Family** — deliberately incompatible Abstract Factory counterexample.

---

# Chapter 5 — Logistics

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.logistics.darius-cole` | Darius Cole | dispatch manager | network coordination/SLAs |
| `character.logistics.lena-foster` | Lena Foster | dock coordinator | local constraints and handoffs |
| `character.logistics.owen-hart` | Owen Hart | driver | edge/offline perspective |
| `character.logistics.imani-reed` | Imani Reed | network planner | aggregate routing/capacity |
| `character.recurring.sam-rivera` | Sam Rivera | integration/vendor role | external carrier/system boundaries |

## Main quest sequence

| # | Quest | Problem | Pattern relationship |
|---:|---|---|---|
| 1 | Monday Network | coordinate several nodes manually | network baseline |
| 2 | Everyone Calls Everyone | pairwise coordination explodes | Mediator |
| 3 | One Registry | one bounded authority simplifies local coordination | Singleton |
| 4 | Ten Million Packages | repeated metadata costs scale badly | Flyweight |
| 5 | Half the Network Vanished | partial failure and asynchronous operation | reliability specialization opens |
| 6 | The Late Event | ordering/correlation/duplication | EIP/messaging specialization |
| 7 | Two Dispatchers, One Truth | singleton assumption breaks under distribution | Leader Election/coordination transfer |
| 8 | Peak Week | distributed composition proof | chapter proof |
| 9 | Control Plane Down | mediator centralization stress test | Mediator stress test |

## Side quest families

- channels/routing/translation;
- publish-subscribe;
- split/aggregate/resequence;
- retry/circuit breaker/bulkhead;
- idempotency/inbox/outbox;
- gateway/bridge/control bus;
- cache/configuration/leader election.

These can become substantial specialization arcs without blocking the main campaign.

---

# Chapter 6 — Safety-Critical Operations

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.safety.nora-kim` | Nora Kim | operations lead | safe output and procedure |
| `character.safety.caleb-ross` | Caleb Ross | senior operator | human recovery and local knowledge |
| `character.safety.fatima-hassan` | Fatima Hassan | safety engineer | hazard analysis/controls |
| `character.safety.luis-mendez` | Luis Mendez | instrumentation tech | sensor/observation truth |
| `character.recurring.rowan-hale` | Rowan Hale | independent reviewer | drills, evidence, adversarial assumptions |

## Main quest sequence

| # | Quest | Problem | Primary lesson |
|---:|---|---|---|
| 1 | Quiet Shift | learn normal operation and safety constraints | baseline |
| 2 | Too Many Alarms | notification flood hides important signal | Observer stress test |
| 3 | Green Light, Red Reality | reported state diverges from physical state | State/knowledge stress test |
| 4 | Who Can Override? | automation needs bounded manual authority | Manual Task Gate / governance |
| 5 | Hidden Behind the Button | simplified interface masks critical condition | Facade stress test |
| 6 | The Long Escalation | responsibility chain loses ownership/time | Chain stress test |
| 7 | Drill Day | fail safely, recover, prove readiness | checkpoints/audit/recovery |
| 8 | The Review Board | explain evidence and tradeoffs | mastery reflection |

No new GoF pattern is required here. This chapter exists to make the player experience consequences and counterexamples.

---

# Chapter 7 — Financial / Transactional

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.finance.naomi-ellis` | Naomi Ellis | operations/compliance lead | policy and audit pressure |
| `character.finance.victor-chen` | Victor Chen | transaction operations analyst | process/runtime detail |
| `character.finance.amina-yusuf` | Amina Yusuf | reconciliation specialist | eventual truth and repair |
| `character.finance.theo-brooks` | Theo Brooks | customer support lead | user-visible consequences of consistency/failure |
| `character.recurring.rowan-hale` | Rowan Hale | reviewer | recovery/audit stress tests |

## Main quest sequence

| # | Quest | Problem | Pattern relationship |
|---:|---|---|---|
| 1 | The Morning Batch | transaction flow baseline | transactional model |
| 2 | Policy Is Becoming a Language | rule combinations explode | Interpreter |
| 3 | Exactly Once? | duplicate/replayed requests | Idempotent Receiver |
| 4 | Committed, But Never Sent | DB state and message publication diverge | Outbox |
| 5 | Yesterday's Truth | read model lags authoritative writes | CQRS/Materialized View/eventual consistency |
| 6 | Reverse It | completed distributed work must be compensated | Compensating Transaction |
| 7 | Audit Me | reconstruct why a decision occurred | Audit Log/Event Sourcing exposure |
| 8 | Reconcile | detect/repair disagreement | Eventual Consistency Monitor |
| 9 | Close the Quarter | composition + evidence proof | chapter proof |

---

# Chapter 8 — Software / Platform

## Working cast

| ID | Name | Role | Narrative/mechanical function |
|---|---|---|---|
| `character.platform.morgan-pike` | Morgan Pike | platform architect | Rosetta bridge from operations to code |
| `character.platform.casey-lin` | Casey Lin | site reliability engineer | runtime/failure/observability |
| `character.platform.andre-bell` | Andre Bell | application engineer | code representation/refactoring |
| `character.platform.rhea-singh` | Rhea Singh | product/domain lead | domain meaning and changing requirements |
| `character.recurring.sam-rivera` | Sam Rivera | external platform/provider | build/buy/integration replay |

## Main quest sequence

| # | Quest | Problem | Payoff |
|---:|---|---|---|
| 1 | The Familiar Bug | software system has a problem structurally identical to earlier physical work | Rosetta reveal |
| 2 | Same Shape, Different Surface | map process/state/automation concepts to code | code lens unlock |
| 3 | Name What You Built | revisit GoF patterns in conventional code | Codex Expressed state |
| 4 | Refactor Without Breaking | replace duplication/coupling while tests preserve behavior | design/refactoring |
| 5 | Make It Observable | logs/metrics/traces distinguish reality, state, knowledge | observability |
| 6 | The Failing Dependency | retries, circuit breaking, idempotency in software | reliability transfer |
| 7 | Build or Buy Again | vendor/platform boundary mirrors prior industries | architecture/economics transfer |
| 8 | Ship It | CI/testing/release strategy as operational system | delivery automation |
| 9 | The Platform Is an Organization | architecture boundaries map to teams/ownership | Conway/context-map themes |
| 10 | Your System, Your Proof | open-ended capstone with explicit evidence | campaign mastery |

## Optional PatternKit laboratory

After a pattern has been named/expressed, the player can compare:

```text
Lived implementation
Conceptual model
Conventional C#
PatternKit fluent/generated representation
Tradeoffs
```

PatternKit is a vocabulary/tool, not the only correct code representation.

---

# Cross-chapter character progression

## Sam Rivera

Restaurant: small automation vendor.

Retail: provider/integration boundary.

Logistics: multi-partner messaging/integration.

Software: managed platform/provider choice.

The player's relationship can evolve from "vendor sells solution" to "player can define and verify an explicit boundary/SLA."

## Rowan Hale

Appears after the player has enough confidence to benefit from challenge.

Progression:

```text
"What if this signal is wrong?"
→ "What if this dependency is unavailable?"
→ "Who can recover this?"
→ "Show me the evidence."
→ "How do you know the organization can operate this design?"
```

## Morgan Pike

Should be foreshadowed sparingly, perhaps through late logistics/finance correspondence or tools, but primarily belongs to the software/platform payoff. Morgan validates connections the player has already experienced instead of retroactively claiming software invented them.

# Chapter completion contract

Every chapter must ship with:

1. a manual baseline before its key abstractions;
2. at least one human/organizational problem, not only machine logic;
3. one primary systems/pattern arc;
4. one failure/counterexample;
5. one evidence/replay moment;
6. at least one optional side branch;
7. a final composition scenario;
8. a transfer hook into the next chapter;
9. chapter-specific cast with bounded knowledge/authority;
10. deterministic test scenario plus human playtest questions.
