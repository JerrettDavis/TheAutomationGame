# ADR-0013: Controlled Automation Preset Comparison

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S025 must make experimentation explicit. Comparing the live station before and after a rule edit would confound different queue state, demand timing, operator actions, and random history. A meaningful A/B result needs preserved alternatives and identical authoritative trial conditions, while leaving the player's live world untouched.

## Decision

The simulation owns two immutable rule-preset slots, `Baseline` and `Variant`, captured from the currently applied player rule. Explicit replay-serialized commands save either slot and run a comparison for a bounded horizon.

Comparison creates two isolated `DishStationWorld` instances. Both receive the same validated scenario configuration, seed, tick horizon, demand enablement, and deterministic support-operator algorithm. Initial new-hire and automation configuration are normalized off identically so each arm installs only its captured player rule through the S024 edit/apply capability. All dish transitions, washer starts, service consumption, sticky-ready behavior, command acceptance, and queue outcomes remain ordinary authoritative simulation behavior.

Each arm records completed dishes, service shortages, automated starts, unsafe incidents, prevented unsafe requests, and the first evaluated reported-ready/physical-not-ready divergence. The result preserves both full trial inputs and evaluator evidence. Reliability is compared first (fewer unsafe incidents), then shortages, then completed throughput; this ordering makes an unsafe speedup ineligible to win. The comparison command changes only preset/result evidence in the live world.

The Automation Editor provides `B`/`V`/`R` controls to save baseline, save variant, and run comparison. Its presenter displays the shared controls, metric deltas, verdict, and the predicate difference. It does not calculate or mutate experiment results.

## Alternatives considered

- Compare metrics from two different moments in the live world. Rejected because workload and starting state would differ.
- Evaluate only the captured incident inputs. Rejected because that proves safety but cannot measure throughput or shortages.
- Estimate results from rule structure. Rejected because it would duplicate and bypass simulation semantics.
- Run an unbounded Monte Carlo or statistical suite. Rejected because one deterministic paired experiment is the proven S025 need.
- Clone private world fields directly. Rejected because commands and ordinary simulation transitions are the authoritative capability boundary.

## Consequences

### Positive

- both arms have inspectably identical experimental controls;
- measured outcomes come from real world transitions rather than presentation formulas;
- presets and comparison evidence reconstruct through replay saves;
- the player can distinguish safer, faster, and merely different rules;
- the live station is unchanged by an experiment.

### Negative

- v1 has two fixed slots and one deterministic operator/workload;
- verdict ordering is deliberately closed rather than player-configurable;
- comparison is synchronous and bounded to 10,000 ticks;
- this is not statistical evidence across a population of seeds.

## Validation

- preserve distinct reported-ready baseline and physical-ready variant rules;
- prove both arms use equal seed, scenario configuration, horizon, and workload;
- prove the baseline records an unsafe incident while the variant prevents the same divergence;
- prove the variant completes more dishes and incurs fewer shortages in the reference trial;
- prove divergence traces expose the differing PhysicalReady predicate;
- prove running a comparison does not mutate live station state;
- reconstruct slots, metrics, verdict, and evidence through replay save.

## Revisit when

- players need custom workloads, horizons, or seed batches;
- a second rule or process definition needs comparison;
- statistical confidence or performance budgeting becomes a release requirement;
- comparisons need persistence independent of command-journal reconstruction.
