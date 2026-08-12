# ADR-0019: Evidence-Backed Pattern Knowledge

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S030 gives the player two lived uses of one routing decision slot: copying the main-room policy produces a patio shortage, while fitting a different policy supplies both stations. S031 must remember that reusable shape and expose the player's own evidence without prematurely teaching the conventional Strategy name reserved for S032. Recognition also has to survive career resume and remain explainable from authoritative outcomes.

## Decision

Add engine-neutral `PatternId`, `PatternEvidenceId`, problem signature, evidence, milestone, knowledge, and profile values to Domain. Knowledge is immutable; each concluded milestone cites the evidence IDs that justify it. Direct evidence may establish lived milestones such as `Encountered` or `Applied`, while derived recognition remains an explicit conclusion.

Schema-v1 authors a hidden pattern definition with a pre-name title, qualifying problem signature, evidence threshold, required application, and primary quest. The simulation remains unaware of this catalog. A concrete `RestaurantPatternEvidenceRecognizer` in Persistence reads the authoritative S030 replay: a copied-policy shortage records the encountered mismatch, and a later demand-fitted zero-shortage result records application. Both records conclude recognition idempotently. The recognizer never concludes `Named`.

Replace the client career file with a versioned envelope containing the first-shift replay, two-station replay, and pattern profile. Continue reconstructs both worlds and the profile; legacy raw first-shift files upgrade with empty S030/S031 state. The Pattern Codex opens only after recognition, pauses gameplay, and projects the authored pre-name title and saved evidence. It does not calculate or grant knowledge.

## Consequences

### Positive

- recognition follows simulated consequences and remains causally explainable;
- the player's own problem, move, and result are the teaching surface;
- naming remains a separate evidence-backed lifecycle event for S032;
- replay, persistence, headless output, client presentation, and resume share stable IDs;
- Domain, Simulation, and Content remain independent of Stride.

### Negative

- the first recognizer is intentionally restaurant-specific rather than a generic rule engine;
- the career envelope now coordinates three engine-neutral histories and requires explicit migration;
- the pre-name Codex has only one concept until later episodes add evidence.

## Validation

- reject malformed IDs, duplicate evidence, duplicate milestones, and invalid milestone citations;
- compile and validate the authored pattern reference and deterministic manifest;
- prove the copied and fitted authoritative trials yield exactly two idempotent evidence records and recognition, never naming;
- round-trip both replay journals and knowledge, and upgrade the legacy save shape;
- run the pattern headless proof twice and compare bytes;
- open and visually inspect the Codex before and after native career resume.

## Revisit when

- S032 defines the player action and authored content that conclude `Named`;
- another industry provides transfer evidence;
- repeated recognizers establish a concrete need for a reusable recognition-rule representation;
- the career envelope needs content-version migration beyond the supported legacy shape.
