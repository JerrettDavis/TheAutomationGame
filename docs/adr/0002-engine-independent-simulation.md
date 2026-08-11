# ADR-0002: Engine-Independent Simulation Core

- Status: Accepted
- Date: 2026-08-10

## Decision

All authoritative gameplay simulation lives in ordinary .NET libraries independent of Stride.

## Rationale

The game concept is fundamentally a simulation platform. Renderer/editor ownership would create lock-in, make headless validation difficult, and encourage one engine entity per simulated concept.

## Consequences

- more explicit presentation mapping;
- easy headless runs and tests;
- future renderer replacement is possible;
- simulation data structures can optimize independently;
- client cannot directly mutate authoritative state.
