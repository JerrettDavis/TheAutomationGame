# ADR-0004: Fixed/Scheduled Simulation Time Independent of Rendering

- Status: Accepted
- Date: 2026-08-10

## Decision

Authoritative simulation advances through deterministic ticks/scheduled phases independent of render frames.

## Notes

Different systems may run at different frequencies. Rendering interpolates presentation state.

## Benefits

- replay;
- headless acceleration;
- stable testing;
- scalable scheduling;
- no need to evaluate business rules at 120 FPS.
