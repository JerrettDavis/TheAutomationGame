# ADR-0012: Player-Owned Automation Rule Drafts

- Status: Accepted for prototype
- Date: 2026-08-12

## Context

S023 established deterministic automation semantics, but the first-shift scenario still selected prebuilt washer policies through direct commands. S024 must let the player create and refine the real live rule without exposing arbitrary commands or allowing the client to own simulation state.

## Decision

The simulation owns one editable player rule at stable ID `automation.rule.dish-station.player-start-washer`. Its v1 editor exposes enabled state, three Boolean condition capabilities (`RackPresent`, `ReportedReady`, and `PhysicalReady`), and the single closed `StartWasher` effect.

Begin, set-enabled, toggle-condition, set-action, apply, and discard are explicit replay-serialized simulation commands. Each change regenerates targeted draft diagnostics. Invalid drafts remain inspectable and cannot apply. Apply compiles the draft to the S023 IR, replaces the active rule, clears an automation halt so a corrected rule can resume, and leaves world mutation to the evaluator-selected effect's existing authoritative `Perform` path.

The Stride client supplies only a paused semantic modal and presenter. It reads the draft, diagnostics, and latest bounded evaluator trace from the snapshot and requests commands. The canonical first-shift run creates the reported-ready rule and later refines that same stable rule with physical readiness; it no longer advances those beats through direct policy-selection shortcuts.

Legacy `ConfigureWasherAutomationCommand` and `WasherAutomationPolicy` remain compatibility surfaces for authored initial state, existing saves/tests, and explicit simulation setup. They are not the production client authoring path.

## Alternatives considered

- Persist editor-local state and submit only a final policy. Rejected because invalid drafts, save/replay reconstruction, and authority would be presentation-owned.
- Build a generic graph editor. Rejected because only one real rule and one effect capability have been proven.
- Add arbitrary action selection. Rejected because the washer editor has authority only to request `StartWasher`.
- Delete the compatibility policy command immediately. Rejected because it remains useful for initial scenario configuration and backwards-facing deterministic setup.

## Consequences

### Positive

- a player-created artifact drives the real live automation path;
- draft and applied state reconstruct exactly from the command journal;
- unsafe and corrected behavior retain the same stable player rule identity;
- validation and trace evidence are visible without granting client authority;
- the next session can compare presets over a concrete owned rule.

### Negative

- v1 edits one rule and one action only;
- action is visible but intentionally locked because no second safe effect exists;
- condition editing is a finite capability list rather than a generic expression tree UI;
- the input profile schema adds six editor actions; the client migrates v2 profiles by preserving existing bindings and appending only their new defaults.

## Validation

- create and enable the reported-ready draft, apply it, and observe an authoritative washer start;
- make rack/readiness/action combinations invalid and prove targeted blocked apply;
- reproduce an unsafe captured input, add physical readiness to the same rule, and prove replay prevention;
- restore both an applied rule and an open draft through replay save;
- project enabled state, conditions, action, diagnostics, and latest inputs/predicates/outcome through the client presenter;
- run the canonical scenario using only draft/apply commands for its automation configuration beats.

## Revisit when

- S025 introduces baseline/variant presets and controlled comparison;
- a second action or rule proves selection, ordering, or conflict semantics;
- player-authored persistence needs a compact serialized IR independent of replay history;
- a later code surface compiles to the same restricted capability model.
