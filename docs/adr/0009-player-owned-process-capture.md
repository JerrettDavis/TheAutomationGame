# ADR-0009: Player-Owned Process Capture from Authoritative Work

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S021 begins the player-tools gate. The player must be able to turn work they perform into an explicit process artifact without introducing a generic workflow engine or deriving authored intent from every world transition. Existing dish transitions include machine cycles, service demand, automation, worker actions, and failed attempts; treating all of them as captured manual steps would produce a false process.

## Decision

Add engine-neutral process-capture entities to `Automation.Domain`: capture and artifact IDs, ordered captured steps, provenance, immutable versions, player ownership, active-session snapshots, and lifecycle events. A captured step records sequence, observed tick, actor, workstation, action, item kind, and authoritative input/output states.

Capture begins and completes through explicit replay-serialized simulation commands. While active, `DishStationWorld` records a step only after an existing `DishAction` succeeds with `DishTransitionCause.PlayerWork`. Failed commands, service demand, washer completion, automation, and new-hire work do not become authored steps.

Completing a nonempty capture creates one player-owned artifact. Its immutable baseline and current versions are both version 1 with identical steps and provenance. The baseline remains the comparison anchor; S022 may derive a later current version without mutating it. IDs are deterministic within a world/replay, and replay reconstructs active sessions, events, and completed artifacts by re-executing the same commands.

## Alternatives considered

- Convert the recent transition log directly into a process. Rejected because it mixes manual intent with environmental and automated transitions and is bounded telemetry rather than an owned artifact.
- Capture attempted commands before validation. Rejected because rejected work did not change the authoritative process state.
- Infer a generic graph immediately. Rejected because S021 proves ordered capture; routing edits belong to S022 and cross-industry structure is not yet proven.
- Store the artifact in the Stride client. Rejected because ownership, versions, and replay are gameplay truth.

## Consequences

### Positive

- captured steps correspond exactly to successful authoritative manual work;
- artifacts preserve causal state transitions and provenance;
- baseline/current semantics are ready for S022 without implementing editing early;
- capture is deterministic, replayable, headless, and engine-independent.

### Negative

- S021 captures a linear observed sequence, not branches or loops;
- machine wait time is visible in observed ticks but is not an authored work step;
- process artifacts are currently world/replay state and are not yet persisted in a separate career repository;
- only the player actor can own/capture a process in this first slice.

## Validation

- capture the five successful actions that restore one plate and inspect their exact order, workstations, state transitions, ticks, actor, and item kind;
- prove failed and non-player transitions are excluded;
- prove lifecycle validation, baseline/current v1, ownership, provenance, deterministic IDs, and replay reconstruction;
- expose the artifact in a headless capture demo while preserving the unchanged reference run.

## Revisit when

- S022 needs branching, routing, assignments, or applying edited versions;
- a second industry demonstrates reusable process-step semantics beyond dish actions;
- career persistence needs artifact migration or cross-scenario ownership;
- observation of another actor becomes an explicit player tool rather than implicit telemetry.
