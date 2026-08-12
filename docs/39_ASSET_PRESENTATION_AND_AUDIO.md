# Asset, Presentation, Animation, VFX, and Audio Plan

> Replaceable production presentation over authoritative simulation.

## Principle

Assets are projections of stable game identities and state. They do not become domain truth.

```text
Simulation/content ID + state
        ↓
PresentationCatalog
        ↓
Model / material / animation / icon / audio / VFX
```

A missing asset must degrade to a safe fallback, never break the scenario.

## Presentation tiers

### Tier 0 — Fallback

Guaranteed renderer-safe primitives:

- simple mesh/shape;
- generic icon;
- text label when necessary;
- generic sound or silence.

Every presentation category needs a fallback.

### Tier 1 — Prototype

Deliberately licensed/free packs or simple generated meshes sufficient for gameplay validation.

### Tier 2 — Production

Final or near-final authored assets with consistent art direction, materials, rigging, icons, sound, and performance budgets.

### Tier 3 — LOD/aggregate

Simplified representation for far zoom/large facilities.

## First 3D proof set

Do not import hundreds of assets first. Prove one complete room with:

### Environment

- floor module;
- wall module;
- inside/outside corner;
- doorway/opening;
- counter/service opening;
- utility/trim piece;
- light fixture.

### Equipment

- dish washer;
- scrape/prep table;
- clean rack;
- dirty rack;
- service pass/buffer.

### Items

- glass;
- plate or second dishware family;
- rack/container.

### Character

- one shared humanoid rig;
- player appearance variant;
- one worker variant.

### Required states

The washer alone should prove:

- idle;
- available/ready;
- active;
- complete;
- blocked/fault/attention;
- selected;
- interactable.

Not every state needs a unique model. Use animation/material/icon/VFX combinations.

## S008 import decision

The first real-asset spike uses Stride's package pipeline (`.sdpkg` root asset → `.sdm3d` model asset → source `.glb`) as the canonical authored-model import path. The licensed source remains in `Resources`; the compiler owns conversion and bundling.

The current dish room is still a SpriteBatch isometric projection rather than a native Stride scene. Until that boundary moves deliberately, the live room may render a publisher-provided isometric projection from the same authored model, with the same authoritative floor anchor and presentation-only state overlays. Missing projection data falls back to the existing primitive workstation. This is a temporary hybrid seam, not a second gameplay identity or movement/collision model.

Native `Model` rendering requires a scene/camera/graphics-compositor integration and belongs with the presentation catalog and modular room work. Do not add a one-off 3D renderer solely for an individual prop.

## Presentation catalog

Example conceptual entry:

```yaml
id: presentation.workstation.dish-washer.standard
model: asset://Models/Restaurant/DishWasher
fallback: presentation.fallback.workstation
animations:
  active: wash_cycle
materials:
  fault: material.workstation.fault-overlay
icons:
  fault: icon.status.warning
audio:
  start: sfx.washer.start
  loop: sfx.washer.loop
  complete: sfx.washer.complete
```

The exact Stride asset URI remains presentation-only.

### Implemented S009 boundary

The client owns stable IDs for the first migrated projections:

- `presentation.workstation.dish-washer.standard`;
- `presentation.actor.new-hire.standard`;
- `presentation.item.dish.plate`, `.glass`, and `.tray`;
- typed `presentation.fallback.workstation`, `.actor`, and `.item` roots.

The catalog resolves only presentation data: asset locations, primitive colors, and projection dimensions. Simulation entities, commands, replay data, and career saves do not store these IDs. A catalog entry may therefore be replaced without changing authoritative identity or save compatibility.

Missing IDs resolve through an explicitly selected category fallback. If an authored asset location resolves but loading fails, the renderer follows that entry's fallback before drawing. Catalog construction validates that all three root fallbacks exist, have the correct kind, and do not themselves recurse.

This is intentionally not yet a general external content registry. Add new definition fields only when another concrete presentation consumer requires them.

## Asset provenance

Maintain a machine-readable or clearly structured manifest:

```text
AssetId
Source URL / creator
Original package/name
License
Attribution required?
Commercial use allowed?
Modification allowed?
Imported path
Modified?
Shipping status: prototype | production | rejected
Notes
```

Never assume a downloaded asset is safe to ship because it was free.

## Art direction

Target a readable stylized 3D/isometric language:

- simplified forms;
- strong silhouettes;
- restrained texture detail;
- clear material categories;
- readable at mid/far zoom;
- characters/equipment distinct by shape before color;
- operational status overlays consistent across industries.

The world should support many industries without requiring a new rendering style for each.

## Modular environment strategy

Each industry gets a kit rather than bespoke monolithic maps.

Common kit concepts:

- wall/floor/ceiling/trim;
- door/opening/window;
- work surfaces;
- shelving/storage;
- utility connectors;
- signage;
- safety markings;
- clutter/props;
- light fixtures.

Facility definitions place modules through presentation metadata; gameplay topology remains separate.

### Implemented S010 dish-room kit

The first modular room uses a client-only authored plan and a native Stride forward-rendering layer. Its reusable module kinds cover floor, back/side walls, a doorway frame with an actual wall gap, work counters, washer zone/model, dirty and clean racks, and the service pass. Repeated styles share generated model/material resources rather than building unique geometry per tile.

Fixture module positions are derived from `DishStationPlacements` whenever the authoritative layout changes. The fixed architectural shell is not gameplay topology: it currently contributes no collision or walkability rules. S011 must introduce those rules explicitly rather than inferring them from render entities.

The native orthographic camera uses the same floor-cell projection as gameplay picking and overlays. The existing SpriteBatch world remains a renderer-safe fallback if native scene setup fails; the HUD, player/worker projections, labels, process evidence, selection, and interaction overlays remain composited over either room representation.

The Kenney washer is loaded through its catalog model URL. Missing model data falls back to a native primitive module, while failure of the larger native scene boundary falls back to the prior complete room projection. The window title exposes `room=native` or a typed fallback status for human acceptance and diagnostics.

## Procedural visual variation

Variation should be deterministic for saved worlds when it matters visually.

Parameters can include:

- material variant;
- prop subset;
- decal/signage;
- small geometry variant;
- worker appearance set;
- clutter seed.

Do not procedurally vary anything that changes gameplay collision/ports unless that variation is part of the authored facility/template result and therefore deterministic simulation content.

## Characters

### Implemented S012 character slice

The first player and new-hire variants share a renderer-safe procedural humanoid rig. Stable catalog IDs select their colors and silhouette dimensions; neither simulation identity nor save data stores presentation IDs.

The client-owned character presenter derives its state from existing authoritative projections:

- player targets come from `DishStationLayoutSnapshot.PlayerCell`;
- worker targets come from `NewHireSnapshot.ActionsCompleted` and `LastAction`, resolved to the current topology's interaction port;
- position changes produce walk and stable screen-relative facing states;
- successful authoritative player work commands and observed worker action-count changes produce bounded work poses;
- no change produces idle;
- the player variant renders a persistent selection ring, while the worker remains visually distinct without implying selection;
- reduced-motion mode snaps presentation travel and suppresses cyclic bob/stride while preserving a static work reach.

The rig is a temporary production candidate built from the existing batched primitive renderer. It proves the shared animation vocabulary and fallback behavior without introducing a skeletal asset pipeline or a generalized animation graph. Presentation interpolation and pose timing do not issue commands, mutate snapshots, or enter replay/save identity. Imported skeletal characters, carry/inspect/talk states, workstation hand anchors, and LOD animation remain future requirements once concrete assets demand them.

### Shared rig first

Use one skeleton/animation vocabulary across many worker variants where possible.

Required early animation states:

- idle;
- walk;
- carry;
- pick/place;
- operate/work;
- inspect;
- wait;
- talk/gesture;
- react/attention.

Animation clips can be generic and contextualized by workstation pose points.

### Workstation pose anchors

Presentation definitions may expose anchors:

```text
StandPosition
Facing
HandTarget
ItemTarget
OptionalAnimationOverride
```

These are visual alignment data. Simulation interaction ports remain authoritative.

## VFX

VFX should communicate, not decorate every event.

Useful families:

- selection/highlight;
- work progress subtle cue;
- completion;
- blocked/error;
- routing/transfer trace when a lens is active;
- automation action pulse;
- sensor/knowledge disagreement hint after discovery;
- money/metric deltas sparingly.

Avoid constant particles that make state less readable.

## Audio layers

### Ambience

Industry/room ambience:

- restaurant back-of-house;
- warehouse docks;
- retail floor/registers;
- factory machinery;
- logistics yard/control room;
- office/platform environments.

### Equipment

State-bearing loops/transients:

- start;
- active loop;
- complete;
- fault;
- reset.

### Work

- footsteps;
- pick/place;
- scan;
- cart/rack movement;
- tool use.

### UI

- confirm;
- reject;
- notification classes;
- editor apply;
- checkpoint/save;
- Codex reveal.

### Narrative

Full voice acting is optional and should not block early production. Text + authored barks + limited vocal exertion/nonverbal cues can provide character without huge localization/recording cost.

## Accessible audio

If audio conveys unique information, supply a visual/text equivalent.

Examples:

- machine complete sound pairs with world state change;
- offscreen critical alarm gets a directional/semantic notification;
- dialogue has text.

### Implemented S013 audio slice

Seven project-authored, deterministically synthesized mono cues compile through Stride's normal sound-asset pipeline: looping dish-room ambience, work, washer start, washer complete, blocked action, operational failure, and quest success. Their provenance and regeneration command live beside the WAV sources in `Resources/Audio/PROVENANCE.md`.

The client-owned router observes existing authoritative evidence rather than adding audio concepts to simulation:

- accepted player work commands and increases in authoritative worker action count route the work cue;
- `Washer started` and `Cycle complete` notifications route distinct equipment cues;
- rejected commands route blocked feedback;
- automation/reliability/rework/hypothesis failure notifications route the failure cue;
- increases in completed quest count route quest success once;
- notification counts, action counts, and completed-quest counts prevent repeated frame sampling from replaying a cue.

The saved master-volume percentage now updates live Stride `SoundInstance` gain; zero is a true mute. Ambience uses a quiet loop and event cues use bounded one-shot instances. Audio content or device initialization failure leaves a typed silent fallback rather than preventing play.

Every emitted cue carries a visible `SOUND • ...` caption in the gameplay HUD. Detailed command feedback, operational notifications, progression receipts, washer visuals, and failure colors remain present, so audio conveys no unique required information. This first slice is intentionally non-spatialized: emitters, listeners, occlusion, music buses, dialogue, and dynamic mixing wait for concrete later requirements.

## Implemented S034 restaurant approved-alpha pass

The first restaurant chapter now has a concrete shipping decision in `30_INITIAL_ASSET_MANIFEST.md` and an executable `RestaurantAlphaAssetAudit`. Its nine accepted surfaces cover room, equipment, items, world cast, narrative cast, UI, audio, and VFX. The audit fails missing categories, duplicate identity, critical `placeholder`/`fallback-only` status, absent provenance/license, undocumented alpha limitations or replacement triggers, missing accessible equivalents, and incomplete operational-state coverage.

The integrated pass adds:

- five colored work zones, equipment-specific procedural details, utility trim, and three light fixtures to the native modular room;
- basin/splash-guard, open-shelf rack, drainboard, washer-door, and service-pass silhouettes while retaining authoritative floor anchors;
- distinct plate-oval, glass-tumbler, and tray-rectangle aggregate marks, tested independently of color;
- a deterministic washer language for `IDLE`, `READY`, `RUN`, `DONE`, and `ATTN`, with attention taking priority and cyclic motion suppressed in reduced-motion mode;
- distinct badges for Avery, Ray, Jules, Tessa, Devon, and Sam wherever live authored dialogue or the vendor presentation identifies them;
- a washer running loop and UI confirmation cue, bringing the deterministic mono set to nine compiled assets; start/complete evidence starts/stops the loop, and the visible washer state remains authoritative;
- runtime diagnostics `[assets=alpha] [audio=ready]`, a non-playing `--diagnose-assets` path, and a dedicated native smoke frame during a real washer cycle.

All additions remain client presentation. Simulation snapshots and notifications select visual/audio states; presentation never creates domain facts, commands, topology, save fields, or replay identity. The primitive/catalog fallbacks remain deliberately available and tested, but do not count as the reviewed critical-path surface.

## Camera and LOD budgets

Define budgets after the first real room proof, then test them with production-like assets.

Measure separately at:

- near room view;
- mid facility view;
- far aggregate view;
- dense actor scenario.

Optimization order:

1. remove accidental overdraw/state churn;
2. batch/instance repeated assets;
3. LOD/cull appropriately;
4. simplify far animation/labels;
5. preserve simulation correctness independently.

## Asset-complete definition for a chapter

A campaign chapter is presentation-complete when:

- no unapproved placeholder is visible on its critical path;
- all interactive equipment has readable state presentation;
- characters have required shared animations;
- UI iconography exists;
- ambience and core action sounds exist;
- critical failures have visual and audio feedback;
- asset provenance is complete;
- performance meets the chapter budget;
- all gameplay still works with presentation fallback mode enabled.
