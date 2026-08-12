# ADR-0020: Evidence-Gated Pattern Naming

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S030 produces the lived contrast and S031 records two causal evidence entries plus recognition, while deliberately withholding conventional vocabulary. S032 must reveal Strategy only after that work, explain the structure and tradeoffs, persist the result, and avoid turning the reveal into a memorization test. Naming is consequential career progression but is not restaurant simulation behavior.

## Decision

Author the conventional name, named display title, reflection prompt and acknowledgement, intent, concrete structure, benefits, and costs in the schema-v1 pattern definition. The pre-name title remains independently validated not to reveal the catalog token.

Use `PatternNamingService.RecordReflection` as the explicit application boundary. It accepts an immutable profile and pattern definition, refuses a profile without `Recognized`, chooses the recognized application evidence as its basis, and concludes `Named` idempotently. Domain lifecycle validation requires recognition before naming and naming before mastery.

Persist knowledge conclusions as milestone/evidence-ID pairs. Career schema 2 validates unique evidence and conclusions and exact milestone backing. Loading schema 1 reconstructs its existing recognition conclusion from applied evidence; raw first-shift legacy migration remains unchanged.

The Pattern Codex presents lived evidence first. `Enter` acknowledges the recurring shape, invokes the service, saves immediately, and switches to the named Strategy page. The page keeps the two restaurant consequences visible beside the authored intent, context/strategy/selection roles, benefits, and costs. No answer text or multiple-choice result is evaluated.

## Consequences

### Positive

- a new profile cannot reach the reveal without qualifying lived evidence;
- the conventional name remains attached to the player's own causal history;
- conclusion provenance survives save/resume and future content interpretation;
- structure and tradeoffs are content-authored and testable without Stride;
- Simulation remains unaware of design-pattern vocabulary.

### Negative

- naming is currently one explicit acknowledgement rather than a longer character scene;
- the named page is a bounded Strategy layout rather than a generalized multi-tab Codex;
- career schema 2 requires a migration path for the short-lived schema-1 profile shape.

## Validation

- prove lifecycle order, citation integrity, malformed save rejection, schema-1 migration, and schema-2 named round-trip;
- reject incomplete or revealing pattern content and pin deterministic manifests;
- run the naming demo twice and compare normalized bytes;
- complete recognition, reflect, save, relaunch, and assert the named state through native UI automation;
- visually inspect both reveal and resumed named-page screenshots.

## Revisit when

- a later character scene supplies a more expressive reflection choice without becoming a quiz;
- another named pattern proves a recurring multi-page Codex layout;
- transfer or stress-test evidence changes how the page organizes history;
- a content-version migration must translate renamed catalog entries.
