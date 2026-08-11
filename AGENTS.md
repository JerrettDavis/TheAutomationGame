# Agent Development Guide

## Product intent

The Automation Game teaches systems thinking through natural discovery. Never reduce a feature to a programming tutorial when the concept can emerge through simulated consequences.

## Architectural constraints

1. `Automation.Domain` and `Automation.Simulation` must remain independent of Stride.
2. The simulation is authoritative; client presentation is a projection.
3. Consequential player changes enter through explicit commands or application services.
4. Prefer engine-neutral IDs and value types.
5. Avoid per-tick allocations in high-frequency/high-count systems.
6. Do not introduce a generic abstraction until a concrete recurring problem exists.
7. New simulation primitives require ontology review.
8. Every major simulation capability needs a headless validation path.
9. Scenario content should describe outcomes/conditions, not implementation-shaped tasks.
10. Hidden scenario conditions must be discoverable and causally explainable.

## Development workflow

For a feature:

1. Write the player/system episode.
2. Identify starting state and observable terminal outcome.
3. Identify the smallest core-domain behavior required.
4. Decide whether an unknown requires a spike.
5. Implement domain/simulation behavior headlessly.
6. Add validation and telemetry.
7. Add Stride presentation adapter.
8. Run representative scenario.
9. Update docs/ADR/content schema as needed.

## Performance

Profile before rewriting. Optimize by avoiding unnecessary work and choosing correct simulation frequency before adding concurrency or native code.

## Documentation

Update the nearest authoritative document rather than adding disconnected notes. Architecture decisions belong in `docs/adr/`.
