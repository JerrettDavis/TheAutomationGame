# ADR-0017: Authoritative First-Shift Economy

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

The first shift exposed operational outcomes but did not value them. Layout, staffing, rework, shortages, and unsafe automation therefore had visible physical consequences without one comparable shift-level tradeoff. S029 needs enough economy to make those choices legible while preserving simulation authority and avoiding a generalized accounting system before a second concrete use exists.

## Decision

The dish-station scenario owns one complete engine-neutral integer rate set: completed-dish value, labor ticks per successful work action, labor cost per tick, staffing cost per enabled-worker tick, tray-rework waste, service-shortage downtime, automation-incident downtime, and flow-cell investment. Missing economy content uses the same explicit first-shift defaults; a present block is all-or-nothing and strictly validated.

`DishStationWorld` derives quantities only from accepted work actions and authoritative ticks/consequences. Its economy snapshot keeps shortage and incident downtime separately explainable, provides combined total cost and net value, and is projected live. When the reliability window passes, the shift report freezes that exact snapshot. Replay/save stores no mutable balance or transaction list: the existing authored configuration, command journal, and tick replay reconstruct identical live and completed economy state.

The bounded comparison runs the same seed, scenario, horizon, staffing command, and knowledge command twice. The only operating choice is linear station versus flow-cell investment. This concrete comparison is not a generic experiment framework or ledger.

## Consequences

### Positive

- operational changes expose comparable value, cost, and net consequences;
- every cost remains causally traceable to a simulation count;
- live UI, completed scorecard, replay/save, and headless proof share one authoritative snapshot;
- the first shift can show a real throughput/capital tradeoff without organization-scale finance.

### Negative

- integer rates are balance values rather than currency, accrual, or depreciation;
- worker staffing time and productive labor are intentionally separate costs;
- the flow-cell investment is charged once per shift rather than amortized;
- only the known first-shift rework and downtime causes are represented.

## Validation

- reject incomplete, negative, or nonpositive authored rates at semantic content paths;
- prove successful/rejected actions, staffed ticks, rework, shortages, incidents, throughput, and investment affect only their owned counters;
- replay JSON checkpoints to identical economy state and freeze the completed scorecard;
- compare viable staffed linear and flow-cell runs at seed 42 for 120 ticks and reproduce byte-identical metrics;
- keep Domain and Simulation independent of Stride.

## Revisit when

- S030 or a later industry supplies a second concrete economic shape;
- maintenance, training, vendor, lease, or software costs become actual player decisions;
- campaign persistence needs multi-shift balances or amortization;
- balance testing shows integer range or unit labels are insufficient.
