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

Native automation proves reachability and projection, not comprehension or human-paced duration. S035 is therefore a research episode, not a simulation episode:

- **starting state:** S034 has a tested, presentation-complete restaurant chapter but no human session evidence;
- **participant episode:** a new player completes a fresh career through **Own the Shift**, explains the observed system in their own words, then compares the two restaurant stations and describes the recurring routing move before choosing the Codex reflection that reveals its conventional name;
- **facilitator behavior:** observe first attempts, stalls, false assumptions, ignored UI, predictions, retries, and exact interventions without naming the intended concepts or directing consequential actions;
- **terminal outcome:** at least five versioned human observations and their objective completion exports produce one deterministic readiness report with a pass/fail result for every N5 criterion and owned follow-ups for repeated blockers or critical UI/accessibility issues;
- **non-human proof:** automated fixture sessions validate collection, rejection, aggregation, and report formatting but are explicitly excluded from the human sample count.

### Session procedure

1. Launch `.\tools\playtest-first-hours.ps1 -PlayerId <anonymous-id>` to use a clean, isolated save and the normal visible Windows client. Do not use semantic-driver controls, god tools, or facilitator-directed actions.
2. Keep the generated `facilitator-debrief.md` with the session. Record player background, movement/interaction discovery, the first attempted response to each pressure, stalls, ignored feedback, retries, and every intervention with its time and exact help.
3. At **Own the Shift**, the client atomically writes `first-hours-evidence.json`: anonymous session identity, guidance/comfort choices, wall-clock and active-simulation duration, quest outcomes, handbook visits, reliability trial, and frozen scorecard. An incomplete session intentionally emits no completion evidence; its isolated save and observation still count when assessing progression blockers.
4. Without reopening the journal or introducing design vocabulary, ask:
   - What constrained glass service, and which observed evidence supports that conclusion?
   - What did the panel say was true during the incident, and what was physically true?
   - Why are the captured replay and live reliability window stronger evidence than another happy-path run?
   - Would you trust the revised system during another rush? Why?
5. Let the player use the post-shift **Two Stations** comparison. Before they acknowledge the Codex reflection/reveal, ask: "What part of your solution stayed the same, and what part changed between stations?" Record a pass only if they independently describe a stable decision slot with swappable routing choices in ordinary language. Do not prompt with *Strategy*, *policy*, *interface*, or *pattern*.
6. Score bottleneck, readiness disagreement, and replay/proof independently. A causal answer links an observed state/event to a service or reliability consequence; repeating a label, selected button, or facilitator wording is insufficient.
7. After the client closes, record the structured observation. Every critical UI/accessibility issue needs a stable code, concise summary, owner, and either `Fixed` or `Backlog` disposition.

Players may open the Shift Handbook with `F12`; consulting an available control is not facilitator intervention. The launcher does not opt into developer tools, and the client keeps consequence-bypassing tools locked until the shift is complete.

### Fixed formative cohort contract

Record this contract before collecting evidence so results do not redefine the gate:

- minimum five human sessions, including at least two vocabulary novices;
- at least one Guided and one Contextual session; Minimal is exploratory until those modes are represented;
- at least 80% complete without action-directed help;
- at least 80% discover both movement and contextual interaction without coaching;
- at least 80% causally identify a meaningful bottleneck;
- at least 80% explain reported-versus-physical readiness after the incident;
- at least 80% articulate why replay/proof matters;
- at least 60% express the Strategy shape before naming;
- no progression blocker appears in more than one session;
- every critical UI/accessibility issue has a fix or backlog owner;
- at least 80% of completed first shifts finish in the initial **45–120 wall-clock minute** formative envelope.

For cohorts larger than five, the percentage thresholds apply to every recorded human observation rather than a favorable subset. The duration envelope is a formative study target, not a promise of final campaign length; revise it only in a dated decision after reviewing this cohort.

The evidence schemas contain no name or contact field. Use a study-local anonymous `PlayerId`; do not put personal information in IDs or notes. Run `.\tools\summarize-first-hours-playtests.ps1` to validate session/evidence identity and write the durable Markdown report. The gate cannot pass from missing observations, incomplete issue ownership, automated fixtures, or console claims. Log confirmed friction in the nearest authoritative quest, UX, or simulation document before expanding the sample.
