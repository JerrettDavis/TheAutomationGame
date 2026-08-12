# Decision Log and Open Spikes

> Decisions made now to prevent future sessions from repeatedly spending tokens reopening settled design questions. Spikes remain bounded where evidence is still required.

## Decision status

- **SET:** future sessions should follow unless new evidence materially invalidates it.
- **PROVISIONAL:** direction chosen, implementation proof still required.
- **OPEN:** execute a bounded spike before committing.

---

## D001 — Embodied play first, managerial abstraction later

**Status:** SET

The player begins as an embodied worker/improver. Higher-level organizational views unlock with responsibility and scale.

Reason: systems thinking is grounded in lived queues, handoffs, people, machines, and failures before abstraction.

---

## D002 — WASD movement + click-to-move, mouse-first camera

**Status:** SET

Default production control direction:

- WASD direct movement;
- left-click selection/click-to-move;
- E interact/work;
- F inspect;
- mouse pan/zoom;
- keyboard camera alternatives allowed but not on movement keys.

Prototype scenario hotkeys move to developer mode.

---

## D003 — YAML authored content, JSON saves/replays

**Status:** SET

YAML compiles into validated immutable runtime definitions. Simulation does not parse YAML directly.

---

## D004 — Deterministic bounded procedural generation

**Status:** SET

Procedural systems generate parameterized facilities/content variations from explicit template versions + parameters + seeds. The project is not pursuing infinite random-world generation as a core requirement.

---

## D005 — True 3D incrementally replaces the 2D isometric greybox

**Status:** PROVISIONAL

The intended production world remains stylized 3D/isometric. Replace one room/equipment/character slice through a presentation catalog before committing to a full conversion pipeline.

Blocked by SPK-UI? No. Proof is SPK-3D-001 below.

---

## D006 — Small deterministic automation IR before general scripting

**Status:** SET

The first player automation editor targets a safe, traceable rule algebra. Programming later maps to the same capability surface.

---

## D007 — Editors pause by default

**Status:** SET

Process/build/automation editors pause or freeze operational time in early play. Specific later scenarios may intentionally require live changes.

---

## D008 — Lightweight economy, not a life simulator

**Status:** SET

Model operational tradeoffs needed for automation decisions: labor, equipment/software/vendor cost, downtime, waste, throughput/value, maintenance/training. Housing, hunger, personal finance life-sim systems are not required for the core campaign.

---

## D009 — GoF 23 on main campaign; broader PatternKit mostly optional

**Status:** SET

All 23 classic GoF patterns receive main-story exposure. Other PatternKit patterns appear through side quests, incidents, specializations, late architecture/software content, and emergent transfer.

---

## D010 — PatternKit integration is metadata/data, not simulation coupling

**Status:** SET

PatternKit may export a versioned catalog. The game overlays story/evidence/mastery and can show PatternKit code later. Core simulation should not depend on PatternKit runtime merely for curriculum integration.

---

## D011 — Multiplayer deferred

**Status:** SET

Do not constrain current simulation/content architecture around multiplayer. Revisit only after the single-player campaign and organization-scale model are stable.

---

## D012 — Custom content designer/editor deferred

**Status:** SET

Author two industries through YAML + focused in-game editors first. Build bespoke content-authoring UI only from observed authoring friction.

---

# Open bounded spikes

## SPK-UI-001 — Production UI approach

**Status:** OPEN

**Question:** Which UI implementation approach best supports current Stride version, scaling, text/layout, modal/focus input, accessibility, controller future, and iteration speed?

**Method:** Rebuild **one existing screen only**, preferably Journal or Settings, with the candidate approach while retaining the current version for comparison.

**Compare:**

- current custom procedural UI;
- Stride-native UI/tooling available in the pinned repo version;
- minimal custom retained wrapper only if native blockers are demonstrated.

**Evidence:** LOC/complexity, layout quality, scaling, input/focus behavior, performance, screenshot/recording, developer iteration notes.

**Stop:** choose approach/seam. Do not migrate all screens.

---

## SPK-3D-001 — Authored 3D asset + picking pipeline

**Status:** OPEN

**Question:** What exact Stride import/runtime path should production world assets use?

**Proof slice:** one washer or equivalent workstation with model, material, transform, selection/picking, presentation-state mapping, and fallback.

**Evidence:** source/license, import settings, coordinate/scale convention, material behavior, picking result, performance notes.

**Stop:** document pipeline and unblock S009/S010.

---

## SPK-PATH-001 — Deterministic facility routing

**Status:** OPEN only when S011 needs it

**Question:** Does simple deterministic grid A* satisfy authored 3D room navigation/performance, or is a more complex navigation representation required?

**Proof:** representative room + blocked workstations + 20/100 actors depending current scale; fixed endpoints produce stable paths.

**Stop:** select simplest sufficient topology/path method.

---

## SPK-LABEL-001 — World labels after 3D conversion

**Status:** DEFERRED

**Question:** Which state belongs in-world, HUD, and lenses at near/mid/far zoom after authored assets provide stronger silhouettes?

Do not answer using the greybox alone. Run after S010/S012.

---

## SPK-CHAR-001 — Character source/rig

**Status:** OPEN before S012

**Question:** Which deliberately licensed character/animation source or internal pipeline yields a shared stylized rig with acceptable commercial terms and Stride import behavior?

**Proof:** one player + worker variant using idle/walk/work on the same skeleton.

**Stop:** choose alpha production path and record provenance requirements.

---

## SPK-CODE-001 — Player programming sandbox

**Status:** DEFERRED until Software/Platform chapter planning

Questions eventually include:

- restricted C# compilation vs DSL vs external process;
- deterministic capability boundary;
- timeout/resource limits;
- save/replay semantics;
- mod/security implications.

Do not solve this while building the rule editor. The automation IR is the earlier capability boundary.

---

# Decision update format

When a spike closes:

```text
## D### — Decision title
Status: SET
Date: YYYY-MM-DD
Evidence: SPK-...

Decision:
...

Why:
...

Consequences:
...

Revisit when:
...
```

Leave rejected alternatives summarized briefly so later agents do not repeat the same investigation without new evidence.
