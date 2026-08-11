# Asset Pipeline

## Objectives

The asset strategy must support:

- rapid prototyping;
- coherent stylized visual language;
- high object counts;
- legal/provenance clarity;
- replacement of generated/provisional assets;
- community/mod content later.

## Source versus runtime assets

```text
assets-src/
  concept/
  models/
  textures/
  materials/
  animations/
  audio/
  ui/

Automation.Client.Stride/Assets/
  imported/compiled runtime assets
```

Original source files remain authoritative. Stride-specific imported/compiled assets are derivatives.

### Imported prototype assets

The isometric sandbox uses a five-image subset of [Kenney Cursor Pack 1.1](https://kenney.nl/assets/cursor-pack) for contextual pointer feedback. The CC0 source subset, original license, and provenance record live under `assets-src/kenney/cursor-pack-1.1/`; runtime copies live under `src/Automation.Client.Stride/Assets/imported/kenney-cursor-pack/` and are embedded by the client project.

Kenney's CC0 Food Kit is a promising source for dishes and kitchen props, but it is a 3D pack. It should enter only with a deliberate Stride model/material import pass rather than being flattened into the current procedural 2D projection.

## Asset categories

### Environment kits

- floors;
- walls;
- doors;
- shelves;
- counters;
- pipes/utilities;
- roads/loading docks.

### Equipment

- dishwashers;
- conveyors;
- registers;
- scanners;
- racks;
- pallets;
- carts;
- robots;
- servers/network devices.

### Characters

Use modular characters with shared rigs and interchangeable:

- body variants;
- uniforms;
- hair/accessories;
- role indicators.

### Work items/resources

Use simple shapes/material variants and instancing for high-volume objects.

### UI/iconography

Create a coherent icon language for:

- actors;
- states;
- decisions;
- effects;
- failures;
- knowledge;
- automation;
- contracts;
- metrics.

## Creation strategy

### Phase 1 — programmer art

- primitives;
- flat materials;
- simple icons;
- public-domain/CC0 placeholders with logged provenance.

Purpose: validate mechanics and scale.

### Phase 2 — coherent prototype kit

- custom low-poly modular environment;
- reusable machinery kits;
- basic character rig;
- consistent UI pack.

### Phase 3 — production art

Replace or refine assets based on established visual direction.

## Generative tools

Generated concept art, textures, reference imagery, voices, or models may be used only with documented provenance and licensing/terms suitable for commercial use at the time of creation.

Generated assets should normally be treated as:

- concept/reference;
- prototyping material;
- source requiring cleanup;

unless explicitly approved for shipping.

Store metadata beside source assets:

```yaml
id: equipment.dishwasher.prototype01
source_type: generated
created_by: <tool/model>
created_at: YYYY-MM-DD
prompt_or_brief: ...
license_review: pending
shipping_status: placeholder
```

## Third-party asset sourcing

Every external asset must record:

- source URL/vendor;
- creator;
- license;
- purchase/order reference if commercial;
- modification rights;
- redistribution restrictions;
- attribution requirements;
- whether source files may be committed.

Do not rely on marketplace availability as provenance.

## 3D standards

Initial conventions:

- meters as world units;
- Y-up or Stride-native convention documented at bootstrap;
- consistent forward axis;
- pivots at meaningful placement points;
- low material count;
- shared atlases/materials where appropriate;
- LODs for repeated complex objects;
- collision meshes separate from render meshes where needed.

## Performance budgets

Asset authors receive approximate budgets by category rather than one global triangle rule.

Early prototype priorities:

- batching/instancing compatibility;
- modest bone counts;
- limited transparent materials;
- predictable shader complexity;
- texture resolution proportional to camera distance.

## Asset request workflow

Use [Asset Request Template](templates/ASSET_REQUEST_TEMPLATE.md).

Requests must state gameplay purpose and viewing distance before aesthetic details.
