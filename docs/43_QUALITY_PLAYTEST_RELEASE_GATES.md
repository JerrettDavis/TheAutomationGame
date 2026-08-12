# Quality, Playtest, and Release Gates

> A feature is not complete because it compiled. This game must prove correctness, playability, comprehension, and consequence.

## Per-session proof

Every delivery session must satisfy its Goal Contract and applicable layers below.

### 1. Static/build

- Release build succeeds;
- warnings introduced by the change are addressed or explicitly justified;
- content/schema compile succeeds where applicable.

### 2. Focused tests

Test the semantic change directly.

Examples:

- movement rejects blocked cell;
- template expansion is deterministic;
- quest condition detects outcome;
- rule trace reports observed value;
- PatternKnowledge records transfer once per qualifying event policy.

### 3. Regression

Run the relevant project/solution suite.

### 4. Determinism

For gameplay-affecting changes:

- fixed seed + fixed content + fixed commands produce same authoritative digest/outcome;
- presentation-only changes must not change digest.

### 5. Headless

Representative scenarios run without Stride and expose useful summary/trace output.

### 6. Native UI smoke

Use for changes affecting:

- startup;
- input;
- journal/modals;
- save/load;
- settings;
- editors;
- world interaction.

### 7. Manual acceptance

A person can reproduce the promised value from concise steps.

## Content gates

Shipping content must pass:

- unique/stable IDs;
- valid references;
- schema compatibility;
- deterministic template expansion;
- required quest outcomes reachable in representative scenario validation;
- required presentation fallbacks;
- pattern IDs aligned with imported catalog;
- no developer-only shortcut required for critical path.

The implemented schema-v1 gate is `Automation.Content.Tests`. It compiles every checked-in valid bundle and exercises durable seeded invalid cases for IDs, duplicates, references, route transitions, scenario configuration, quest-beat reachability, and presentation fallbacks. Diagnostics are asserted by source, semantic path, and message so a generic failure does not satisfy the gate.

## Playtest evidence types

Automated tests answer **does the software behave as specified?**

Human playtests answer **does the player form the intended mental model?**

Both are required.

### Observation protocol

Do not coach unless the test specifically evaluates assisted onboarding.

Record:

- timestamp/location of stalls;
- what the player tried first;
- words they use to describe the system;
- false assumptions;
- ignored UI;
- accidental successes;
- whether they can predict consequences before acting;
- whether they can explain outcomes afterward.

Ask after the relevant play segment, not before:

- "What do you think is causing the delay?"
- "What would you change next?"
- "What does the game say is true, and what do you think is actually true?"
- "Why did that automation act?"
- "Would you trust this change during another rush? Why?"

Avoid leading with vocabulary such as bottleneck, state machine, Strategy, Observer, idempotency.

## N1 Gate — Comfortable Interaction

Pass when, across representative new players:

- movement is discovered without instruction;
- interaction is discovered from context affordance;
- camera can be recovered/recentered;
- no critical-path quest depends on memorized prototype keys;
- blocked actions explain why;
- no severe input-focus bugs remain.

## N2 Gate — Presentation Seam

Pass when:

- authored 3D room can replace placeholders without simulation changes;
- important work/equipment states are legible at intended zooms;
- actor selection/interactions remain reliable;
- fallback presentation works;
- real-asset performance is measured and inside provisional budget.

## N3 Gate — Content Platform

Pass when:

- first-shift narrative and dish scenario are loaded through the content pipeline;
- invalid content fails with actionable diagnostics;
- deterministic expansion tests pass;
- content-only changes do not require simulation source edits;
- ID/reference coverage is automated.

## N4 Gate — Player Tools

Pass when a player can:

1. observe/capture a process;
2. create a variant;
3. run the same controlled pressure;
4. compare outcomes;
5. create one automation rule;
6. inspect why it did or did not fire;
7. revert/disable it.

No console/dev shortcut may be required.

## N5 Gate — Restaurant Production Slice

Minimum **five first-hours human sessions** before declaring the chapter internally ready.

The fixed first formative cohort includes at least two vocabulary novices, at least one Guided session, and at least one Contextual session. Pass criteria are:

- at least 4/5 discover core movement/interaction without coaching;
- at least 4/5 complete without action-directed facilitator help;
- at least 4/5 correctly identify a meaningful bottleneck after observation;
- at least 4/5 understand that reported readiness can disagree with reality after the incident;
- at least 4/5 can articulate why replay/proof matters;
- at least 3/5 independently express the Strategy concept in ordinary language before naming;
- no recurring progression blocker;
- critical UI/accessibility issues have fixes/backlog owners;
- at least 4/5 completed shifts fall inside the precommitted 45–120 wall-clock minute formative envelope.

For larger cohorts, the 80%/60% proportions apply to all recorded human sessions rather than a selected five. These thresholds are initial internal gates, not scientific claims. Adjust only after collecting evidence and record the change.

## N6 Gate — Warehouse Reuse Proof

Pass when:

- warehouse runs headlessly and in client;
- restaurant still works;
- shared concepts have neutral names and two concrete uses;
- no dish-specific simulation assumption is required;
- second-industry authoring requires substantially less code than the first;
- reuse audit documents what was generalized and what intentionally was not.

## N7 Gate — Pattern Learning

Pass when:

- player evidence can unlock/advance Strategy without a quiz;
- previous evidence changes later presentation/dialogue;
- one pattern transfers across restaurant and warehouse;
- Codex shows player-specific history;
- coverage validation reads imported PatternKit IDs;
- naming the pattern does not gate use of the mechanic.

## Performance gates

Keep simulation and presentation budgets separate.

### Simulation

Retain deterministic performance scenarios at small and large scales. Track regressions in:

- tick/update time;
- allocations where measured;
- event/trace growth;
- save/replay size.

### Client

At each presentation milestone measure:

- near room;
- mid facility;
- far/aggregate;
- dense workers/items;
- active lens overlays;
- open editor/modal.

Do not optimize only an empty scene.

## Regression artifacts

Useful retained artifacts:

- deterministic digest for canonical seeds;
- normalized compiled-content snapshot/hash;
- first-shift headless report;
- warehouse canonical report once available;
- at least one inspected reviewer screenshot for each delivered client-facing feature;
- an integrated consequence screenshot for domain/content work that has no standalone visual surface;
- playtest observation summaries.

Do not make binary screenshot equality the primary gameplay test.

The native smoke runner accepts `-RetainScreenshotsPath docs/screenshots/first-shift` to refresh the checked-in gallery. A delivery is not visually evidenced merely because a PNG exists: inspect the retained frame for cropping, stale state, illegible text, and whether it actually shows the claimed feature. Record the session-to-frame mapping in `47_FEATURE_SCREENSHOT_GALLERY.md`.

## Alpha gate

Campaign alpha requires:

- all main chapters playable;
- all 23 GoF primary exposures implemented/planned through tested content;
- player tools stable;
- save/load stable enough for campaign-length use;
- accessibility/settings baseline;
- no placeholder on critical path unless explicitly accepted;
- economy/progression supports complete campaign;
- external cohort can finish without developer intervention.

## Beta gate

Beta is content/polish/balance stabilization:

- no major architecture migration scheduled;
- performance target hardware passes;
- save migrations exercised;
- crash/progression blockers low and triaged;
- pattern/comprehension pacing validated;
- controller/platform support complete if in 1.0 scope;
- localization pipeline ready if in 1.0 scope.

## 1.0 gate

1.0 requires a coherent complete game, not total catalog saturation.

- main campaign complete;
- sandbox/replay meaningful;
- critical PatternKit curriculum works as designed;
- presentation/audio/accessibility/settings complete for target platforms;
- known severe bugs resolved;
- performance/reliability targets met;
- content pipeline stable enough for maintenance and expansion.
