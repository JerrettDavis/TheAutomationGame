# ADR-0014: Stable Character and Quest Participant Content

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

The first-shift quests previously depended on an anonymous manager placeholder. S026 needs a durable restaurant cast whose identity, knowledge, authority, relationships, and quest involvement survive presentation changes and can support later dialogue without embedding story decisions in client code.

## Decision

Schema v1 character definitions require a stable character ID, industry, display name, role ID, player-facing motivation, stable known-fact, blind-spot, and authority IDs, directional typed relationships, and primary/fallback presentation references. Relationship targets resolve to a different character in the same industry and are unique per source.

A scenario explicitly owns its character roster. Every quest explicitly lists one or more participant character IDs, and compilation requires those characters to belong to the quest scenario's roster. The first-shift runtime adapter carries participant IDs unchanged. Presentation resolves names and roles through the compiled catalog; it does not replace the IDs with client-owned identity.

The checked-in restaurant cast is Avery Chen, Ray Morales, Jules Martin, Tessa Brooks, and Devon Price. All eight first-shift quests declare the intended subset. This decision defines facts, authority, relationships, and involvement only; contextual dialogue and barks remain S027.

Because schema v1 is still a pre-alpha authoring boundary, S026 expands its required shapes in place and migrates every checked-in bundle and template. Older v1 bundles fail with targeted diagnostics rather than receiving implicit defaults.

## Consequences

### Positive

- quest involvement is deterministic, validated, and independent of display text;
- missing, off-roster, self, duplicate, and cross-industry references fail during compilation;
- presentation can fall back without losing character identity;
- later dialogue can target existing characters without inventing a second cast registry.

### Negative

- existing schema-v1 bundles must add the new required character metadata and quest participants;
- facts, authority, and relationship kinds are opaque IDs until later systems consume them;
- directional relationships require authors to add both directions when both meanings matter.

## Validation

- compile exactly five complete restaurant character definitions;
- resolve every relationship and all eight quest participant lists;
- reject invalid participant and relationship mutations at semantic paths;
- preserve participant IDs through the first-shift runtime adapter;
- resolve journal participant names and roles from the catalog;
- print the complete roster and quest mappings deterministically in headless mode.

## Revisit when

- S027 defines dialogue speakers/listeners and contextual triggers;
- authority or knowledge becomes authoritative NPC decision state;
- a second industry reveals genuinely shared relationship or role taxonomies;
- external content compatibility requires a schema-v2 migration path.
