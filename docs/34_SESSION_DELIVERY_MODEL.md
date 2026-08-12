# Session Delivery Model

> Rules for turning the roadmap into short, fixed, high-value agent sessions.

## Principle

A session is **not** "work on subsystem X." A session is a bounded contract that changes observable product behavior and ends with evidence.

The ideal session is small enough that an agent can hold the full problem in context, yet vertical enough that a human can test the outcome immediately.

## Goal Contract

At the beginning of every session, write this contract before editing code:

```yaml
session: S001
title: Direct Player Navigation
gate: N1
player_value: "I can move my character naturally with WASD."

include:
  - semantic movement actions
  - WASD input
  - movement through authoritative simulation command(s)
  - keep click-to-move working

exclude:
  - pathfinding rewrite
  - gamepad
  - new art
  - quest changes unrelated to key conflicts

proof:
  automated:
    - release build passes
    - tests pass
    - deterministic headless proof passes
    - input/movement tests added where appropriate
  playable:
    - launch client
    - move with W/A/S/D
    - click-to-move still works
    - W does not invoke a quest/scenario shortcut

stop_when:
  - all proof conditions pass
```

If the work cannot fit this contract, split it before implementation.

## Session classes

### PLAY

Changes directly observable player behavior.

Examples: movement, interaction prompts, process editor, quest beat.

**Required proof:** human reproduction steps plus automated regression where possible.

### PLATFORM

Creates a seam required for immediate subsequent content or presentation work.

Examples: content compiler, presentation catalog.

**Required proof:** one real existing feature migrates to and uses the seam. Empty frameworks do not count.

### PRESENTATION

Changes visuals, animation, audio, or information hierarchy.

**Required proof:** before/after observable in a runnable scenario; no simulation semantic drift.

### CONTENT

Adds or transforms authored gameplay.

**Required proof:** content validation + playable/headless path + expected quest/outcome.

### SPIKE

Answers one uncertainty that blocks a real delivery decision.

A spike does not become production architecture by default.

**Required proof:** comparison, tiny executable prototype when appropriate, measured result, and explicit decision/recommendation.

## Before editing

The agent must:

1. read the minimum progressive-disclosure set;
2. inspect `git status` and avoid overwriting unrelated user work;
3. locate the current concrete implementation with search rather than assuming a stale design doc is correct;
4. run the narrowest useful baseline test or reproduce the current behavior;
5. state the Goal Contract.

## Implementation rules

1. Preserve simulation authority and determinism.
2. Stride/presentation code may request commands and display state, not invent gameplay truth.
3. Use stable content/domain IDs. Do not persist asset paths as gameplay identity.
4. Prefer one concrete seam over a speculative framework.
5. Generalize after two real uses reveal the shared shape.
6. Add tests around new semantics, not implementation trivia.
7. Keep developer cheats/shortcuts available when useful, but isolate them from production controls.
8. If changing content schemas, include migration or explicit compatibility handling.
9. If changing input, update visible hints and documentation in the same session.
10. If a bug is found outside scope, record it; fix only if it blocks the session or is trivial and safe.

## Validation ladder

Run the cheapest relevant proof first and stop early on failure:

```text
focused unit/content test
    ↓
project-level build/test
    ↓
solution Release build/test
    ↓
headless deterministic scenario
    ↓
native UI smoke if affected
    ↓
manual playable acceptance steps
```

Recommended standard commands, adjusted only when repo truth differs:

```powershell
dotnet build TheAutomationGame.sln -c Release
dotnet test TheAutomationGame.sln -c Release --no-build
dotnet run --project src/Automation.Headless -c Release -- --ticks 250 --seed 42
```

Use `tools/ui-smoke.ps1` for native-client flows when relevant, respecting any script-specific requirements documented in the repository.

## Evidence bundle

Every completed session should leave:

- changed implementation;
- focused tests;
- deterministic proof if simulation/content changed;
- manual acceptance steps;
- screenshots/logs only when useful, not as a substitute for tests;
- updated backlog status/evidence;
- a short decision note if the session settled an open design question.

## Completion report

End with exactly this information:

```text
SESSION: S### — Title
STATUS: COMPLETE | BLOCKED

DELIVERED
- ...

PROOF
- command/result
- manual reproduction

FILES
- important paths only

FOLLOW-UPS DISCOVERED
- IDs or concise backlog candidates

NEXT
- next unblocked session ID
```

Do not continue into the next session.

## Blocker rule

If a dependency or unknown blocks delivery:

1. stop broad implementation;
2. define the smallest bounded spike capable of answering the unknown;
3. record the original session as blocked by that spike;
4. execute the spike only if it fits the current session budget and has no destructive consequences;
5. otherwise end with the exact proposed spike contract.

Never hide an unresolved architectural choice inside a large refactor.

## Token discipline

Agents should minimize context by:

- reading index + session + linked specialist docs only;
- searching for symbols before opening entire source files;
- reading targeted line ranges;
- trusting passing baseline tests rather than re-deriving whole systems;
- avoiding long restatements of design documents;
- writing decisions back into docs so future sessions do not repeat research;
- keeping one session to one player-visible or architecture-proof outcome.

## Session size heuristics

Split a session when it requires more than one of these:

- new persistent schema **and** new production UI;
- new renderer path **and** new gameplay semantics;
- multiple industries;
- unrelated input + quest + economy changes;
- a broad refactor touching many assemblies without a single immediate proof.

A session may touch many files if they all serve one vertical outcome.
