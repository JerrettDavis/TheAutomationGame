# Definition of Done

## Simulation feature

A simulation feature is done when:

- domain behavior is defined;
- ownership/state transitions are explicit where meaningful;
- headless test exists;
- deterministic behavior is validated where required;
- failure/edge behavior is considered;
- telemetry needed to explain behavior exists;
- performance impact is measured if feature runs frequently;
- no Stride dependency enters core libraries.

## Client feature

Done when:

- presentation is derived from simulation state;
- input becomes explicit player intent/command where consequential;
- reasonable LOD/pooling behavior exists;
- UI does not become authoritative state;
- feature works at supported camera scales;
- accessibility basics are considered.

## Content scenario

Done when:

- condition/outcome is understandable without implementation prescription;
- hidden facts are fairly discoverable;
- at least two plausible solution approaches exist unless the scenario is explicitly teaching one tool;
- headless scenario can run;
- failure consequences are causally explainable;
- learning target emerges through play;
- playtesters can explain what happened afterward, with first-hours evidence scored against the causal debrief protocol in `20_TESTING_VALIDATION_PERFORMANCE.md`.

## Asset

Done when:

- source/provenance exists;
- license status is recorded;
- scale/pivot/material conventions are correct;
- performance tier/LOD requirement is satisfied;
- runtime import is reproducible;
- placeholder/production status is explicit.

## Architecture change

Done when:

- ADR exists;
- alternative considered;
- dependency direction remains explicit;
- benchmark/profile evidence exists when performance is justification;
- migration/rollback is described.

## Quest

Done when:

- objective is outcome-oriented;
- completion logic is testable;
- failure does not rely on arbitrary hidden randomness;
- optional discoveries and consequences are documented;
- localization-ready text exists;
- scenario state can be replayed/debugged.
