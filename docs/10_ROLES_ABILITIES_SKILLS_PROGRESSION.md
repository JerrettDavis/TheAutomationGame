# Roles, Abilities, Skills, and Progression

## Philosophy

Progression should primarily unlock **ways of seeing and acting**, not arbitrary stat bonuses.

The player becomes more capable because they can observe more variables, model more concepts, delegate more effectively, inspect deeper evidence, and manipulate larger systems.

## Career progression

The game may begin with ordinary entry-level jobs and grow into technical/organizational roles.

A representative path:

```text
Worker
  -> Experienced Worker
    -> Lead / Supervisor
      -> Process Analyst
        -> Systems Analyst
          -> Automation Specialist
            -> Developer
              -> Systems Designer
                -> Architect
                  -> Technical Leader / Owner
```

This is not a mandatory linear class tree. Different industries and player choices create alternate paths.

## Roles

A role grants:

- authority over particular decisions;
- access to information;
- tools;
- organizational scope;
- responsibilities;
- consequences for outcomes.

Roles are both progression and simulation concepts. NPCs use the same role model where practical.

## Abilities

Abilities are concrete player actions.

### Observation abilities

- watch process;
- time step;
- count arrivals/completions;
- inspect queue;
- interview worker;
- compare shifts;
- capture exception;
- inspect machine history.

### Modeling abilities

- draw process;
- identify actor;
- define state;
- define interaction;
- record assumption;
- mark invariant;
- define expected outcome;
- create scenario.

### Improvement abilities

- rearrange layout;
- alter staffing;
- change sequence;
- add checkpoint;
- remove unnecessary step;
- standardize work;
- create fallback.

### Automation abilities

- configure machine;
- add sensor;
- create rule;
- create state transition;
- route event;
- call capability;
- add retry/timeout;
- add reconciliation;
- script behavior;
- write code.

### Validation abilities

- replay scenario;
- generate cases;
- run load simulation;
- inject failure;
- compare expected/observed;
- create automated test;
- inspect trace.

### Delegation abilities

- assign worker;
- create procedure;
- contract specialist;
- create acceptance criteria;
- request research;
- request implementation;
- review assumptions;
- approve result.

## Skill domains

Skills represent accumulated understanding and can affect UI assistance, NPC collaboration, or efficiency without replacing player reasoning.

Suggested skill domains:

- Process Analysis
- Human Factors
- Operations
- Measurement & Statistics
- Mechanical Automation
- Electrical/Sensing
- Software Modeling
- Programming
- Data & Persistence
- Integration & Contracts
- Reliability
- Security & Authorization
- Distributed Systems
- Architecture
- Testing & Validation
- Debugging & Observability
- Leadership & Delegation
- Economics

## Discovery-based unlocks

Many concepts unlock by encountering them.

Example:

1. Player creates two different routing policies.
2. Third facility needs another variation.
3. UI offers ability to treat routing as interchangeable policy.
4. Player uses it successfully.
5. Codex unlocks **Strategy** pattern.

## Pattern codex

The codex records:

- common name;
- problem it solves;
- structure;
- tradeoffs;
- where the player has already used it;
- related patterns;
- code representation after programming is unlocked.

## Tool progression

Early tools:

- eyes;
- stopwatch;
- notebook;
- simple layout editor.

Midgame:

- process diagrammer;
- metrics dashboard;
- automation blocks;
- state editor;
- contract inspector;
- scenario runner.

Late:

- trace viewer;
- code editor;
- profiler;
- architecture map;
- organizational ownership map;
- simulation scripting;
- generated testing.

## Mastery

Mastery should mean the player can safely delegate and automate enormous amounts of activity while still tracing why the system exists, what it assumes, what must remain true, and how to challenge it.

## Implemented first-hours progression

The dish-station arc currently awards progression only for observable outcomes:

| Quest | XP | Capability |
|---|---:|---|
| Clock In | 100 | State lens |
| Where Did the Glasses Go? | 200 | Layout tools |
| Dinner Rush | 300 | Knowledge lens |
| The New Hire | 300 | Exception notes |
| The Rare Tray | 400 | Automation workbench |
| It Said It Was Ready | 500 | Runtime trace |
| Prove the Fix | 700 | Ownership map |
| Own the Shift | 900 | Shift scorecard |

Career levels occur at 0, 100, 300, 600, 1,000, 1,500, and 3,000 XP. Levels are communicative milestones; they do not make dish actions faster or increase machine capacity. Capability rewards arrive after the player has experienced the problem that makes the tool meaningful.

Every first-hours quest authors an unlock rationale alongside its outcome and discovery. On completion, the client presents a single causal receipt: completed outcome, XP gained and total, level only when the threshold was actually crossed, unlocked capability, and the authored reason it is useful now. This prevents a generic level-up overlay from hiding the more important capability relationship, and it keeps quests that grant a capability without crossing a level threshold semantically honest.

The level-7 Shift Scorecard is a functional capability rather than a badge. At the reliability-pass tick, the simulation freezes the report's layout, worker, automation, service, and trial evidence; later sandbox experimentation cannot rewrite that history. `K` opens a debrief assembled from this authoritative report snapshot plus progression. It summarizes retained evidence and asks the player to explain the constraint, failed assumptions, and strength of the regression/live-window proof without prescribing those answers during play.

Each quest records its active simulation start, completion, and elapsed ticks. Pauses, briefing time, and launch-menu time do not inflate this value because the authoritative clock does not advance there. Career active time freezes when the final quest completes, so post-shift sandbox experimentation and debrief review do not rewrite the episode's pacing evidence. The journal and Shift Scorecard expose a minutes/seconds projection for playtest pacing, while headless output retains exact ticks. These measurements describe active scenario time, not human attention or comprehension; representative playtests remain necessary before claiming a target number of hours.
