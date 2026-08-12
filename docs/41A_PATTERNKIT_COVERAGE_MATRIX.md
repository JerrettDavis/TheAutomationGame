# PatternKit Coverage Matrix

> Planning placement for the current PatternKit catalog. The imported catalog remains authoritative if names/counts change.

## Catalog summary

| Category | Count |
|---|---:|
| Application Architecture | 29 |
| Behavioral | 12 |
| Cloud Architecture | 21 |
| Creational | 6 |
| Enterprise Integration | 42 |
| Messaging Reliability | 4 |
| Structural | 7 |
| **Total** | **121** |

Classic GoF coverage is the 23 entries marked **Main** below. `Null Object` and `Object Pool` remain useful PatternKit additions but are not counted among the GoF 23.

## Placement matrix

| Pattern | Catalog | Path | Primary placement | First problem/story hook | Reinforcement |
|---|---|---|---|---|---|
| Activity Tracker | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Aggregate Root | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Anti-Corruption Layer | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Audit Log | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Bounded Context | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Context Map | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| CQRS | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Data Mapper | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Domain Event | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Domain Service | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Event Sourcing | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Eventual Consistency Monitor | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Feature Toggle | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Identity Map | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Lazy Load | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Manual Task Gate | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Materialized View | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Ports and Adapters | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Repository | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Service Layer | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Snapshot / Checkpoint Management | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Specification | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Table Data Gateway | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Timeout Manager | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Transaction Script | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Unit of Work | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Compensating Transaction | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Value Object | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Workflow Orchestration | Application Architecture | Side specialization | Financial / Software-Platform | Architecture, domain ownership, persistence or workflow problem | Organization-scale / Software |
| Chain of Responsibility | Behavioral | Main | Warehouse | Exception escalation/ownership | Software / Platform code revisit |
| Command | Behavioral | Main | Warehouse | Disconnected scanner actions become queueable operations | Software / Platform code revisit |
| Interpreter | Behavioral | Main | Financial | Policy rules become a grammar | Software / Platform code revisit |
| Iterator | Behavioral | Main | Warehouse | Multiple inventory traversal orders | Software / Platform code revisit |
| Mediator | Behavioral | Main | Logistics | Dispatch/control coordination hub | Software / Platform code revisit |
| Memento | Behavioral | Main | Retail | Suspend/restore transaction state | Software / Platform code revisit |
| Null Object | Behavioral | Side specialization | Software-Platform | Optional behavior creates null/special-case branching | Architecture refactor mission |
| Observer | Behavioral | Main | Restaurant | Readiness/inventory event notification | Software / Platform code revisit |
| State | Behavioral | Main | Restaurant | Machine/item lifecycle; reported vs real state | Software / Platform code revisit |
| Strategy | Behavioral | Main | Restaurant | Rush versus normal routing policy | Software / Platform code revisit |
| Template Method | Behavioral | Main | Restaurant | Shared procedure skeleton with variable station steps | Software / Platform code revisit |
| Visitor | Behavioral | Main | Factory | New inspections over heterogeneous equipment | Software / Platform code revisit |
| Ambassador | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Backends for Frontends | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Bulkhead | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Cache-Aside | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Cache Stampede Protection | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Circuit Breaker | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Distributed Lock / Lease | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| External Configuration Store | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Gateway Aggregation | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Gateway Routing | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Health Endpoint Monitoring | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Leader Election | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Priority Queue | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Queue-Based Load Leveling | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Rate Limiting | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Read-Through Cache | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Retry | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Scheduler Agent Supervisor | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Sidecar | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Strangler Fig | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Write-Through Cache | Cloud Architecture | Side specialization | Logistics / Software-Platform | Distributed-system scale or failure creates the need | Organization-scale |
| Abstract Factory | Creational | Main | Factory | Compatible equipment/system families | Software / Platform code revisit |
| Builder | Creational | Main | Factory | Progressive valid cell commissioning | Software / Platform code revisit |
| Factory Method | Creational | Main | Warehouse | Handling type selects concrete creation path | Software / Platform code revisit |
| Object Pool | Creational | Side specialization | Factory / Software-Platform | Expensive reusable resource pressure | Performance/scaling mission |
| Prototype | Creational | Main | Factory | Clone proven line/config then vary | Software / Platform code revisit |
| Singleton | Creational | Main | Logistics | One bounded authoritative instance, then misuse stress test | Software / Platform code revisit |
| Aggregator | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Canonical Data Model | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Change Data Capture | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Channel Adapter | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Channel Purger | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Claim Check | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Competing Consumers | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Content Enricher | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Content-Based Router | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Control Bus | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Correlation Identifier | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Dead Letter Channel | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Durable Subscriber | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Dynamic Router | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Event Notification | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Event-Carried State Transfer | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Event-Driven Consumer | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Guaranteed Delivery | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Invalid Message Channel | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Mailbox | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Bus | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Channel | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Envelope | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Expiration | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Filter | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message History | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Store | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Message Translator | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Messaging Bridge | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Messaging Gateway | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Pipes and Filters | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Polling Consumer | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Publish-Subscribe | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Recipient List | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Request-Reply | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Resequencer | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Routing Slip | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Saga / Process Manager | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Scatter-Gather | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Service Activator | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Splitter | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Wire Tap | Enterprise Integration | Side specialization | Logistics | Systems begin exchanging asynchronous messages | Financial / Software-Platform |
| Backpressure | Messaging Reliability | Side specialization | Logistics / Financial | Messaging failure/replay/delivery pressure | Software-Platform |
| Idempotent Receiver | Messaging Reliability | Side specialization | Logistics / Financial | Messaging failure/replay/delivery pressure | Software-Platform |
| Inbox | Messaging Reliability | Side specialization | Logistics / Financial | Messaging failure/replay/delivery pressure | Software-Platform |
| Outbox | Messaging Reliability | Side specialization | Logistics / Financial | Messaging failure/replay/delivery pressure | Software-Platform |
| Adapter | Structural | Main | Retail | New terminal/provider versus legacy interface | Software / Platform code revisit |
| Bridge | Structural | Main | Factory | Process abstraction independent of machine implementation | Software / Platform code revisit |
| Composite | Structural | Main | Warehouse | Item/case/pallet/shipment recursive operations | Software / Platform code revisit |
| Decorator | Structural | Main | Retail | Composable sale/pricing behaviors | Software / Platform code revisit |
| Facade | Structural | Main | Retail | Simple sale boundary over coordinated subsystems | Software / Platform code revisit |
| Flyweight | Structural | Main | Logistics | Share repeated package metadata at scale | Software / Platform code revisit |
| Proxy | Structural | Main | Retail | Remote/sensitive refund/provider boundary | Software / Platform code revisit |

## Coverage rules

- Every **Main** entry requires a primary encounter in the campaign, at least one transfer exposure, a tradeoff/stress-test plan, and a software/code revisit.
- Side-specialization entries are problem-driven. They do not need equal screen time or a mandatory unlock quest.
- Do not expose a pattern merely because its catalog row is incomplete. Create a scenario only when the game contains the problem that makes the pattern meaningful.
- The coverage report should be generated from content + the imported PatternKit catalog rather than manually trusting this markdown forever.
- If PatternKit adds/removes/renames entries, catalog validation should surface the drift and require an explicit game overlay decision.

## Suggested specialization groupings

### Reliability / resilience

Cloud and messaging patterns centered on Retry, Circuit Breaker, Bulkhead, Backpressure, Rate Limiting, Queue-Based Load Leveling, Health Endpoint Monitoring, Idempotent Receiver, Inbox, and Outbox.

### Enterprise integration

Channels, envelopes, translation, routing, publish/subscribe, splitting/aggregation, delivery/recovery, gateways/bridges, orchestration, and control.

### Application architecture

Domain boundaries, persistence, workflow, consistency, audit, CQRS/event sourcing, ports/adapters, repositories/services, and specification.

### Distributed/cloud platform

Gateway patterns, Sidecar/Ambassador, caches, distributed coordination, external configuration, leader election, scheduler supervision, and Strangler Fig.

## Review milestone

Revisit this matrix after the warehouse reuse audit. At that point the game will have two concrete industries and enough evidence to turn the broad side-specialization placements into fixed quest tranches without inventing abstractions prematurely.
