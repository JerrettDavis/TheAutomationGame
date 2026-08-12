# Canonical `/goal` Prompt

> Drop this suite into `./docs`, then use `/goal` to execute one fixed delivery session at a time.

## Invocation

```text
/goal
```

Optional targeting:

```text
/goal S001
/goal N3
/goal "make first-time movement comfortable"
```

An explicit session ID wins. A gate/intent is used to choose the earliest unblocked matching session.

---

## Agent instruction

When the user invokes `/goal`, execute the following protocol.

### 1. Read only the progressive-disclosure minimum

Read:

1. repository `AGENTS.md` and any nearer scoped agent instructions;
2. `docs/31_NEXT_PHASE_INDEX.md`;
3. `docs/34_SESSION_DELIVERY_MODEL.md`;
4. `docs/35_SESSION_BACKLOG.md`.

Then open only the specialist documents linked by the selected session. Do not preload the entire docs tree.

### 2. Establish repository truth

Before choosing/implementing work:

- inspect `git status`;
- do not overwrite unrelated local changes;
- search the codebase for the relevant current symbols/behavior;
- inspect the minimum relevant source ranges;
- check current tests/tools/docs when they are directly relevant;
- prefer repository reality over stale roadmap prose.

If backlog status is stale but code clearly proves a session is complete, update the backlog with evidence and select the next uncompleted session.

### 3. Select exactly one session

Selection order:

1. explicit `S###` requested by the user;
2. earliest incomplete session matching an explicit gate/intent;
3. otherwise the first `TODO` session whose prerequisites are complete and which is not blocked.

Do not start a second session after completing the first.

### 4. Emit the Goal Contract before editing

Use:

```yaml
session: S###
title: ...
gate: N#
player_value: "..."
include:
  - ...
exclude:
  - ...
proof:
  automated:
    - ...
  playable:
    - ...
stop_when:
  - ...
```

Resolve small ambiguities from existing design intent. Do not ask the user for choices that this documentation already settles.

### 5. Implement vertically

Rules:

- preserve deterministic simulation authority;
- presentation submits commands and displays state;
- use stable IDs;
- do not introduce unused frameworks;
- move prototype/debug shortcuts behind developer tools instead of deleting useful diagnostics;
- generalize only with concrete evidence;
- update visible hints/docs whenever controls or semantics change;
- add focused tests for semantics changed;
- if content changes, validate content and deterministic scenario outcome;
- if a presentation-only change alters headless behavior, investigate it as a defect.

### 6. Run the validation ladder

Use the narrowest focused checks first, then as applicable:

```powershell
dotnet build TheAutomationGame.sln -c Release
dotnet test TheAutomationGame.sln -c Release --no-build
dotnet run --project src/Automation.Headless -c Release -- --ticks 250 --seed 42
```

Use repository-native equivalents if paths/options have changed.

For client/input/UI work, run `tools/ui-smoke.ps1` when applicable and provide concise manual acceptance steps. Follow any safety/process requirements documented by the script itself.

Do not claim a playable result from compilation alone.

### 7. Update durable project knowledge

Before ending:

- mark the session `DONE` only if proof passed;
- add concise proof/evidence below it or in the project's preferred session log;
- record newly discovered gaps with IDs or bounded follow-up session proposals;
- update `45_DECISION_LOG_AND_OPEN_SPIKES.md` if a spike settled a decision;
- update specialist docs only when implementation invalidates or concretizes them.

Do not rewrite roadmap documents merely to narrate the code diff.

### 8. Stop at the contract boundary

Return:

```text
SESSION: S### — Title
STATUS: COMPLETE | BLOCKED

DELIVERED
- ...

PROOF
- ...

FILES
- ...

FOLLOW-UPS DISCOVERED
- ...

NEXT
- S### — ...
```

Then stop. The next session begins only on the next `/goal` invocation.

---

## Token-minimization rules

The agent should:

- search before reading full files;
- open targeted docs only;
- avoid restating architecture already captured here;
- use existing tests/headless traces as authoritative evidence;
- write durable decisions back once so later agents do not repeat research;
- prefer a small code change with a complete proof over broad scaffolding;
- not generate plans for later roadmap gates during an implementation session unless a blocker requires a bounded follow-up.

## Blocked session behavior

If the goal cannot be completed because one uncertain technical choice blocks it:

1. keep the original session `BLOCKED`;
2. create or use a single bounded SPIKE from `45_DECISION_LOG_AND_OPEN_SPIKES.md`;
3. the spike must answer one decision with evidence;
4. do not transform the spike into a production rewrite;
5. end after the decision and specify how the blocked session can resume.

## Example

User:

```text
/goal
```

Agent should effectively do:

```text
Select S001.
Read input specialist doc.
Inspect current key handling and movement command.
State S001 contract.
Implement WASD through authoritative movement.
Keep click move.
Move W's scenario shortcut to developer controls.
Test.
Provide manual launch proof.
Mark S001 complete.
Stop and point to S002.
```

It should **not** also implement S002 interaction, S003 camera controls, or S004 rebinding because those are attractive while the same files are open.
