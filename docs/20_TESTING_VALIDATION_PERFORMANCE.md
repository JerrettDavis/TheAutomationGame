# Testing, Validation, and Performance

## Quality principle

Test the claim at the cheapest level capable of disproving it.

## Test layers

### Domain tests

Pure rules, invariants, state transitions, calculations.

### Simulation tests

Multiple actors/systems over ticks without renderer.

### Content tests

Schema, references, reachability, scenario conditions, progression rules.

### Contract tests

Boundaries among capabilities/adapters.

### Headless scenario tests

Run complete scenarios across seeds and assert outcome envelopes.

### Client tests

Presentation mapping, interaction/input, UI state.

### Performance tests

Repeatable benchmark worlds and regression thresholds.

## Property/model testing

High-value invariants should use generated state/command sequences where useful.

Examples:

- inventory cannot become negative unless definition explicitly permits backlog;
- one idempotent payment command cannot create multiple accepted tenders;
- completed work items do not re-enter active queues without explicit reopen transition.

## Failure injection

The simulation test harness should be able to inject:

- machine failure;
- stale sensor;
- timeout;
- duplicate event;
- lost message;
- slow dependency;
- worker absence;
- power/network outage;
- malformed input.

## Initial performance budgets

These are engineering targets, not promises:

### Headless synthetic benchmark

- 100,000 active simple actors;
- 1,000,000 passive resources/items;
- 10,000 concurrent process instances;
- 1,000 facilities in aggregated or mixed fidelity;
- 10 Hz principal work tick under ~20 ms on a representative development desktop after optimization phase.

### Client

- 60 FPS target at 1080p/1440p on midrange gaming hardware;
- avoid one full Stride entity per passive high-count object;
- visible actor count target established through prototype benchmark.
- virtual-canvas UI must remain fully visible and readable in a resizable window and at a near-fullscreen 4K viewport;
- native UI smoke covers movement, placement/undo/reset, pan/zoom/reset, every lens, save/restore, the representative benchmark, and a captured 4K-scaled frame.
- the Windows fullscreen probe must show monitor-sized bounds and a matching native backbuffer rather than desktop stretching;
- window captures use exact DWM frame bounds and composited-screen capture so DPI virtualization cannot crop HUD or modal evidence and GPU-backed Stride frames are captured only after presentation.
- the native journey uses OS pointer movement and clicks for onboarding, preference cards, HUD navigation, quest row selection/details, scorecard, career confirmation/resume, floor movement, and workstation interaction; semantic controls remain for long deterministic scenario setup and capabilities without a spatial widget.
- because OS-level pointer input is shared with the interactive desktop, the native journey requires the explicit `-AllowDesktopInput` switch and must run only on an idle desktop or an isolated interactive Windows session.
- progression receipt assertions cover an early level threshold, a capability reward that remains at the same level, and the final level-7 outcome; the captured early receipt is visually reviewed for outcome/XP/capability/rationale legibility.

### Allocation

Steady-state hot simulation ticks should approach zero transient allocation. Do not prohibit allocation in setup, content load, UI, or rare workflows unnecessarily.

## Benchmark worlds

Create versioned benchmark fixtures:

- `bench_001_actor_workloop`
- `bench_002_queue_contention`
- `bench_003_inventory_flow`
- `bench_004_large_facility`
- `bench_005_multi_facility`
- `bench_006_client_instances`

Store benchmark results in CI artifacts.

## Profiling gate

No major data-oriented rewrite is accepted without a captured profile showing the problem it solves and a before/after benchmark.

## Validation of educational content

A scenario fails content QA if players can only succeed by guessing the designer's intended solution rather than reasoning from observable evidence.

## First-hours human validation protocol

Native automation proves reachability and projection, not comprehension or human-paced duration. A first-hours readiness claim therefore requires recorded fresh-career sessions under these conditions:

1. Launch `.\tools\playtest-first-hours.ps1 -PlayerId <anonymous-id>` to use a clean, isolated save and the normal visible Windows client. Do not use semantic-driver controls, god tools, or facilitator-directed actions.
2. Keep the generated `facilitator-debrief.md` with the session. Record player background, quest or control blockers, reliability attempts, and every facilitator intervention. At final completion, `first-hours-evidence.json` atomically records guidance and comfort choices, wall-clock and active-simulation duration, all quest outcomes, handbook open count and first/last tick by tutorial stage, the reliability trial, and the frozen scorecard. An incomplete session intentionally emits no completion evidence; its save and debrief remain available.
3. Include both players unfamiliar with systems/programming vocabulary and players with relevant experience. Exercise Guided and Contextual before treating Minimal as an onboarding default candidate.
4. After **Own the Shift**, open the Shift Scorecard with `K`. Without reopening the journal, ask:
   - What constrained glass service, and which observed evidence supported that conclusion?
   - Which unwritten assumptions failed under delegation or automation?
   - Why were the captured replay and live reliability window stronger evidence than another happy-path run?
5. Score an answer as causal only when it links an observed state or event to the resulting service outcome. Repeating UI labels or naming the chosen button is not sufficient.

Players may open the in-game Shift Handbook with `F12`; consulting an available control is not facilitator intervention. The evidence export records repeated visits by tutorial stage so they can be compared with observed confusion rather than treated as a failure by themselves. The launcher does not opt into developer tools, and the client keeps those tools locked until the first shift is complete.

For the first formative gate, run at least five sessions, including at least two players unfamiliar with the intended vocabulary. Do not claim the first few hours ready unless:

- at least four of five players complete the arc without a facilitator telling them which consequential action to take;
- at least four of five provide causal answers to two of the three debrief questions;
- no common blocker prevents more than one player from advancing;
- observed wall-clock duration supports the intended first-hours envelope rather than relying on active ticks or the accelerated smoke duration;
- failures and retries remain explainable from world feedback rather than external instruction.

Log findings in the nearest authoritative quest, UX, or simulation document and fix common blockers before expanding the sample. The protocol is a gate; an empty checklist is not evidence.

The evidence schema is versioned and contains no name or contact field. Use a study-local anonymous `PlayerId`; do not put personal information in the ID or facilitator notes unless the study has an approved reason and handling policy. On client exit, the launcher prompts for the four structured facilitator judgments used by the gate. Run `.\tools\summarize-first-hours-playtests.ps1` to aggregate those observations with objective evidence; it deliberately leaves the wall-clock envelope as `REVIEW` until the study defines a numeric target.
