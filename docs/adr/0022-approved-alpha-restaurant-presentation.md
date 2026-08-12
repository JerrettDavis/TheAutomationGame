# ADR-0022: Executable approved-alpha restaurant presentation

- Status: Accepted
- Date: 2026-08-12

## Context

The first restaurant chapter had accumulated a native room, one imported model, procedural actors/items, UI, VFX, and seven audio cues across earlier slices. The original asset manifest still called broad categories placeholders and did not distinguish a safe renderer fallback from the presentation reviewers would actually accept. S034 needs an honest internal vertical-slice bar without declaring procedural work final production art or coupling presentation status to simulation identity.

## Decision

Treat the complete first-chapter presentation as a concrete set of restaurant surfaces. Each critical surface must be `approved-alpha` or `production`; approved alpha requires source, license, limitation, replacement trigger, accessible equivalent where relevant, and operational-state coverage. Enforce this with the client-owned `RestaurantAlphaAssetAudit` and keep the human-readable decision in `30_INITIAL_ASSET_MANIFEST.md`.

Accept the coherent code-native room, equipment, item, character, UI, and VFX families plus the CC0 Kenney washer and project-authored audio set for the internal vertical slice. Keep fallback primitives/projection/silence as typed failure behavior, but never count fallback-only output as accepted critical-path art.

Presentation state continues to derive from authoritative simulation snapshots, notifications, and content IDs. The audit and asset IDs do not enter domain assemblies, commands, saves, replay data, collision, or topology.

## Consequences

- Reviewers can distinguish “accepted for alpha with a named limitation” from an accidental placeholder.
- Headless tests reject incomplete categories, unsafe status, missing evidence, inaccessible audio/VFX, non-distinct item silhouettes, and incorrect washer-state priority.
- The native smoke reports asset/audio readiness and retains an actual running-state frame.
- Future production replacement is incremental through stable presentation seams and explicit triggers.
- This is deliberately restaurant-specific. A generic cross-industry asset-governance abstraction waits for the warehouse to expose a recurring need.
