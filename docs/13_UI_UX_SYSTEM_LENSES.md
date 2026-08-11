# UI/UX and System Lenses

## Core interface principle

The player views one underlying world through progressively richer lenses. Lenses reveal information; they do not create separate game realities.

## Reality lens

Shows people, objects, machines, buildings, movement, and visible outcomes.

Use for:

- direct work;
- physical layout;
- observation;
- immersive understanding.

## Process lens

Shows:

- stages;
- flow arrows;
- queues;
- work items;
- bottlenecks;
- role responsibility;
- time spent.

## Interaction lens

Shows actors and messages/actions among them.

Useful for handoffs and system boundaries.

## State lens

Shows valid states, current state, recent transitions, blocked/illegal transitions, and triggering events.

## Knowledge lens

Shows who knows what:

- documented;
- tribal;
- uncertain;
- stale;
- assumed;
- inaccessible.

This is critical for delegation and legacy-system gameplay.

## Automation lens

Shows which responsibilities are performed by:

- human;
- machine;
- software rule;
- external vendor;
- AI/agent;
- fallback/manual path.

## Architecture lens

Shows capabilities, contracts, shared services, dependencies, ownership, and blast-radius relationships.

## Runtime lens

Shows:

- latency;
- queue depth;
- failures;
- retries;
- throughput;
- utilization;
- trace chains;
- dependency health.

## Code lens

Late-game view of the same process/state/decision models as text code.

The player should be able to correlate visual nodes with generated or authored code.

## Incident view

A dedicated debugger compares:

```text
Expected timeline
-----------------
A -> B -> C -> D

Observed timeline
-----------------
A -> B -> X

First divergence: X
```

Then allows traversal into the cause.

## Progressive disclosure

Do not expose all lenses immediately. New lenses unlock when the player has encountered a problem that makes them useful.

The Shift Handbook follows the same rule. It is an opt-in control reference, not a quest solution panel: core movement and work controls are always visible, while layout, delegation, exception, automation, trace, reliability, and scorecard actions appear only after their underlying capability or situation exists. Its current-opportunity text may identify an available interaction, but hidden causal discoveries remain in the world and quest record.

Card-shaped onboarding and progression actions are real pointer targets, not keyboard-only decoration. Intro continuation, guidance and comfort choice, career selection, journal rows/details, handbook close, and scorecard close retain keyboard equivalents. Both paths dispatch the same presentation intent; neither path owns consequential world state.

Progression feedback explains causality rather than merely celebrating accumulation. One receipt keeps the completed outcome, XP, actual level threshold, capability, and “why now” rationale together long enough to read. Do not let a generic level animation suppress the specific tool relationship, and do not label a same-level capability reward as a level-up.

Consequence-bypassing sandbox tools are not part of a fresh career. They unlock after the first-shift outcome so experimentation can continue without invalidating discovery, or through an explicit development-run opt-in. Human readiness sessions use the locked player path.

## Accessibility

Important state cannot depend solely on color. Use icons, shapes, labels, animation, and configurable overlays. Camera and UI scaling must support high-density desktop play.
