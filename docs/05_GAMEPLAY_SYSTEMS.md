# Gameplay Systems

## Work simulation

Work is represented as process instances competing for actors, resources, assets, time, and attention.

Important mechanics:

- arrival rates;
- service times;
- batching;
- setup/changeover;
- queues;
- priorities;
- resource contention;
- preconditions;
- interruptions;
- handoffs;
- rework;
- completion quality.

The first versions should prioritize comprehensibility over academically perfect operations research.

## Human work

Human actors should differ by more than speed modifiers.

Relevant properties may include:

- learned capabilities;
- familiarity with local processes;
- judgment quality;
- tolerance for ambiguity;
- physical speed;
- fatigue;
- schedule constraints;
- communication relationships;
- authority;
- morale/trust;
- propensity to improvise.

Humans can resolve unmodeled situations at a cost. Automation tends to be cheaper and more consistent inside its modeled envelope but less adaptive outside it.

## Knowledge and tribal knowledge

Knowledge has provenance and ownership.

Examples:

- "Maria knows Vendor B shipments need inspection."
- "The written receiving process does not contain this rule."
- "The automation therefore cannot act on it."

When Maria leaves, the organization may lose the capability unless knowledge has been transferred or encoded.

## Measurement

The player should be able to instrument work progressively.

Early:

- stopwatch;
- clipboard counts;
- direct observation.

Later:

- sensors;
- event logs;
- dashboards;
- traces;
- statistical analysis;
- automated anomaly detection.

Measurements have cost, precision, latency, coverage, and failure modes.

## Physical automation

Examples:

- conveyors;
- sorters;
- washers;
- robotic arms;
- scanners;
- automatic doors;
- sensors;
- machine controllers.

Physical devices should expose realistic imperfections: jams, calibration drift, maintenance, capacity limits, false readings, and failure states.

## Information automation

Examples:

- routing rules;
- scheduling;
- pricing;
- approval policy;
- inventory reconciliation;
- notifications;
- forecasting;
- orchestration.

These are where visual rule systems eventually transition into programming.

## Process specification

The player can capture a process as explicit states and interactions. Explicit specification enables:

- delegation;
- repeatable training;
- validation;
- automation;
- comparison of expected versus observed behavior;
- organizational knowledge retention.

Specification is never guaranteed to be complete.

## Outsourcing

External implementers have attributes such as:

- domain familiarity;
- technical quality;
- cost;
- speed;
- communication quality;
- maintenance availability;
- incentives;
- proprietary dependency.

Result quality is influenced by both implementer capability and the player's problem definition.

## Economy

Money should constrain decisions without turning the game into pure accounting.

Costs include:

- labor;
- equipment;
- downtime;
- maintenance;
- software/services;
- training;
- consulting;
- incidents;
- inventory;
- opportunity cost;
- coordination.

Benefits include:

- throughput;
- revenue;
- quality;
- reduced waste;
- reliability;
- customer satisfaction;
- reduced risk;
- retained knowledge.

## Reliability and incidents

Systems can fail through:

- random physical failure;
- wear;
- bad configuration;
- incorrect assumptions;
- dependency failure;
- overload;
- race/order conditions;
- stale information;
- human workaround;
- policy conflict;
- correlated rare conditions.

Incidents should be reconstructable through evidence if the player has invested in observability.

## Abstraction and reuse

When similar capabilities accumulate, the player can choose to:

- duplicate;
- parameterize;
- extract shared capability;
- establish a platform;
- intentionally keep systems separate.

Reuse reduces repeated work but increases coordination cost and blast radius.

## Organizational systems

Late-game systems include:

- teams;
- ownership boundaries;
- budgets;
- review gates;
- standards;
- shared platforms;
- vendor relationships;
- decision authority;
- release processes.

Architecture and organization affect each other through communication overhead and change lead time.
