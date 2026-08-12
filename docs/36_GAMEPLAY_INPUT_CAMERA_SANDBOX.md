# Gameplay Input, Camera, and Sandbox

> Production interaction contract for embodied play and player-authored system experimentation.

## Design intent

The player should begin as a person in a workplace, not as a floating management cursor. They should physically encounter queues, people, machines, handoffs, delays, and failures before gaining increasingly abstract tools.

As responsibility grows, the game may expose higher-level management views without discarding the grounded world.

## Default mouse/keyboard vocabulary

These are product defaults, not hard-coded gameplay dependencies.

| Action | Default | Notes |
|---|---|---|
| Move | `W A S D` | camera-relative or world-isometric mapping, chosen once and kept consistent |
| Interact / Work | `E` | best valid context action |
| Inspect | `F` | opens details without performing work |
| Select / click move | Left click | selection; ground click may route player |
| Camera pan | Middle-drag | right-drag acceptable only if UI/context interactions remain clear |
| Camera zoom | Wheel | clamped |
| Recenter/follow | `C` or `Home` | documented in settings |
| Journal / goals | `J` | not Q/E page stepping from gameplay |
| Lens selector | `V` | opens selector; direct lens debug keys may remain dev-only |
| Build/layout mode | `B` | when unlocked |
| Pause/back | `Esc` | modal-stack aware |

Quest mechanics must never require knowledge of developer/debug shortcuts.

## Input contexts

Physical keys are resolved through an active context stack.

Required contexts:

```text
Gameplay
Menu
Journal
Lens
Build
ProcessEditor
AutomationEditor
IncidentReplay
OrganizationView
Developer
```

Rules:

1. `Esc` closes the top modal/context before opening another menu.
2. Text entry consumes character keys before gameplay.
3. Developer actions require explicit developer mode.
4. A visible control hint comes from the action binding, never a hard-coded string such as "Press E".
5. Rebinding operates on logical actions.

## Direct movement

### Authority

Input does not teleport the presentation pawn. It requests movement through simulation/application commands.

Preferred conceptual flow:

```text
Input sample
  ↓
Logical movement intent
  ↓
Resolve target neighboring cell / movement segment
  ↓
MovePlayerCommand
  ↓
Authoritative world transition
  ↓
Presentation interpolation
```

The renderer may interpolate between authoritative positions for feel. The simulation owns where the actor is allowed to be.

### Grid/topology

The first production implementation may remain discrete even when the scene is visually 3D.

A facility exposes:

- traversable cells/nodes;
- blocked cells/volumes;
- workstation footprints;
- interaction ports;
- doors/links;
- optional cost/zone metadata.

This allows deterministic routing and preserves clean headless behavior.

### WASD semantics

Pick one mapping and test it with the isometric camera:

**Preferred:** camera-relative cardinal intent projected onto the facility grid. Pressing W should visually move "up the screen/away from camera" enough to feel conventional.

Diagonal simultaneous input can resolve to one of:

- true diagonal neighbor when valid; or
- ordered cardinal steps.

Whichever is chosen must be deterministic and consistent.

### Click-to-move

Click-to-move is retained as an alternative input surface, not a separate movement system.

```text
Ground click
  ↓
Resolve authoritative destination
  ↓
Path over facility topology
  ↓
Issue same movement semantics used by direct navigation
```

Use deterministic A* or equivalent only when obstacles make it necessary. Do not introduce navmesh complexity before the authored 3D room requires it.

### Implemented S011 topology

The first dish-station room uses a concrete engine-neutral topology derived from its authoritative `DishStationPlacements`:

- the 13×8 floor bounds are walkable except for the six single-cell workstation footprints;
- each workstation resolves one deterministic adjacent cardinal interaction port;
- direct movement is one cardinal or diagonal neighbor per `MovePlayerCommand`, with diagonal corner cutting prohibited;
- click movement uses deterministic breadth-first route generation with a fixed neighbor order, then queues the same `MovePlayerCommand` steps accepted by direct input;
- clicking a workstation footprint resolves to its interaction port; pressing WASD cancels any remaining click route;
- layout changes reject overlapping fixtures, player overlap, sealed ports, and disconnected ports.

Ontology review: this is a spatial value/constraint over existing `FloorCell`, `DishStationFixture`, and `DishStationPlacements` concepts. It introduces no new simulated entity, resource, process state, or content ontology type. Stride presentation consumes the topology but does not own collision or player location. Multi-cell footprints, weighted terrain, dynamic doors, and navigation meshes remain future concrete requirements rather than generalized S011 abstractions.

## Interaction ports

A workstation can occupy many cells but expose one or more interaction ports.

Example:

```yaml
id: workstation.washer.01
footprint: [[4,3], [5,3]]
interaction_ports:
  - id: load
    cell: [4,4]
    actions: [load, inspect]
  - id: unload
    cell: [5,4]
    actions: [unload, inspect]
```

This avoids "stand anywhere and press work" ambiguity and creates meaningful layout constraints.

## Context action resolution

When the player presses Interact:

1. gather interactables in permitted range;
2. prefer currently selected valid target;
3. otherwise score by facing/proximity/priority;
4. determine best currently valid action;
5. issue its command;
6. if no action is valid, show why rather than doing nothing silently.

HUD example:

```text
[E] Load washer
[F] Inspect
```

Blocked:

```text
[E] Load washer
    Rack is empty
```

Do not create quick-time events for ordinary work. Dexterity mechanics should exist only if human motor skill is itself part of the operational tradeoff being modeled.

## Camera contract

### Near zoom

Purpose: embodied work and spatial detail.

Must show:

- character intent/facing;
- workstation state;
- interaction affordances;
- local queue/item state.

### Mid zoom

Purpose: process flow.

Must show:

- several stations;
- queues;
- worker movement;
- alert/goal summaries.

### Far zoom

Purpose: operational overview.

Must show:

- zones;
- aggregate flow/pressure;
- high-level alerts;
- minimal world labels.

Do not render every label/icon at every zoom. Use semantic zoom.

### Occlusion

For 3D rooms, select a deterministic visual strategy:

- fade/cut away camera-facing walls;
- fade large obstructing props when actor/selection is behind;
- preserve collision even when presentation fades.

This is presentation only.

## Sandbox progression

"Sandbox" should mean progressively more powerful authoring tools, not unrestricted magic from minute one.

### Stage 1: Manual operation

Player can move, work, inspect.

### Stage 2: Layout

Player can relocate permitted buffers/equipment within constraints.

Constraints may include:

- footprint;
- walkability;
- utilities;
- safety clearance;
- ownership/permission;
- money;
- downtime.

### Stage 3: Process

Player captures/edits routing and work definitions.

S021 establishes capture as authoritative model state: explicit start/complete commands collect only successful player work into an owned, versioned artifact with ordered steps and provenance. Editing, routing changes, and applying a derived current version remain S022.

S022 supplies that first editor. The modal pauses early-game simulation, displays baseline/current/draft identity, allows stable captured steps to be selected and reordered, assigns each action to the player or new hire, and chooses captured-order, plates-first, or glasses-first routing. Apply validates sequence/state compatibility and creates an immutable next current version while preserving baseline v1. The applied assignment/routing definition controls delegated work; it does not mutate dishes directly.

### Stage 4: Automation

Player creates deterministic rules/policies/triggers.

### Stage 5: Organization

Player assigns responsibilities, purchases services, defines standards, and delegates.

### Stage 6: Free experimentation

Scenario/sandbox modes can relax money/progression while preserving the same simulation semantics.

## Build/layout editing

Build mode should:

- pause by default in early game;
- display footprints, ports, walkability, utilities and invalid placement reasons;
- preview consequences before commit where possible;
- require deliberate confirmation for destructive/expensive changes;
- issue normal authoritative commands when committed.

Later real-time construction can be introduced if it adds meaningful operational pressure.

## Process editing

The process editor edits **definitions/policies**, not direct entity state.

It must separate:

- what normally happens;
- current live state;
- observed evidence;
- automation attached to the process.

Players should be able to maintain baseline and variant versions and replay both under a controlled scenario.

## Automation editing

Automation authoring should expose:

```text
WHEN <observable condition>
IF   <predicate>
THEN <allowed action/policy>
```

Advanced forms can become graphs, strategies, scripts, or code later.

Every execution should be inspectable:

```text
Rule: glass-rush-routing
Tick: 1842
Condition: clean_glass_inventory < 12
Observed: 8
Result: true
Action: set route priority = glasses
Outcome: applied
```

That trace is critical to the game's "do not outsource your thinking" thesis.

S024 implements the first bounded authoring surface: one player-owned washer rule with editable enabled state and rack-present, reported-ready, and physical-ready Boolean conditions over the closed `StartWasher` action. Draft changes enter through replay-serialized simulation commands, invalid drafts remain visible with targeted diagnostics, apply replaces the live rule at a stable ID, and the paused editor projects the evaluator's latest observed inputs, predicate results, effect selection, and authoritative command outcome. Multiple rules and arbitrary actions remain later work.

S025 adds immutable baseline/variant slots and a paired controlled replay. Both rules run in isolated authoritative worlds with identical scenario configuration, seed, horizon, demand, and deterministic support work. The editor presents completed work, shortages, starts, unsafe incidents, prevented requests, and the first readiness-divergence predicates side by side. Running the experiment does not mutate the live station.

## Acceptance checklist

The input/sandbox foundation is ready when:

- a new player can move and work without coaching;
- camera motion never steals ordinary movement keys;
- production quests do not require direct debug bindings;
- direct movement and click movement obey the same world constraints;
- world state remains deterministic under replay;
- build/process/automation tools modify explicit game artifacts, not hidden scenario variables.
