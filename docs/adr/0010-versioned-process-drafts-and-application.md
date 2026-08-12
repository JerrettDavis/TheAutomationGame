# ADR-0010: Versioned Process Drafts and Authoritative Application

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S022 must let a player alter a captured process without editing files or directly mutating live dishes. The S021 artifact already preserves observed actions, workstations, state transitions, ownership, provenance, and baseline/current v1. The current dish simulation has two concrete editable execution dimensions: which actor performs a supported step and whether delegated work prioritizes plates, glasses, or its existing captured/default order.

## Decision

Editing creates a mutable simulation-owned draft copied from an artifact's current immutable version. Steps retain stable IDs while their display sequence, assignment (`player` or `new hire`), and routing policy are edited through explicit replay-serialized commands. Baseline remains immutable.

Apply validates a nonempty unique contiguous sequence and adjacent state compatibility. The washer's asynchronous `StartWasher -> Unload` handoff is the one explicit supported transition whose physical cycle supplies the intermediate `WashedInMachine` state. Invalid drafts remain visible with targeted diagnostics and cannot apply. A valid apply derives current version `N+1`, records edit provenance, closes the draft, and marks that artifact as the active delegated-work definition.

The authoritative new-hire executor consults the applied definition: it considers only actions assigned to actor 1 and resolves plate/glass choice through the applied routing policy. The editor never changes dish state. The Stride modal is a projection/controller over snapshot/commands and pauses simulation through an independently tested client policy.

## Alternatives considered

- Let the client mutate captured arrays directly. Rejected because validation, replay, and applied truth would become presentation-owned.
- Permit arbitrary step creation/deletion. Rejected because S022 has no safe action-construction ontology or generic workflow engine.
- Treat reordering as cosmetic. Rejected because invalid state transitions must be explainable and blocked.
- Reuse legacy new-hire training flags as editor state. Rejected because an owned process version must remain explicit, inspectable, and independently versioned.

## Consequences

### Positive

- draft and applied state are explicit and replayable;
- baseline/current comparison is durable;
- invalid process order has targeted player-facing evidence;
- applied assignment and routing produce measurable world consequences;
- the client remains a semantic input/presentation adapter.

### Negative

- v1 supports a linear sequence and two actors only;
- asynchronous transition validation has one concrete dish-washer rule;
- routing is a closed three-policy enum rather than a general predicate;
- editing one artifact at a time is intentional.

## Validation

- reorder a captured step into an invalid transition and prove apply is rejected;
- restore valid order, assign steps to the new hire, set glass-first, and prove current v2 with baseline v1;
- rerun equivalent plate/glass work and compare service shortage/transition outcomes against plate-first;
- replay active drafts and applied versions;
- cover semantic input context, modal routing, presenter output, and editor pause policy without GUI automation.

## Revisit when

- a concrete process needs branches, loops, step insertion/removal, multiple worker roles, or workstation reassignment;
- S023 automation rules need to reference applied process versions;
- warehouse reuse reveals a cross-industry routing-policy seam;
- persistence requires conflict resolution between concurrent/current versions.
