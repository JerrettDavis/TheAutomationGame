# Initial Asset Manifest

This is the concrete asset list for Phase 0/1. All entries begin as programmer art unless otherwise noted.

## Environment

| ID | Asset | First source | Ship status |
|---|---|---|---|
| `env.floor.tile01` | restaurant floor tile | simple authored material | placeholder |
| `env.wall.modular01` | modular wall | Blender primitive kit | placeholder |
| `env.door.single01` | single door | Blender | placeholder |
| `env.counter.steel01` | steel work counter | Blender | placeholder |
| `env.shelf.dish01` | clean-dish shelf | Blender | placeholder |
| `env.bin.trash01` | trash bin | Blender | placeholder |

## Dish station equipment

| ID | Asset | Gameplay state needs | Notes |
|---|---|---|---|
| `equip.dishwasher01` | commercial washer | idle, ready, washing, fault | state lights must be visible |
| `equip.scrape01` | scrape station | normal/blocked | simple |
| `equip.rack.glass01` | glass rack | empty/partial/full | instancing candidate |
| `equip.rack.plate01` | plate rack | empty/partial/full | instancing candidate |
| `equip.cart01` | dish cart | empty/full | movable |
| `equip.sensor.ready01` | machine status sensor | true/false/stale/fault | educationally important |

## Work items

| ID | Asset | Expected visible count |
|---|---|---:|
| `item.plate01` | plate | 100s |
| `item.glass01` | glass | 100s |
| `item.utensil.aggregate01` | utensil bundle/aggregate | 10s-100s |
| `item.tray.standard01` | standard tray | 10s |
| `item.tray.rare01` | uncommon tray | few |

Individual utensils should initially aggregate rather than require thousands of physics objects.

## Characters

### Prototype

- one low-poly shared humanoid rig;
- 3-5 palette/uniform variants;
- simple head/hair/accessory variants;
- animations: idle, walk, carry, scrape, load, unload, inspect, gesture/talk, confused/error.

### Source location

```text
assets-src/models/characters/base/
assets-src/models/characters/clothing/
assets-src/animations/characters/
```

## UI/icon pack

Required first-pass icons:

- actor;
- worker;
- machine;
- resource;
- queue;
- process;
- state;
- observation;
- knowledge;
- assumption;
- warning;
- automation;
- manual action;
- sensor;
- failure;
- metric;
- trace.

Prefer SVG source where practical, rasterized/converted as required by the client pipeline.

## Audio

Minimum:

- room ambience;
- dish clatter variants;
- washer start/run/stop;
- fault beep;
- UI selection/confirmation;
- incident cue kept subtle.

## Provenance manifest

Create `assets-src/manifest.yaml` before importing any third-party or generated asset.

Required fields:

```yaml
id:
source_path:
source_type:
creator:
source_url:
license:
license_evidence:
created_or_acquired_at:
shipping_status:
notes:
```

## First art spike

Build the entire dish station from primitives and flat materials before acquiring production assets. The objective is to validate scale, camera, selection, state readability, and visible-object performance.
