# Story, Quest, and Scenario System

The first playable now has an engine-neutral `DishStationEpisodeDefinition` in `Automation.Content`. Its authored data names starting situation, observable outcomes, player-facing clues, and causal evidence; keyboard instructions remain presentation/tutorial concerns rather than scenario completion conditions.

The first-hours implementation also provides eight authored `DishStationQuestDefinition` records. Simulation snapshots expose completion and progress for their stable IDs, while titles, situations, outcomes, discoveries, and reward presentation remain content. Journal detail pages make situations and terminal outcomes inspectable before completion, but reveal authored discoveries only after the simulation reports the outcome complete. The final outcome composes earlier changes in a retryable live window: three supplied demand checks and no new unsafe automation request. Completion follows real episode consequences and reconstructs deterministically from replay; it never depends on opening the journal or pressing a tutorial key.

## Purpose

Narrative content exists to expose systems and conditions, not to shepherd the player through one correct implementation.

## Hierarchy

```text
Campaign
  -> Industry
    -> Career Arc
      -> Scenario
        -> Situation / Request / Incident
          -> Quest / Goal
            -> Discoveries / Experiments / Changes
```

The player's implementation may create its own emergent subproblems.

## Scenario

A scenario establishes a simulated world with:

- facilities;
- actors;
- existing processes;
- hidden/known rules;
- equipment;
- initial metrics;
- organization structure;
- starting player role;
- latent incidents;
- progression opportunities.

A scenario should function even if the player ignores the intended teaching sequence.

## Quest philosophy

Quests should usually define **conditions and outcomes**.

Example:

> Clean glasses are unavailable during the dinner rush for an average of 18 minutes per evening.

Do not immediately say:

> Buy another dishwasher.

The player can discover whether the cause is capacity, layout, batching, staffing, glass mix, rack shortage, return delays, or something else.

## Quest types

### Work quest

Perform or learn a job.

### Observation quest

Understand why an outcome occurs.

### Improvement quest

Improve a measurable condition.

### Discovery quest

Reveal a hidden rule, constraint, or dependency.

### Research/spike quest

Answer a question before a consequential design decision.

### Incident quest

Reconstruct expected versus observed behavior and recover service.

### Delegation quest

Specify work clearly enough for another actor/team/vendor to perform it.

### Architecture quest

Resolve repeated coupling, duplicated capabilities, ownership conflict, or scale limitation.

### Modernization quest

Recover the story of a legacy system before changing it.

## Hidden conditions

Some scenarios intentionally contain conditions the player has not discovered.

Rules:

- hidden conditions must be discoverable through fair observation, questioning, evidence, or experimentation;
- consequences should be causally explainable after the fact;
- avoid arbitrary "gotcha" randomness;
- rare conditions may require enough time/load to emerge naturally.

## Story representation

Every important scenario should support an expected narrative:

```text
Given starting conditions
When actors interact
Then meaningful states change
And an outcome becomes observable
```

Incidents store an observed narrative for comparison.

## Example first arc: dish station

### Quest 1 — First shift

Goal: complete 30 minutes of dish work manually.

Discovery: work has stages, queues, roles, resources, and bottlenecks.

### Quest 2 — Dinner rush

Condition: clean glasses run out.

Discovery: throughput and bottleneck measurement.

### Quest 3 — New hire

Condition: teach someone else the process.

Discovery: tribal knowledge versus explicit specification.

### Quest 4 — Automatic detergent dispenser

Condition: inconsistent detergent dosing creates rewash.

Discovery: bounded automation and measurement.

### Quest 5 — The perfect sensor

A new fullness sensor is trusted automatically. It occasionally sticks.

Discovery: observed versus reported state; degraded behavior.

### Quest 6 — Outsource the sorter

A vendor offers an automatic sorting system. Player definition quality affects result.

Discovery: delegation, assumptions, acceptance criteria.

### Quest 7 — The rare tray

An uncommon tray type jams the automated system after hours of success.

Discovery: boundary conditions and representative validation.

## Quest completion

Avoid binary completion where possible. Record dimensions:

- outcome achieved;
- cost;
- reliability;
- worker impact;
- customer impact;
- retained understanding;
- resilience;
- maintainability.

The player may technically complete a request while creating future debt.

## Content template

See [Quest Template](templates/QUEST_TEMPLATE.md) and [Scenario Template](templates/SCENARIO_TEMPLATE.md).
