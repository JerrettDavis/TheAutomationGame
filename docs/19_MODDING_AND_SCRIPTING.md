# Modding and Scripting

## Long-term goal

The Automation Game should be capable of becoming a programmable systems laboratory. Modding is not required for the first playable, but architecture should avoid foreclosing it.

## Mod layers

### Data-only mods

Create:

- industries;
- equipment;
- jobs;
- scenarios;
- quests;
- processes;
- incidents;
- progression content.

This is the safest first mod surface.

### Visual automation scripting

Players compose decisions, events, state transitions, and effects through game tools.

### Text scripting

Late-game/player-authored code represents the same models more compactly.

Potential approaches to evaluate later:

- restricted C# scripting;
- embedded language;
- compiled mod assemblies with explicit trust model;
- WebAssembly sandbox.

Do not select a scripting runtime until security, distribution, debugging, and mod portability requirements are clearer.

## Code view principle

Visual and code views should ideally operate over the same intermediate model where practical.

```text
Visual Rule Graph
      ↕
Automation IR
      ↕
Text Representation
```

Full arbitrary C# cannot always round-trip to a visual graph. The supported visual subset must be explicit.

## Mod API stability

Expose stable domain/content contracts, not Stride internals. Mods should not need to know the client renderer unless they provide custom presentation content.

## Workshop/ecosystem

Future possibilities:

- community industries;
- challenge scenarios;
- automation puzzles;
- reusable capability packs;
- educational curricula;
- competitive optimization scenarios.
