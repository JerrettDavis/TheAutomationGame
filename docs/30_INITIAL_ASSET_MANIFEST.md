# Restaurant Approved-Alpha Asset Register

This is the shipping decision for the complete first restaurant chapter. It supersedes the original Phase 0 programmer-art wish list. The executable counterpart is `RestaurantAlphaAssetAudit`: a critical-path row may not ship as `placeholder` or `fallback-only`, and every approved-alpha row records its limitation and replacement trigger.

`approved-alpha` means coherent, readable, licensed, and accepted for the internal vertical slice. It does not mean final production art. Renderer-safe fallbacks remain implemented but are failure behavior, not the reviewed critical-path presentation.

## Accepted surfaces

| ID | Category | Source and license | Status | Accepted limitation | Replacement trigger |
|---|---|---|---|---|---|
| `restaurant.room.modular-kit` | room | project-authored `DishRoomModulePlan` | approved-alpha | procedural flat-material floor, walls, opening, counters, racks, service pass, utility trim, and lighting; no texture pass or distant LOD | a reviewed restaurant kit meets the same anchors, silhouette, camera, fallback, and readability checks |
| `restaurant.equipment.station-family` | equipment | project-authored `IsometricStationScene` | approved-alpha | scrape, rack, unload, stock, and service use stylized procedural geometry and state markings | authored props preserve distinct silhouettes, interaction anchors, state markings, and minimum-zoom readability |
| `restaurant.equipment.kenney-washer` | equipment | Kenney Furniture Kit, CC0 1.0; imported GLB plus isometric projection | approved-alpha | the licensed model uses client state overlays rather than authored animation | reviewed idle, running, complete, and fault animation lands without removing the typed primitive fallback |
| `restaurant.items.dish-family` | items | project-authored procedural marks | approved-alpha | plate, glass, and tray are compact aggregate shapes, not textured 3D inventory | authored instanced items remain distinct by silhouette at default and minimum zoom within count budgets |
| `restaurant.cast.world-rig` | cast | project-authored `SharedCharacterRig` | approved-alpha | player and new hire cover idle, walk, and work; no carry, inspect, talk, or attention pose | a shared authored rig covers the missing first-chapter poses within actor and reduced-motion budgets |
| `restaurant.cast.dialogue-identities` | cast | project-authored cast badge catalog | approved-alpha | recurring speakers use consistent color-and-monogram badges, not illustrated portraits | every first-shift speaker has one coherent reviewed portrait set |
| `restaurant.ui.semantic-language` | UI | project-authored `DishStationGame` and `PixelFont` | approved-alpha | pixel glyphs, text labels, panels, and state colors form the alpha UI kit | an accessibility-reviewed UI/icon pass preserves text labels, keyboard use, scaling, and contrast semantics |
| `restaurant.audio.core-cues` | audio | project-authored synthesized WAVs, CC0-1.0 dedication; see audio provenance | approved-alpha | seven mono cues; no spatialization, dialogue, music bus, or dynamic mix | reviewed recordings cover the same routed events and retain visible captions/equivalent state changes |
| `restaurant.vfx.operational-states` | VFX | project-authored geometry overlays and pulses | approved-alpha | bounded overlays replace particles | a reviewed family remains legible at minimum zoom and has a reduced-motion form |

## Required operational coverage

The equipment and VFX rows jointly cover:

- idle and ready through station body/zone treatment;
- active through the washer running indicator and bounded motion;
- complete through the completion pulse, machine state, queue counts, and sound caption;
- blocked/fault/attention through the warning marker, red attention treatment, notification, and failure/blocked caption;
- selected through the gold outline/ring;
- interactable through the hover pulse and pointer state.

These meanings are presentation projections of authoritative snapshots and notifications. They never infer or mutate gameplay state.

## Environment and equipment inventory

The accepted modular kit contains floor modules, back and side walls, a doorway gap and frame, work counters, dirty and clean racks, washer zone/model, and service pass. The five interactive steps remain individually labeled for accessibility, but their shapes and work-zone colors must be distinguishable without the label. The washer proves all required operational states and all other stations share the same selection, interaction, queue, and attention language.

The original trash-bin, dish-cart, and standalone ready-sensor requests are not first-chapter gameplay entities. They are removed from this critical-path register instead of being carried as implied placeholders. Add them only with a concrete simulation or episode consumer.

## Items

Plate, glass, and tray are aggregate queue projections: hundreds of simulated pieces do not become hundreds of physics objects. Alpha marks differ by silhouette as well as color. The rare tray uses the tray family because its rarity and routing consequence are communicated through counts, incident evidence, and traces rather than an undocumented hidden prop variation.

## Cast

The world needs two actor bodies: the selected player and delegated new hire. Recurring narrative cast—Avery Chen, Ray Ortiz, Jules Martin, Tessa Brooks, Devon Price, and Sam Rivera—receive a stable badge treatment wherever their authored dialogue or proposal is shown. Identity continues to come from content IDs; badge color and monogram remain client-only presentation.

## UI and cursors

The first-pass icon language is code-native and always paired with text. It covers actor, worker, machine, resource/queue, process, state/observation/knowledge, warning/failure, automation/manual action, sensor, metric, and trace concepts. The pointer set is Kenney Cursor Pack 1.1, CC0 1.0, with provenance and license retained under `assets-src/kenney/cursor-pack-1.1/` and imported copies under the client assets.

## Audio

The compiled cue set contains dish-room ambience, work, washer start, washer complete, blocked action, operational failure, and quest success. Source WAVs and deterministic regeneration evidence live in `src/Automation.Client.Stride/Resources/Audio/PROVENANCE.md`. Every information-bearing cue has a `SOUND • ...` caption and corresponding visible state or feedback. Silence remains a valid typed fallback when the device or asset pipeline is unavailable.

## External provenance

| Asset | Creator/source | License | Evidence | Shipping decision |
|---|---|---|---|---|
| Furniture Kit washer GLB/projection | Kenney | CC0 1.0 | `src/Automation.Client.Stride/Assets/imported/kenney-furniture-kit/PROVENANCE.md` and `LICENSE.txt` | approved-alpha |
| Cursor Pack 1.1 | Kenney | CC0 1.0 | `assets-src/kenney/cursor-pack-1.1/PROVENANCE.md` and `License.txt` | approved-alpha |
| seven synthesized WAV cues | project-authored | CC0-1.0 dedication | `src/Automation.Client.Stride/Resources/Audio/PROVENANCE.md` | approved-alpha |

No generated or downloaded asset without a listed source and license is accepted by this register.
