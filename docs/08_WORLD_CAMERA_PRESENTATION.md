# World, Camera, and Presentation

## Initial visual direction

Build a true 3D world presented primarily through an orthographic or low-perspective isometric camera.

The current greybox is a 2D isometric projection used to validate that direction before introducing authored 3D assets. It includes a tiled sandbox floor, pseudo-depth workstation blocks, projected actors and inventory, layout-dependent placement, mouse picking, and bounded pan/zoom controls. Because it consumes the same snapshot contract as other clients, replacing these primitives with a Stride 3D scene does not change simulation ownership.

The floor is a 13×8 bounded placement surface. Arbitrary fixture placement is depth-sorted for correct overlap; invalid occupied cells render red and valid previews green. A centered virtual canvas scales the composition uniformly from ordinary windows through the current 4K test viewport, keeping world labels and analytical panels readable without resolution-specific coordinates.

Gameplay is borderless fullscreen by default. HUD panels hug the canvas edges so they do not turn the world back into a dashboard. Informational lenses deliberately dim, rather than replace, the world and use a centered modal surface; sandbox tools remain a docked overlay, including a compact bottom dock when an analytical modal is open.

Reasons:

- clear process visualization;
- good density of information;
- simple stylized assets can look intentional;
- supports rotation and close inspection;
- leaves room for later immersive views;
- conceptual zoom can mirror systems decomposition.

## Conceptual zoom levels

The camera is not merely spatial. Different distances expose different system concepts.

### Workstation

See individual interactions, hands/tools, item states, machine controls.

### Department

See queues, worker assignments, bottlenecks, work cells.

### Facility

See departments, flows, utilities, receiving/shipping, staffing.

### Organization

See facilities, shared services, contracts, ownership, supply chain.

### System/architecture view

Physical geography can partially fade into logical relationships.

## Visual language

Use readable stylization over realism.

Priorities:

1. state readability;
2. object identity;
3. flow visibility;
4. animation clarity;
5. performance;
6. aesthetic detail.

## Process overlays

World overlays may render:

- arrows showing flow;
- queue lengths;
- process stage colors;
- role ownership;
- resource contention;
- blocked transitions;
- assumptions/unknowns;
- automation boundaries.

In the current dish-station slice, the Reality lens keeps the physical projection uncluttered while the Process lens adds directional traces, queue counts, oldest-item age, accumulated item-ticks, and bottleneck emphasis in world space. Deeper lenses may temporarily cover the floor with a focused analytical panel, then return to the same camera state.

## Runtime overlays

Advanced views expose:

- event rate;
- latency;
- retry counts;
- failure hotspots;
- utilization;
- dependency chains;
- trace propagation.

## Character presentation

Workers do not need deep life-sim fidelity initially. Required behaviors:

- navigate;
- perform recognizable work animations;
- wait/queue;
- communicate visually;
- display confusion/error/decision states;
- operate tools/machines.

Animation must clearly communicate process state even with simple assets.

## Art direction constraint

The game should be able to display hundreds or thousands of visible units without requiring hero-quality models. Favor modular stylized kits, shared materials, instancing, LODs, and palette variants.
