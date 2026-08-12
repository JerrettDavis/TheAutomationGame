# ADR-0011: Restricted, Traceable Automation IR

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S023 needs deterministic automation semantics that the simulation can validate, replay, explain, and eventually expose through a player-facing editor. The existing washer policies encode the first concrete decision: start a present rack from either reported readiness alone or reported readiness corroborated by physical readiness. That decision must remain authoritative and must not grant rules arbitrary access to simulation internals.

## Decision

The domain owns a closed automation intermediate representation with typed Boolean and integer values, constants and named observable references, typed comparisons, `all` / `any` / `not` condition composition, stable rule IDs, enabled state, ordered effects, and structural validation with path-specific diagnostics.

S023 exposes only four dish-station observables (`rack count`, `rack present`, `reported ready`, and `physical ready`) and one effect: issue a supported dish action. The evaluator is pure and deterministic. It records every observed input and predicate result, preserves selected-effect order, and accepts authoritative command outcomes after the simulation executes those effects.

The simulation compiles both existing washer policies to stable IR rules. Live evaluation and captured-incident replay call the same evaluator. A selected effect still enters the existing `Perform` application boundary; the evaluator never mutates world state. The world retains a bounded trace history for inspection.

## Alternatives considered

- Embed delegates, arbitrary C#, or a scripting language. Rejected because unrestricted behavior is not safely validatable, teachable, or replayable.
- Keep separate policy-specific Boolean branches. Rejected because live decisions, replay, and future authoring could silently diverge.
- Let the evaluator execute simulation changes. Rejected because it would bypass authoritative commands and couple the domain to runtime state.
- Add a generic command or reflection-based observable surface. Rejected because S023 has only one proven effect and four proven inputs.

## Consequences

### Positive

- the same decision semantics drive live automation and incident replay;
- invalid rules fail before evaluation with targeted paths and messages;
- traces explain inputs, predicate nodes, selected effects, and command outcomes;
- domain and simulation remain engine-independent;
- later editors or code can target a restricted capability surface.

### Negative

- v1 has no arithmetic, strings, user-authored persistence, multi-rule priority, or generic effect system;
- adding a new observable or effect requires an explicit ontology and authority decision;
- legacy washer policies remain as a compatibility-facing selection until S024 provides authoring.

## Validation

- evaluate constants, observables, comparison, `all`, `any`, and `not` deterministically;
- reject malformed IDs, empty composites/effects, excessive nesting, and incompatible comparisons;
- prove disabled rules emit no effects or observations;
- prove live selected effects execute through authoritative dish actions and record outcomes;
- prove captured-incident replay uses the same predicate trace as live evaluation;
- compare repeated fixed-input headless traces byte-for-byte.

## Revisit when

- S024 requires serialized player-authored rule definitions and editor-facing diagnostics;
- a second concrete automation domain proves additional observables or effects;
- multiple simultaneous rules require explicit priority/conflict semantics;
- arithmetic, temporal predicates, or process-version references become real player needs.
