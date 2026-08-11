# ADR-0003: Build a 3D World with Orthographic-First Presentation

- Status: Accepted for prototype
- Date: 2026-08-10

## Decision

Use real 3D scenes and assets while presenting the core game primarily with an orthographic/isometric-like camera.

## Rationale

This combines management-game readability with future freedom to rotate, zoom, inspect close-up, or introduce immersive views. Conceptual zoom from worker to facility to organization can mirror systems decomposition.

## Consequences

- assets require 3D pipeline;
- rendering performance must handle many simple objects;
- navigation/spatial systems are 3D-aware;
- no commitment to first-person gameplay.
