# UI, UX, and Game Feel

> Information architecture for a game that teaches through the world first and abstraction second.

## Core hierarchy

The interface should reveal information in layers:

```text
WORLD
  ↓
COMPACT HUD
  ↓
INSPECT
  ↓
SYSTEM LENS
  ↓
EDITOR / ANALYSIS TOOL
```

A player should not need the deepest layer to perform ordinary work.

## The world must carry meaning

Before opening panels, players should be able to see or hear:

- where work is accumulating;
- whether equipment is busy/idle/blocked/failed;
- what a worker is doing or waiting for;
- when a handoff happens;
- which station is starved or overloaded;
- when an automation acts;
- when reported state and physical state disagree, at least once they investigate.

Labels supplement the world rather than replacing it.

## Compact gameplay HUD

Keep persistent HUD minimal:

- current objective/shift phase;
- time/shift state when relevant;
- selected target summary;
- context action prompt;
- critical notifications;
- small resource/economy summary once unlocked.

Avoid permanent walls of metrics. Detailed operational metrics belong in lenses/inspectors.

## Context affordances

For the focused target, present:

```text
Washer 01
Washing • 00:18 remaining

[E] Inspect cycle        // example action
[F] Inspect workstation
```

When blocked, explain the state:

```text
[E] Load washer
Unavailable: no dirty rack at load port
```

This turns a failed action into learning rather than friction.

## Selection

Selection should be visually obvious and independent of color alone:

- outline/ring;
- small marker;
- optional nameplate;
- inspect summary.

Hover may preview but should not be required for touch/controller futures.

## Notifications

Use priority classes:

1. **Ambient:** completed work, minor changes. Usually world/audio only.
2. **Operational:** queue threshold, worker waiting, automation action. Compact feed/toast.
3. **Important:** quest beat, incident, failed rule. Persistent until seen or timed carefully.
4. **Critical:** safety/shift-blocking failure. Strong visual + audio + clear required attention.

Do not teach the player to ignore everything by making routine events flash.

## Journal and goals

Journal should answer:

- What am I trying to accomplish?
- What evidence have I gathered?
- What changed?
- What did the system ask me to prove?
- What optional leads exist?

Quest copy should describe conditions/outcomes rather than mandatory implementation.

Bad:

> Move the clean rack two tiles left.

Better:

> Reduce the time service spends waiting for clean glasses during the rush.

The first may be a hint, not the requirement.

## System lenses

Preserve the existing conceptual lenses:

- Reality
- Process
- State
- Knowledge
- Automation
- Architecture
- Runtime
- Code, later

### Lens rules

1. A lens changes **what the player can inspect**, not simulation truth.
2. Lenses may add overlays, arrows, timelines, ownership, metrics, or traces.
3. One lens should have one primary question.
4. Dense labels collapse at far zoom.
5. Cross-lens links are explicit: "View this rule in Automation" / "View reported value in Knowledge."

Example primary questions:

| Lens | Question |
|---|---|
| Reality | What physically exists and what is happening? |
| Process | How is work supposed to flow? |
| State | What states/transitions exist right now? |
| Knowledge | Who/system believes what, and why? |
| Automation | What rules act automatically? |
| Architecture | What owns/depends on what? |
| Runtime | What executed, when, and with what cost? |
| Code | How is this represented programmatically? |

## Editors

Process and automation editors are major play surfaces, not debug windows.

They need:

- explicit draft vs applied state;
- validation before commit;
- undo/version/checkpoint behavior;
- baseline/variant comparison;
- links back to world entities;
- explainable failure messages;
- preview where semantics permit it.

Pause by default while editing early in the campaign. Later scenarios may intentionally require live operational changes.

## Pattern Codex UX

The Codex should open with **the player's lived history**, not textbook prose.

Example:

```text
STRATEGY

First encountered
Rossi's Restaurant • Dish Station • Shift 4

You used it when
You created separate rush and normal routing policies.

You reused it
Warehouse • Storage routing

What people commonly call this
Strategy Pattern
```

Then expose tabs/sections:

- Lived
- Structure
- Tradeoffs
- Related
- Code
- PatternKit

No multiple-choice quiz is required to "earn" the name.

## UI architecture seam

Before committing to a large UI rewrite, extract concrete responsibilities:

```text
ScreenRouter
ModalStack
HudPresenter
ContextPromptPresenter
NotificationPresenter
JournalPresenter
LensPresenter
SettingsPresenter
```

These are presentation/application responsibilities. Keep them independent of whether the widgets are currently SpriteBatch-drawn, Stride UI, or a later solution.

### UI implementation decision

Do a bounded spike using one real existing screen, preferably Journal or Settings:

Compare:

- current custom immediate/procedural UI approach;
- Stride-native UI/layout tooling available in the current version;
- a minimal retained custom layer only if native constraints are material.

Measure:

- text/layout quality;
- focus/input handling;
- scaling;
- implementation complexity;
- iteration speed;
- controller/accessibility implications;
- performance.

Do not rewrite every screen during the spike.

## Typography

Production UI needs:

- readable proportional UI font for body text;
- strong numeric/tabular treatment for operational values;
- distinct but restrained headings;
- fallback glyph plan;
- scaling without pixel-font dependence.

Pixel styling can remain an artistic choice for selected accents, not an accessibility constraint.

## Iconography

Create a coherent icon vocabulary for:

- work/item categories;
- queue/buffer;
- worker;
- inspect;
- process;
- automation;
- warning/error;
- reported/observed state;
- architecture/dependency;
- cost/time/throughput;
- save/checkpoint;
- pattern/Codex.

Every critical icon needs text/shape support. Color alone is insufficient.

## Accessibility baseline

Before campaign alpha:

- rebindable keyboard/mouse actions;
- UI scale;
- text size where layout permits;
- high-contrast/legible focus states;
- color-independent statuses;
- captions/text equivalents for information-bearing sound;
- reduce flashing;
- reduce camera motion/shake;
- mouse sensitivity;
- pause during reading/editing by default;
- no required rapid input unless the mechanic intentionally models motor work.

## Game feel stack

Every meaningful action can combine:

1. authoritative state transition;
2. immediate animation pose/anticipation;
3. item/equipment motion;
4. sound;
5. small VFX;
6. compact world/HUD state change;
7. metric/trace update in deeper views.

Avoid fake latency in gameplay truth. Presentation can anticipate or interpolate but must converge on authoritative results.

## Onboarding target

By the end of the first 10–15 minutes, without external instructions, a player should have discovered:

- movement;
- interaction;
- inspection;
- current objective;
- selection;
- camera zoom/pan;
- that waiting/queues matter.

System lenses and editors should unlock progressively after the player experiences the problem they illuminate.
