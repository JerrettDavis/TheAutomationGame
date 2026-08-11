# ADR-0006: Code-Native Stride First Playable

- Status: Accepted
- Date: 2026-08-10

## Context

The bootstrap plan proposed authoring the initial client package and scene in Stride Game Studio. The first playable instead needed rapid, repeatable delivery from the ordinary .NET solution, native UI automation, and a thin projection over the authoritative simulation. No authored asset dependency is required for the dish-station greybox.

## Decision

Build the first playable as a code-native Stride `Game` using `SpriteBatch`, while retaining normal Stride package references and the simulation/client boundary. Treat Game Studio scenes and the asset pipeline as a production-presentation migration after the gameplay slice is validated, not as a prerequisite for running or testing the greybox.

## Consequences

- A clean checkout can build, test, launch, and UI-drive the client without generated scene assets.
- The current room and system lenses are deliberately greybox presentation.
- Authoritative state, commands, checkpoint semantics, and scenario content remain reusable when authored scenes replace the code-native room.
- Any Game Studio migration must preserve the headless episode, UI-driver observables, and engine-boundary tests.
