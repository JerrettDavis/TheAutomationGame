# Stride Client Plan

## Baseline

Use Stride 4.3 with .NET 10/C# 14 as the initial presentation engine. Stride is MIT-licensed and its projects are standard C# solutions. Game Studio provides scene and asset editing, while standard C# classes remain available independently of entity-attached scripts.

Exact package versions must be pinned during repository bootstrap and upgraded deliberately through ADR/change notes.

## Responsibilities owned by Stride client

- window/platform bootstrap;
- rendering;
- scene presentation;
- camera;
- input;
- UI;
- audio;
- animation;
- particle/VFX presentation;
- asset references/import;
- client-side interpolation;
- selection/picking;
- editor conveniences.

## Responsibilities not owned by Stride

- business/process state;
- authoritative actor state;
- work queues;
- progression logic;
- economy truth;
- quest state;
- automation decisions;
- save-game semantics;
- deterministic random stream;
- process definitions.

## Presentation entities

Do not create one heavyweight Stride entity per passive simulation object if scale makes that expensive.

Use tiered representation:

1. **Interactive entities** — selected/nearby actors and machines with full presentation components.
2. **Instanced visuals** — repeated props, distant workers, inventory.
3. **Aggregated visuals** — facility-level summaries at large zoom distances.
4. **UI-only representations** — world map/organization views.

## Scene organization

Initial scenes:

```text
BootstrapScene
  - game services
  - UI root
  - camera controller

WorldScene
  - facility presentation roots
  - environment
  - lighting
  - presentation pools

DebugSceneOverlay
  - gizmos
  - state visualization
  - trace markers
```

Prefer runtime presentation roots generated from simulation snapshots over embedding gameplay truth in authored scenes.

## Camera

The initial camera is 3D orthographic/isometric-like with:

- pan;
- zoom across several conceptual scales;
- optional rotation;
- focus selected actor/process;
- smooth transition to closer perspective views;
- lens overlays.

Avoid committing to first-person controls until the core simulation/presentation contract is stable.

### Implemented isometric sandbox slice

The code-native client now renders the dish station as a projected isometric floor. It derives workstation blocks, dish stacks, service supply, flow traces, and actor markers from `DishStationSnapshot`. None of those visuals retain authoritative queue or actor state.

The client owns bounded pan, zoom, recentering, and projected workstation hit-testing. Keyboard, mouse, and native UI-test inputs converge on the same semantic client controls; consequential work still becomes the existing engine-neutral simulation commands.

Placement mode projects a transient floor-cell preview, occupancy feedback, and command-based undo history. Clicking or confirming placement emits `PlaceDishStationFixtureCommand`; clicking a floor tile, clicking a distant fixture, right-clicking, or using the keyboard contextual action emits `MovePlayerCommand`. Fixture picking follows the same depth order and visible silhouettes as rendering, including service, while the contextual cursor and pulsing outline preview the resulting `MOVE`, `WORK`, `INSPECT`, `PLACE`, or `BLOCKED` intent. The player pawn interpolates toward snapshot location, and custom fixtures are depth-sorted without modifying authoritative coordinates.

## First-hours shell

The client opens with a five-page briefing and pauses simulation until the player commits guidance and comfort preferences. `Guided` shows next-action prompts, `Contextual` emphasizes quest outcomes and evidence, and `Minimal` leaves conditions in the journal while retaining causal world feedback. The final page independently toggles reduced motion and high contrast. Reduced motion snaps actor projection and removes reticle pulsing; high contrast uses near-black panels with bright complete edges. Completion emits the replayable `CompleteIntroCommand`, including all three preferences.

The HUD tracks the active outcome quest, career level, and XP. `J` or the persistent HUD button opens an eight-quest journal showing completed, active, and locked outcomes with capability rewards plus active-simulation duration. Pointer selection and visible Details/Back/Close actions mirror `Q` / `E`, arrow, `Enter`, and `Esc` navigation. Detail pages expose the authored situation and observable outcome, but withhold causal discovery text until authoritative completion. The intro wizard, guidance and comfort cards, Continue/New Career flow, handbook, journal, and scorecard likewise maintain pointer/keyboard parity. Pointer adapters invoke the same client handlers as keyboard intent; consequential results still enter the simulation as commands. `F12` opens a progression-aware Shift Handbook: core interaction is stable, while capability controls and the current opportunity follow authoritative unlocks and tutorial state without exposing hidden discovery text. After the regression proof, `W` begins the prepared reliability window and the service panel projects its attempt, status, and successful demand checks. Completing that window unlocks `K`, a first-shift report that collates authoritative outcome, route, delegation, incident, and reliability evidence and presents three facilitator debrief prompts. Quest completion produces one eight-second progression receipt containing the outcome, XP total, actual level-up status, capability, and authored reason the observed problem makes that capability useful. The client projects the authoritative snapshot and does not award XP. Lens and layout access use unlocked capabilities. Consequence-bypassing sandbox tools remain locked in the player client until episode completion; the semantic UI driver or an explicit environment opt-in enables the development bypass.

When a durable career checkpoint exists, launch opens a Continue/New Career screen before simulation advances. Continue restores the authoritative replay and returns directly to the saved quest/level state. New Career uses a confirmation step and does not replace the existing checkpoint until the new intro is committed. Command checkpoints and bounded periodic autosaves use the persistence layer's atomic file replacement.

All greybox UI uses a centered 1024×600 virtual canvas transformed to the current backbuffer. The same inverse transform maps mouse coordinates into virtual/isometric space. Window resizing is enabled; aspect-ratio mismatch is letterboxed rather than stretching text or world geometry.

The Windows bootstrap requests the primary display's native dimensions before Stride creates its graphics device, then applies a borderless monitor-sized window. This keeps the backbuffer native rather than stretching a smaller render target. `--windowed` and `AUTOMATION_WINDOWED=1` retain a resizable test/development path.

HUD composition follows three layers:

1. The isometric world is the continuous full-canvas background.
2. Objective, service health, selection/action, notifications, layout tools, and sandbox tools use compact translucent edge overlays.
3. Analytical lenses and the benchmark use a shared dimmed modal frame with title, question, lens rail, content, and close/navigation affordance.

This first slice deliberately uses procedural primitives instead of committing to an asset vocabulary. Its purpose is to validate spatial composition, selection, camera behavior, snapshot projection, and lens compatibility before the Stride scene/prefab art pass.

## Input

Input is translated into engine-neutral commands.

```text
mouse click
  -> client selection
    -> player intent
      -> simulation command
```

The client may perform hover/selection locally, but consequential world changes are commands.

## UI

Important panels:

- current job/objectives;
- selected actor/resource/machine;
- process inspector;
- expected vs observed story;
- metrics;
- automation editor;
- organization/ownership;
- incident debugger;
- pattern codex;
- progression/skills;
- scenario history.

UI should expose progressively deeper information rather than dump every variable on a new player.

## Assets

Stride Game Studio can import resource files and create scenes, materials, prefabs, and other assets. Source assets live outside generated/compiled output and retain provenance/license metadata.

See [Asset Pipeline](15_ASSET_PIPELINE.md).

## Stride-specific spike tasks

Before full production:

1. Create .NET 10 Stride project.
2. Verify orthographic camera and large-world coordinate behavior.
3. Render 10k+ simple moving presentation objects using appropriate instancing/pooling.
4. Verify runtime asset loading strategy needed for content packs.
5. Prototype selection/picking at multiple zoom levels.
6. Prototype UI overlay architecture.
7. Confirm headless simulation assembly runs without Stride.
8. Document build/distribution path for Windows-first target.

The greybox now covers items 1, 2, 3, 5, 6, 7, and 8 at code-native fidelity. Runtime asset loading remains an explicit production spike.

## Exit strategy

Because simulation/content contracts are engine-independent, replacing Stride should require rewriting presentation adapters rather than core game logic. We do not intend to replace it, but architectural freedom is a product requirement.
