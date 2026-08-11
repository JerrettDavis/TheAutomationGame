# First Playable: Dish Station

## Purpose

The dish station is a deliberately small operational system capable of teaching the game's complete conceptual loop without requiring a large content library.

The player should move from manual work through observation, process improvement, specification, delegation, automation, failure, debugging, and refinement in one physical room.

## Player setup

The player has just been hired at a busy casual restaurant. Their first responsibility is the dish station.

The environment contains:

- dining room dish return;
- dirty-dish staging;
- scrape/trash station;
- rack storage;
- commercial dishwasher;
- drying area;
- clean-dish shelving;
- service stations consuming plates/glasses;
- one experienced coworker;
- one manager;
- dinner demand cycle.

## Core resources

- plates;
- glasses;
- utensils;
- uncommon trays;
- dish racks;
- detergent;
- clean water/machine capacity;
- worker attention;
- floor/staging space.

## Baseline process

```text
Used item returned
  -> staged dirty
    -> scraped/sorted
      -> loaded into rack
        -> rack queued
          -> machine cycle
            -> unload
              -> dry
                -> return to service
```

## State model

Initial dish states:

```text
InUse
Dirty
Staged
Scraped
Racked
QueuedForWash
Washing
CleanWet
CleanDry
Available
NeedsRewash
```

Do not over-model sanitation chemistry in the first playable. Represent enough quality state to create rewash and incorrect-processing consequences.

## Manual interactions

The player can initially:

- pick up/move dishes/racks;
- scrape;
- sort;
- load rack;
- start washer;
- unload;
- move clean dishes;
- restock service stations.

## Simulated demand

Dining/service consumes clean dishes at a rate driven by customer volume and menu mix.

Dinner rush should expose starvation of specific resources, especially glasses, even when total dish throughput appears acceptable.

## Initial hidden knowledge

The experienced coworker knows several facts not present in any formal process:

1. a particular tray must use a different rack orientation;
2. loading glasses too tightly increases rewash probability;
3. during the rush, glass racks get priority over plates;
4. a sticky machine-ready indicator occasionally remains lit after a fault.

These conditions are discoverable through observation and conversation.

## Improvement opportunities

Player solutions can include:

- change staging layout;
- increase rack count;
- change batch policy;
- prioritize glasses;
- change staffing;
- formalize tray handling;
- add simple visual controls;
- instrument cycle times;
- maintain/repair machine;
- introduce sensors/automation later.

## First automation

Introduce an automatic rack-start or routing decision after the player understands the manual process.

Example automation:

```text
IF machine reports Ready
AND rack present
THEN start wash cycle
```

The sticky ready signal creates a fair failure after sufficient successful cycles.

The player learns to distinguish:

```text
machine-reported ready
!=
physical machine actually ready
```

Potential fixes:

- corroborating sensor;
- timeout;
- machine-state model;
- operator confirmation;
- maintenance;
- fallback/manual mode.

No single fix is mandatory.

## Delegation moment

A new employee arrives. The player can:

- demonstrate work informally;
- provide explicit process definition;
- rely on experienced coworker;
- automate selected decisions.

If rare tray knowledge is not transferred, the new worker eventually causes a jam/rewash event.

## Outsourcing moment

A vendor offers a sorting aid/automation. The delivered result depends on the player's definition packet and discovered knowledge.

The vendor should not be arbitrarily incompetent. Missing requirements become assumptions.

## Required lenses

First playable must demonstrate:

1. Reality lens
2. Process lens
3. State lens
4. Knowledge lens
5. Automation lens
6. Runtime/incident lens

Architecture/code lens may remain locked but internal data should support them later.

## Success criteria

A new player can complete the arc and afterward explain, in ordinary language:

- the real process;
- its bottleneck;
- why their first improvement helped;
- why an automation failed;
- what assumption was wrong;
- what evidence revealed the problem;
- how the refined system is safer;
- why fully automating every step was not automatically optimal.

## Headless model requirements

The dish station must run without graphics with configurable:

- arrival rates;
- dish mix;
- rack capacities;
- worker speeds;
- machine cycle times;
- machine failure rates;
- knowledge distribution;
- automation policies;
- demand profile.

This becomes the first enduring simulation benchmark and regression scenario.

Implemented by `DishStationScenarioConfiguration` and exposed by `Automation.Headless`: initial dish counts and arrival mix, rack capacity, worker intervals, washer cycle time, sticky-ready threshold plus deterministic per-start risk, initial knowledge, automation policy, demand kind/rate, rush state, and layout are validated at the world boundary. Use `--empty` for timing variants because the demonstration command schedule intentionally targets the default episode.

## Current implementation status

The runnable greybox implements the first bounded episode:

1. select scrape, rack, washer, unload, and dry/restock workstations;
2. return one plate to service through explicit commands;
3. enable dinner-rush glass demand;
4. observe a causally triggered glass shortage;
5. inspect accumulated item-ticks, peak queue depth, oldest-item age, completed average residence time, and the pressure-leader candidate through the process lens;
6. select a bottleneck hypothesis, record the 22-step baseline route, and arrange a U-shaped flow cell;
7. validate the same state sequence at 10 steps by moving a glass through the constrained flow until service consumes it;
8. complete the episode or reset/configure it through god mode.

The delegation continuation is also runnable: enable the new hire, transfer the happy-path process, observe that plates remain their default priority, add the missing rush glass-priority knowledge, and validate the new behavior when service consumes a worker-produced glass. An uncommon tray then returns to dirty rework when its orientation fact is absent and completes after the knowledge is documented. The worker is identified by an engine-neutral actor ID and all training changes enter through explicit commands.

The automation continuation is runnable after the tray exception is resolved: enable reported-ready automatic start, observe the sticky-ready incident, inspect the first divergence between the reported signal and physical machine state, then install and validate a physical-readiness interlock. Automation changes and fault injection enter through explicit commands; the policy and snapshot are engine-neutral, while Stride only projects them.

The runtime/incident lens retains a bounded decision trace and compares the intended invariant with observed readiness. The player replays the captured inputs once against the original policy to reproduce the failure and again against the corrected policy to establish a regression case. The native UI smoke driver exercises this path through the actual Stride window rather than bypassing presentation input.

The first-playable lens set is now runnable through `V`: reality shows the physical station, process shows queues and timing, state shows current counts plus authoritative transition causes, knowledge compares observed and explicitly transferred facts, automation shows the bounded rule and retained manual work, runtime shows the incident trace, and responsibility shows capability ownership plus the contained blast radius. Lenses progressively unlock as the episode creates a reason to use them.

The same episode runs headlessly and validates the complete tutorial progression, including the baseline/layout comparison. The first intended dish-station arc and its headless configuration matrix are represented end to end; subsequent production work can deepen authored assets, audio, accessibility options, persistence migration, content tooling, and broader campaigns without another missing first-playable episode beat.

## First playable acceptance evidence

| Requirement | Authoritative evidence |
|---|---|
| Runnable Stride client and shared headless simulation | Release solution build plus `Automation.Client.Stride.Windows` and `Automation.Headless` consuming `DishStationWorld` |
| Manual process, demand, diagnosis, layout, delegation, exception, automation, incident, and correction | `TutorialEpisodeAdvancesFromManualWorkToEvidence` and native `tools/ui-smoke.ps1 -AllowDesktopInput` completion on an idle desktop |
| Simulation owns consequential state | command-only client dispatcher, simulation snapshots, and `CoreAssembliesDoNotReferenceStride` |
| Discoverable, outcome-oriented scenario | `DishStationEpisodeDefinition.FirstPlayable` and its outcome/discovery content validation |
| Reality, process, state, knowledge, automation, runtime, and responsibility views | UI-driver traversal with a captured screenshot for each lens |
| God/setup path | `F1` controls for supply, fault, layout, pause/step, benchmark, quick-save, and restore; the UI driver validates save–mutate–restore |
| Configurable headless scenario | `DishStationScenarioConfiguration`, CLI `--help`, focused configuration tests, and a non-default runner proof |
| Determinism and recovery | same-seed tests, captured-incident replay, versioned JSON midpoint restore, and identical future-command continuation |
| Scale boundary | 100k actors × 100 ticks headlessly with deterministic checksum; 10k sampled visuals batch-rendered in god mode |
| Leftmost-monitor review delivery | native driver resolves the leftmost Windows display, positions the actual client, and retains it with `-KeepOpen` |

Verified locally in Release on 2026-08-10 with zero build warnings, the full automated suite, default headless completion, the 100k benchmark, and the native UI smoke path.
