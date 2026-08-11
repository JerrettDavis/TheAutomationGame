# Open Questions and Spikes

These are intentionally unresolved. Resolve through small experiments rather than broad architectural debate.

## Client/rendering

- What visible object count can Stride sustain with our target stylized presentation using entities, instancing, and pooling?
- Which Stride UI approach is appropriate for information-dense management interfaces?
- How should world selection/picking work for instanced objects?
- What is the practical camera/world-coordinate limit before precision techniques are needed?
- How much of movement animation is client-interpolated versus simulation sampled?

## Simulation

- Do workers need continuous navigation simulation at all scales, or can distant travel be scheduled/aggregated?
- Which pathfinding approach handles hundreds to thousands of active agents acceptably?
- How should process definitions balance generic data with compiled behavior hooks?
- What is the minimum knowledge model needed for believable tribal knowledge without an expensive belief system?
- How should fidelity transitions preserve queue/resource invariants?

## Content

- YAML vs another authoring format after prototype feedback?
- Do designers need a custom process/scenario editor before the second industry?
- How much hidden information is enjoyable before it feels adversarial?
- How should quests communicate consequences without revealing the intended concept too early?

## Progression

- Does the player control one avatar throughout, switch roles, or become an organizational perspective over time?
- Should time advance continuously while editing process/automation definitions?
- How much career simulation (salary, housing, personal life) strengthens the fantasy versus distracts from systems play?

## Programming

- What is the exact automation intermediate representation?
- Can a useful subset round-trip between visual and C#-like text representation?
- Should the eventual player language be real C#, restricted C#, or a purpose-built DSL?
- How do we sandbox user scripts for shared/mod content?

## Multiplayer

- Is multiplayer collaborative sandbox, competitive consulting, shared organization ownership, or deferred entirely?
- Deterministic command architecture should keep the option open, but no networking implementation belongs in first playable.

## Business/platform

- Windows-only early access versus broader desktop support?
- Workshop/mod distribution strategy?
- Educational scenario licensing/content packs?

## Spike rule

Each spike must answer one decision question and produce an artifact:

- benchmark;
- tiny prototype;
- comparison;
- recorded recommendation;
- updated ADR if consequential.

Avoid open-ended "research Stride" spikes.
