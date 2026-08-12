# ADR-0018: Concrete Two-Station Routing Trials

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

The first shift lets the player document and apply one dish-routing choice, but one use does not establish that the choice is interchangeable or context-dependent. S030 needs a second lived problem in which the same decision slot accepts different policies and produces measurable consequences. Naming the reusable pattern now would pre-empt the recognition and naming beats reserved for S031 and S032.

## Decision

Add one concrete post-shift scenario with exactly two authored station profiles: the main dish room has glass demand and the patio service station has plate demand. Both expose `ProcessRoutingPolicy` in the same routing slot. The initial glass-first choice is intentionally copied from main to patio; a five-tick authoritative trial then shows one patio shortage. Retaining glass-first at main and selecting plates-first at patio supplies both stations.

`TwoStationRoutingWorld` owns policy state, copy count, trial history, and replay commands. Set, copy, and run-trial operations are explicit commands at an authoritative simulation tick. Each station trial constructs a validated `DishStationWorld` from its authored profile, deterministic derived seed, chosen policy, and common horizon; completion, shortage, work, travel, value, cost, and net results come from that simulation rather than client scoring.

Schema v1 gains only the optional concrete `two_station_routing` scenario block needed by this episode. It requires these two known station IDs and validates their profiles and trial horizon. The quest remains separate from the eight-quest first-shift progression. Its player-facing board unlocks after `EpisodeComplete`, pauses the first-shift world while open, and projects immutable routing snapshots. It deliberately uses workplace language such as “routing choice,” “policy,” and “same decision slot,” not the conventional pattern name.

## Consequences

### Positive

- the player experiences interchangeability and local fit through simulated service outcomes;
- copy convenience and policy suitability remain separately observable;
- headless, replay, client, and screenshot evidence share the same authoritative trial results;
- Domain and Simulation remain independent of Stride;
- S031 can record evidence from an actual repeated decision instead of granting abstract knowledge.

### Negative

- the prototype models exactly two restaurant stations and three existing dish-routing policies;
- a trial creates two short-lived dish worlds, which is suitable for an explicit comparison action but not a per-tick system;
- routing trial history is replayable in its bounded world but is not yet embedded in the career save;
- the content block is concrete rather than a reusable cross-industry policy registry.

## Validation

- reject missing, duplicate, unknown, or invalid station profiles and invalid horizons;
- prove copied glass-first routing causes the authored patio shortage;
- prove fitted main/patio choices remove shortages without synthetic client scoring;
- restore the command journal to identical policies, copy count, and trials;
- compile the production scenario deterministically and run the representative headless demo twice;
- reach the board through the post-shift client path, run both trials, visually inspect the retained frame, and keep Domain/Simulation Stride-free.

## Revisit when

- S031 records pattern knowledge and needs stable evidence references;
- S032 reveals a conventional pattern name;
- a third station or another industry establishes a recurring policy-profile shape;
- career progression requires routing evidence to survive beyond the bounded episode replay.
