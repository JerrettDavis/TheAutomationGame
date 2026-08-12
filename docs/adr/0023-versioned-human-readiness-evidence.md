# ADR-0023: Versioned human-readiness evidence and fixed cohort evaluation

- Status: Accepted
- Date: 2026-08-12

## Context

Native smoke and deterministic scenarios prove reachability and correctness, but cannot prove that a new player discovers interaction or forms the intended causal model. The original facilitator script reduced several N5 criteria to one `causalAnswers` count and printed an ephemeral summary. That could neither identify which concept failed nor produce a durable, auditable readiness decision. Automated journeys must also be unable to masquerade as human comprehension evidence.

## Decision

Keep objective first-shift completion evidence and facilitator judgments as separate versioned DTOs joined by anonymous session ID. Schema-v3 facilitator observations independently record selected guidance mode, movement, interaction, bottleneck, reported-versus-physical readiness, replay/proof value, pre-name Strategy expression, directed help, blocker code, and owned critical UI/accessibility issues. Completed sessions must agree with objective guidance evidence; incomplete sessions retain the facilitator-recorded choice.

Evaluate every recorded human session against the precommitted S035 cohort contract in `20_TESTING_VALIDATION_PERFORMANCE.md`. A `SyntheticFixture` participant kind exists only to validate ingestion and reporting and is excluded from every human threshold. Reject missing/mismatched identity, unsupported schema, incomplete claimed completion, duplicate/unowned issues, or unstructured session directories. Write the complete criteria/session/follow-up result as deterministic Markdown.

Raw session artifacts remain ignored and study-local by default. A de-identified aggregate report may be committed after review; names and contact fields are not part of either schema.

## Consequences

- A green native journey cannot satisfy the five-human-session gate.
- Each failed concept produces an independent count and prioritized follow-up rather than hiding inside a composite score.
- Thresholds and the 45–120 minute formative envelope are fixed before collection, reducing post-hoc reinterpretation.
- Incomplete sessions remain visible as blocker evidence without fabricating completion exports.
- S035 remains open until real human evidence satisfies or fails the gate; tooling completeness alone is not readiness.
