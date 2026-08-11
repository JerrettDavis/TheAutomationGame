# Initial Questline: From Dishwasher to Systems Thinker

## Current playable boundary

The intro briefing and the outcome arc from **Clock In** through **Own the Shift** are implemented as the first-hours vertical slice. They include an authored journal, XP/level milestones, capability unlocks, deterministic replay/save support, headless validation, a retryable live reliability window, and native UI automation. Later vendor, multi-station, and pattern-codex quests below remain planned content rather than silently appearing as completed journal entries.

## Arc 0 — Get the job

### Quest: Clock In

Player learns movement, interaction, shift goals, and basic economic context.

No formal teaching language is introduced.

### Quest: Keep Up

Player manually works the dish station through a mild rush.

Condition: service needs clean dishes.

Discovery: work is a flow, even before the UI calls it a process.

## Arc 1 — See the process

### Quest: Where Did the Glasses Go?

Condition: bar/service runs out of glasses despite unfinished plate inventory elsewhere.

Player gains stopwatch/counting ability.

Discovery: total throughput is not enough; resource mix and bottlenecks matter.

### Quest: Draw What You Do

Manager asks the player to train someone tomorrow.

Player unlocks simple process capture by arranging observed steps.

Discovery: describing work forces hidden assumptions into view.

## Arc 2 — Delegate

### Quest: The New Hire

Player assigns work to a new NPC.

If the player captured only the obvious happy path, edge behavior differs.

Discovery: doing a job and specifying a job are different skills.

### Quest: "That's Just How We Do It"

A veteran performs an undocumented workaround.

Player unlocks knowledge/provenance inspection.

Discovery: organizations can depend on information they do not own explicitly.

## Arc 3 — Improve

### Quest: Dinner Rush

Condition: clean-glass availability and labor cost must both improve.

No prescribed solution.

Player can change layout, batching, priorities, rack inventory, or staffing.

Discovery: local optimization and system outcomes differ.

### Quest: Measure Twice

Player encounters misleading average cycle time because queue delay dominates.

Unlocks richer timing metrics.

Discovery: measure the outcome that matters.

## Arc 4 — Automate

### Quest: Automatic Start

Player gets access to a sensor/controller rule editor.

They can automate washer start when conditions appear safe.

Discovery: inputs, state, decisions, effects.

### Quest: It Said It Was Ready

Sticky status signal eventually creates an invalid start or stalled workflow.

Unlocks incident timeline.

Discovery: reported state is not necessarily reality.

### Quest: Prove the Fix

Player creates/runs several conditions, including normal operation and sticky signal.

Discovery: testing is executable evidence, not merely checking the happy path.

### Quest: Own the Shift

The player prepares the improved station and opens a live reliability window.

Condition: three real service demand checks complete without a new shortage or unsafe automation request. A failed attempt explains the observed consequence and returns to preparation.

Unlocks the shift scorecard.

Discovery: a system is ready when outcomes survive operation, not when its parts work alone.

## Arc 5 — Outsource

### Quest: Vendor Demo

A vendor offers automated sorting.

Player chooses how much discovery/specification to provide before purchase.

Discovery: implementation capability cannot compensate for undefined intent.

### Quest: The Rare Tray

A low-frequency tray jams or routes incorrectly.

The player traces whether the condition was unknown, known-but-undocumented, omitted from specification, or implemented incorrectly.

Discovery: there are different classes of failure.

## Arc 6 — Compose

### Quest: Two Stations, One Problem

The restaurant adds a second work area with similar routing rules.

Player may copy or extract reusable policy.

Discovery: reuse has benefits and costs.

### Quest: Name the Pattern

After the player creates interchangeable routing behavior, the Pattern Codex identifies Strategy.

This is the first explicit design-pattern reveal.

## Arc completion

The player is offered a process/automation role elsewhere in the organization, opening the warehouse campaign.

By this point the player has used programming concepts without text code:

- state;
- event/input;
- condition;
- decision;
- effect;
- queue;
- policy;
- fallback;
- test/scenario;
- trace;
- reusable strategy.
