# Save, Replay, and Determinism

## Why this matters

Replay is useful for:

- debugging;
- educational incident reconstruction;
- automated regression tests;
- multiplayer/server possibilities;
- scenario validation;
- content balancing.

## Strategy

Use:

```text
periodic snapshots
+
seeded random streams
+
ordered consequential command log
```

Do not require every microscopic internal mutation to be event sourced.

## Determinism target

Given:

- same game version;
- same content version;
- same initial snapshot;
- same seed streams;
- same command sequence;

the simulation should produce the same **gameplay-significant outcomes**.

Floating presentation interpolation does not need deterministic identity.

## Randomness

Randomness must be injected through named streams, not `Random.Shared` scattered throughout code.

Examples:

- equipment failure;
- arrival generation;
- worker variation;
- vendor reliability;
- incident scenario.

Named streams reduce accidental coupling when code changes consume random numbers differently.

## Save format

A save contains:

- schema version;
- build/content version;
- world snapshot;
- player knowledge/progression;
- active quests;
- organization state;
- random stream states;
- optional recent command/event history.

## Replay UI

Incident replay should support:

- scrub timeline;
- pause at state transition;
- compare expected versus observed;
- inspect actor knowledge at that time;
- inspect automation decision inputs;
- trace effects/dependencies.

## Tests

Automated determinism test:

1. run scenario N ticks;
2. save snapshot and command digest;
3. replay from start;
4. compare stable outcome hash;
5. restore midpoint snapshot and continue;
6. compare final hash.

## Current slice implementation

`DishStationReplaySave` records schema version, seed, validated scenario configuration, saved tick, and the ordered external command-invocation journal. Future scheduled commands, onboarding preference, quest outcomes, XP, levels, and capabilities reconstruct from that journal. `DishStationSaveStore` serializes the checkpoint as engine-neutral JSON and restores by deterministic replay, including named fault-stream state derived from the seed.

`AutomationCareerSaveStore` is the current versioned career envelope. It contains that first-shift replay, the bounded two-station routing replay, and the player's immutable pattern-knowledge profile. Pattern evidence keeps stable source quest, scenario, industry, problem-signature, player-move, replay reference, and consequence fields; milestones cite evidence IDs rather than being inferred from client state. Loading a legacy raw first-shift replay upgrades it to an empty routing journal and empty pattern profile. Explicit semantic-ID converters reject malformed or null IDs, duplicate journal entries fail validation, and atomic replacement remains the only durable write path.

The Windows client writes a durable career checkpoint after successful consequential commands and at bounded five-second intervals for simulation-driven changes. `SaveFileAtomic` flushes a sibling temporary file before replacing the career checkpoint, so a partial write does not become the continue target. On launch, an existing checkpoint opens a Continue/New Career screen. New Career requires confirmation and retains the prior checkpoint until the replacement briefing is completed. Guidance, reduced-motion, and high-contrast choices restore with the career. The save path defaults to the user's local application-data directory and can be isolated with `AUTOMATION_SAVE_PATH` for testing.

Integration validation covers atomic replacement, removal of the temporary file, legacy upgrade, malformed-ID rejection, JSON preservation of both replay journals and pattern evidence, midpoint continuation, and future-command effects. Simulation validation restores the completed live-window attempt, frozen shift-report evidence, and eight-quest progression from the replay journal, and proves that later sandbox ticks do not change career duration or the report. The native UI driver closes the process, relaunches it, exercises the protected New Career branch, and resumes the level-7 career with the passed 3/3 scorecard, both routing trials, and the recognized pre-name Codex page. God mode still exposes separate in-memory quick-save/restore through `F10` and `F11` for test setup.
