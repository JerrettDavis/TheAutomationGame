# Core Game Loop

## Primary loop

```text
Observe
  -> Understand
    -> Define
      -> Change
        -> Run
          -> Measure
            -> Diagnose
              -> Refine
```

The player can enter this loop at different points, but skipping early activities increases uncertainty rather than being artificially prohibited.

## Minute-to-minute play

At the smallest scale, the player alternates among:

- performing work manually;
- watching workers and machines;
- inspecting resources and state;
- rearranging physical layout;
- interviewing or querying actors;
- measuring timing, queues, failures, and outcomes;
- authoring a process or rule;
- configuring a machine or automation;
- running a scenario;
- inspecting unexpected results.

## The four-model tension

Every automated capability exists across four related but non-identical models:

1. **Reality** — what actually occurs in the simulated world.
2. **Understanding** — what the player currently knows.
3. **Specification** — what the player has made explicit.
4. **Automation** — what the executable system actually does.

Gameplay emerges from gaps between these layers.

Example:

```text
Reality:
Frozen Vendor-B deliveries require inspection before storage.

Player understanding:
All frozen deliveries go to cold storage.

Specification:
temperatureClass == Frozen -> ColdStorage

Automation:
Conveyor routes all frozen packages immediately.
```

The automation may appear successful until a Vendor-B shipment arrives.

## Long-form progression loop

```text
Get responsibility
  -> learn the job
    -> improve local work
      -> formalize knowledge
        -> delegate
          -> automate
            -> scale
              -> encounter new failure modes
                -> create reusable capability
                  -> lead larger systems
```

## Work acquisition

Work enters through several channels:

- assigned job responsibilities;
- observed inefficiencies;
- customer complaints;
- incidents;
- management requests;
- regulatory or policy changes;
- worker suggestions;
- vendor changes;
- player-created goals;
- opportunities detected through metrics.

NPC requests may prescribe bad solutions. The player should be able to challenge the request and address the underlying condition instead.

## Quest structure

A quest should usually state a condition or desired outcome, not the implementation.

Weak:

> Build three conveyors.

Strong:

> Morning receiving requires two employees for three hours and frequently blocks the loading dock.

The latter allows training, layout change, scheduling, mechanization, software, or automation as legitimate responses.

## Failure loop

```text
Outcome deviates
  -> inspect expected story
    -> inspect observed story
      -> locate first divergence
        -> identify missing/incorrect assumption
          -> update model
            -> add evidence
              -> rerun
```

## Mastery loop

Late-game players spend less time directly manipulating individual work and more time:

- defining policies;
- establishing contracts;
- creating reusable capabilities;
- allocating authority;
- reviewing proposed automation;
- deciding what evidence is required;
- designing organizations;
- responding to novel conditions.

The player should feel more powerful without becoming detached from causal reality.
