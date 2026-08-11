# ADR 0007: Concrete First-Hours Progression

## Status

Accepted.

## Context

The dish-station episode already produced the intended consequences, but its progression existed only as presentation-facing tutorial stages. The client had no resumable onboarding choice, quest journal, experience record, level feedback, or explicit capability rewards. A generic campaign/quest expression engine would be premature because only one complete playable scenario currently exists.

## Decision

Implement a concrete, engine-neutral dish-station progression model:

- onboarding completion, guidance preference, reduced motion, and high contrast enter through `CompleteIntroCommand` and replay with other player commands;
- eight stable `DishStationQuestId` values correspond to observable episode outcomes;
- quest completion is derived from authoritative simulation consequences, never from UI actions;
- each quest grants fixed experience and one capability that changes what the player can see or do;
- seven early career levels summarize accumulated experience but grant no throughput/stat multiplier;
- quest start/completion ticks provide active-simulation pacing telemetry without confusing it with wall-clock attention time;
- authored titles, situations, outcomes, and discoveries remain in `Automation.Content`;
- the Stride client projects the same snapshots as an intro wizard, tracker, journal, unlock messaging, and level feedback.

Do not introduce a generic condition language until a second scenario demonstrates recurring quest-evaluation needs.

## Consequences

- Headless runs, saves, and replays can prove onboarding, quest, XP, level, and unlock state.
- UI guidance can vary without changing simulation rules.
- Existing tutorial stages remain an internal episode state machine while quests provide stable player-facing units.
- Adding a second scenario will require reviewing whether these concrete rules should become a shared campaign progression service.
