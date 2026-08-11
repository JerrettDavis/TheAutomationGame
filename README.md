# The Automation Game

> Working design repository for a systems-thinking, process-discovery, automation, architecture, and software-development simulation game.

## Development status

This is a pre-alpha vertical slice under active development. The deterministic headless simulation, Windows Stride greybox, first-shift onboarding and quest arc, initial progression, save/resume, and automated validation paths are runnable; production art, distribution packaging, and human readiness validation are not complete.

See [Contributing](CONTRIBUTING.md), [Security Policy](SECURITY.md), and [Third-Party Notices](THIRD_PARTY_NOTICES.md) before redistributing or contributing assets.

## Premise

The player begins with little authority and little understanding. They get a job, perform real work, observe how the organization actually functions, improve processes, document knowledge, delegate work, mechanize it, automate it, and eventually design systems and organizations capable of operating at enormous scale.

The game is not about maximizing an "automation percentage." The player is rewarded for building **reliable capability**: outcomes that remain useful when people behave unexpectedly, machines fail, vendors change, networks become unreliable, policies conflict, and the player's own assumptions prove incomplete.

The central educational progression is:

```text
Do the work
  -> understand the work
    -> describe the work
      -> improve the work
        -> delegate the work
          -> automate the work
            -> validate the automation
              -> compose systems
                -> design organizations
                  -> decide what should be done
```

Programming arrives late. By the time text code appears, the player already understands state, inputs, decisions, effects, interfaces, contracts, events, processes, failures, and evidence because they have been manipulating those concepts visually for hours.

## Technical baseline

The initial client will use **Stride 4.3**, **C# 14**, and **.NET 10**. The game simulation is engine-independent and must run headlessly without Stride. Stride is initially responsible for presentation, input, audio, asset integration, scene authoring, and client tooling, not for ownership of simulation truth.

See [ADR-0001](docs/adr/0001-use-stride.md) and [Architecture](docs/06_ARCHITECTURE.md).

## Start here

1. [Product Vision](docs/01_PRODUCT_VISION.md)
2. [Design Pillars](docs/02_DESIGN_PILLARS.md)
3. [Core Game Loop](docs/03_CORE_GAME_LOOP.md)
4. [Simulation Ontology](docs/04_SIMULATION_ONTOLOGY.md)
5. [Gameplay Systems](docs/05_GAMEPLAY_SYSTEMS.md)
6. [Architecture](docs/06_ARCHITECTURE.md)
7. [Stride Client Plan](docs/07_STRIDE_CLIENT.md)
8. [Stories, Quests, and Scenarios](docs/09_STORY_QUEST_SCENARIO_SYSTEM.md)
9. [Progression, Skills, and Abilities](docs/10_ROLES_ABILITIES_SKILLS_PROGRESSION.md)
10. [Implementation Roadmap](docs/21_IMPLEMENTATION_ROADMAP.md)
11. [Initial Backlog](docs/22_EPICS_STORIES_AND_TASKS.md)

## Repository documentation map

| Document | Purpose |
|---|---|
| `01_PRODUCT_VISION.md` | Product thesis, audience, fantasy, outcomes |
| `02_DESIGN_PILLARS.md` | Non-negotiable design rules |
| `03_CORE_GAME_LOOP.md` | Minute-to-minute and campaign loops |
| `04_SIMULATION_ONTOLOGY.md` | Primitive nouns/verbs from which worlds are built |
| `05_GAMEPLAY_SYSTEMS.md` | Economy, work, process, automation, reliability systems |
| `06_ARCHITECTURE.md` | Engine-independent application architecture |
| `07_STRIDE_CLIENT.md` | Stride-specific presentation/client integration |
| `08_WORLD_CAMERA_PRESENTATION.md` | 3D orthographic-first visual approach |
| `09_STORY_QUEST_SCENARIO_SYSTEM.md` | Narrative and educational content model |
| `10_ROLES_ABILITIES_SKILLS_PROGRESSION.md` | Career and capability progression |
| `11_INDUSTRIES_AND_CAMPAIGN.md` | Industry progression and scenario themes |
| `12_AUTOMATION_OUTSOURCING_OWNERSHIP.md` | Automation debt, delegation, human ownership |
| `13_UI_UX_SYSTEM_LENSES.md` | Reality/process/state/architecture/runtime/code lenses |
| `14_CONTENT_AUTHORING.md` | Data-driven authoring workflow |
| `15_ASSET_PIPELINE.md` | Art/audio/model sourcing, creation, generation, licensing |
| `16_DATA_AND_SCHEMAS.md` | Save/content/runtime data conventions |
| `17_SAVE_REPLAY_DETERMINISM.md` | Snapshots, commands, replay, seeded randomness |
| `18_AI_NPC_ORGANIZATIONS.md` | NPC behavior and organizational simulation |
| `19_MODDING_AND_SCRIPTING.md` | Future extensibility and player code |
| `20_TESTING_VALIDATION_PERFORMANCE.md` | Quality strategy and performance budgets |
| `21_IMPLEMENTATION_ROADMAP.md` | Phased development plan |
| `22_EPICS_STORIES_AND_TASKS.md` | Initial implementation backlog |
| `23_RISK_REGISTER.md` | Technical/product/design risks |
| `24_DEFINITION_OF_DONE.md` | Completion gates for systems and content |
| `25_GLOSSARY.md` | Shared project language |

## Working repository shape

```text
TheAutomationGame/
  src/
    Automation.Domain/
    Automation.Simulation/
    Automation.Content/
    Automation.Persistence/
    Automation.Headless/
    Automation.Client.Stride/
    Automation.Tools/
  tests/
    Automation.Domain.Tests/
    Automation.Simulation.Tests/
    Automation.Content.Tests/
    Automation.Integration.Tests/
    Automation.Performance.Tests/
  content/
    industries/
    jobs/
    scenarios/
    quests/
    processes/
    incidents/
    skills/
    abilities/
    assets/
  assets-src/
    models/
    textures/
    audio/
    ui/
    concept/
  docs/
  tools/
```

## First playable target

The first vertical slice is intentionally mundane: **a small restaurant dish station**. The player begins by manually moving dirty dishes through scrape, sort, wash, dry, and return steps. Increasing volume exposes queues, bottlenecks, missing information, machine constraints, failure handling, worker knowledge, measurement, and the first opportunities for mechanization and automation.

The vertical slice is successful when a player can:

- perform the process manually;
- observe and diagram it;
- improve layout and sequence;
- introduce a machine with imperfect behavior;
- define a process explicitly;
- automate one bounded decision;
- experience a failure caused by an incomplete assumption;
- inspect why the failure happened;
- refine the model and validate the improved system;
- view the same system through reality, process, state, architecture, and runtime lenses.

## Run the current greybox

The first runnable slice moves plates and glasses through an authoritative, deterministic dish-station simulation. The Stride client is deliberately code-only while scene assets are still placeholders.

The spatial greybox projects the authoritative process queues onto a selectable isometric sandbox floor. Workstations, dish stacks, service supply, the player, and the delegated worker are presentation objects derived from snapshots rather than owners of gameplay state. With the process lens enabled, the floor adds flow traces, accumulated item-ticks (queue pressure), oldest current item age, completed average residence time, and the current pressure leader; these are simulation metrics, not client-side estimates.

The sandbox floor is editable. Placement mode proposes engine-neutral fixture/cell commands, rejects collisions and unsafe washer relocation, supports command-based undo and preset reset, and immediately exposes the resulting handoff route. Compact custom routes improve delegated action frequency; inefficient arrangements lose that advantage. Player location and walking distance are likewise authoritative and replayable, while movement animation remains a client projection.

The client renders through a centered 1024×600 virtual canvas with automatic scaling and letterboxing. A resizable near-fullscreen run on the 4K development display is part of the native UI smoke test, so text, picking coordinates, and analytical lenses share one resolution transform.

The Windows client launches borderless at the display's native fullscreen resolution. Gameplay remains visible as the continuous background; the objective, service health, selected action, notifications, and earned build tools are translucent HUD overlays. State, knowledge, automation, runtime, responsibility, handbook, and benchmark views open as consistent dimmed informational modals. Consequence-bypassing sandbox tools remain locked until the first shift is complete. Pass `--windowed` when launching the Windows project to use a resizable development window.

Career progress autosaves atomically to the user's local application-data directory. A later launch offers Continue or a confirmation-protected New Career; the previous checkpoint remains intact until the replacement intro is completed.

The client begins with a five-page first-shift briefing and replayable choices for guided/contextual/minimal assistance, reduced motion, and high contrast. The first-hours journal then tracks eight outcome quests and their active-simulation duration across one manual restock, dinner-rush observation, bottleneck diagnosis, a measured layout improvement, explicit delegation, a rare exception, bounded washer automation, and a live reliability window. Each quest has a navigable detail page; situation and observable outcome are available immediately, while the causal discovery is recorded only after the outcome is complete. Quest outcomes grant XP, seven initial career levels, and observation/action capabilities rather than throughput bonuses. A unified progression receipt keeps outcome, XP, level, unlocked capability, and its authored “why now” explanation visible together; same-level rewards do not masquerade as level-ups. Completing the episode leaves the sandbox running for experimentation or post-shift tools.

After identifying the first constraint, the player compares the 22-step baseline route with a U-shaped cell. The same dish states then require 10 handling steps, and the shorter route gives delegated work more action opportunities; it does not erase service demand or washer capacity.

The episode then introduces a new hire. The player can transfer only the visible happy-path flow, observe the resulting plate-first behavior during glass demand, and explicitly add the missing rush-priority knowledge. Delegation is validated when service consumes a glass produced by the worker. An uncommon tray then exposes a second omitted fact: without its orientation knowledge the worker creates rework; after that fact is documented, the same tray completes normally.

Finally, the player enables a controller that starts a present rack whenever the washer reports ready. After successful cycles, a sticky-ready signal causes an unsafe start request while the previous rack is physically still in the machine. The controller halts, the player inspects the reported/physical-state divergence, and a corroborated-ready policy prevents the same unsafe request during validation.

```powershell
dotnet build TheAutomationGame.sln -c Release
dotnet test TheAutomationGame.sln -c Release --no-build
dotnet run --project src/Automation.Headless -c Release -- --ticks 250 --seed 42
dotnet run --project src/Automation.Headless -c Release -- --benchmark-actors 100000 --benchmark-ticks 100
dotnet run --project src/Automation.Headless -c Release -- --sandbox-demo --ticks 20
dotnet run --project src/Automation.Client.Stride.Windows -c Release
dotnet run --project src/Automation.Client.Stride.Windows -c Release -- --windowed
```

The headless runner accepts typed scenario overrides. Run `dotnet run --project src/Automation.Headless -- --help` for the full matrix. For example:

```powershell
dotnet run --project src/Automation.Headless -c Release -- --empty --ticks 120 `
  --initial-plates 12 --initial-glasses 4 --arrival-interval 20 --glass-every 2 `
  --rack-capacity 3 --washer-cycle 12 --worker-enabled --knowledge full `
  --worker-interval 4 --flow-worker-interval 2 --automation safe --layout cell `
  --demand-kind Glass --demand-interval 10 --rush --sticky-after 0 --fault-permille 25
```

Client controls:

- onboarding, guidance and comfort cards, Continue/New Career, journal rows and details, handbook close, and the Shift Scorecard all support direct mouse selection as well as their displayed keyboard controls;
- `Enter` advances the intro briefing; on its guidance page `Q` / `E` changes the selected mode;
- `F12` opens the progression-aware Shift Handbook. It shows core interaction, the current opportunity, and only capabilities already earned; it does not reveal hidden quest discoveries;
- `J` opens the first-hours quest journal; `Q` / `E` or `Up` / `Down` selects a quest, `Enter` / `Space` opens its detail page, and `Esc` returns or closes it;
- `W` starts the final live reliability window after the regression proof and station preparation;
- `K` opens the level-7 first-shift report after **Own the Shift** is complete;
- click a workstation or use `Q` / `E` to select it, then press `Space` to perform its contextual work;
- arrow keys pan the isometric floor, `Z` / `X` zoom, and `C` recenters the camera;
- hover a fixture to preview `MOVE`, `WORK`, or `INSPECT`; left-click a workstation to approach it and click again to perform work, left-click service to inspect supply, or click a floor tile to move there (`Space` remains the keyboard contextual action);
- `M` opens placement mode; use `Q` / `E` to choose a fixture, arrows or a floor click to move its preview, `Enter` to place it, `Backspace` to undo, and `H` to restore the linear preset;
- `1` scrape, `2` rack, `3` start washer, `4` unload, `5` dry/restock;
- `Tab` switches between plates and glasses;
- `L` toggles the process lens and `R` toggles dinner-rush demand;
- `B` confirms the selected workstation as the current bottleneck hypothesis when the tutorial asks;
- `G` arranges the U-shaped flow cell when the tutorial asks;
- `N` toggles the new hire, `T` transfers the happy-path flow, and `Y` adds the rush glass-priority knowledge;
- `U` documents the uncommon-tray handling knowledge when that exception is discovered;
- `A` enables reported-ready automatic start, `I` inspects an automation incident, and `S` installs the physical-readiness interlock;
- `D` toggles the incident trace and `P` replays the captured incident against the currently selected automation policy;
- `V` cycles the currently unlocked reality, process, state, knowledge, automation, runtime, and responsibility lenses;
- after the first shift is complete, `F1` opens post-shift sandbox tools: `F2` injects dirty dishes, `F3` provisions clean supply, `F4` resets, `F5` pauses, `F6` steps, `F7` injects sticky-ready, and `F8` toggles layout. Development runs can opt in earlier with `AUTOMATION_DEVELOPER_TOOLS=1`;
- in god mode, `F9` renders a batched 10k representative subset from the 100k-actor benchmark, `F10` creates a deterministic quick-save, and `F11` restores it;
- `Esc` closes an open journal; otherwise it exits.

Native UI smoke test (Windows):

```powershell
dotnet build TheAutomationGame.sln -c Release
.\tools\ui-smoke.ps1 -AllowDesktopInput
```

The smoke driver takes exclusive control of the shared OS cursor and opens/resizes real windows; run it only while the desktop is idle. The required `-AllowDesktopInput` switch prevents accidental takeover. It first launches an ordinary player process and verifies developer tools are locked, then launches the Stride executable and combines semantic controls with real OS pointer movement and clicks. Pointer paths cover every intro page, guidance and comfort selection, handbook, quest rows and details, scorecard, Continue, and New Career confirmation in addition to sandbox movement and work. It completes the episode and live reliability window, opens every lens, validates placement, checks save–mutate–restore while paused, renders the 100k benchmark subset, captures the major HUD and modal layers through DPI-safe settled-frame capture, validates a 4K-scale viewport, and resumes the same level-7 career in a second process. Use `-KeepOpen` to retain the resumed window for review.

Fresh-career human playtest (Windows):

```powershell
.\tools\playtest-first-hours.ps1 -PlayerId novice-01
```

This launches the normal visible client with an isolated save under `artifacts/playtests/`. When the final quest completes, the client atomically emits objective onboarding, progression, quest, duration, per-stage handbook-use, reliability-window, and frozen scorecard evidence. After the window closes, the launcher records the facilitator's vocabulary, intervention, causal-answer, and blocker judgments alongside the free-form debrief. Add `-Windowed` only when the test setup requires a resizable window, or `-NonInteractive` when another study system owns facilitator observations.

After multiple sessions, summarize the formative gate with:

```powershell
.\tools\summarize-first-hours-playtests.ps1
```

The summary reports completion, novice representation, action-directed assistance, causal answers, repeated blockers, wall/active duration, reliability attempts, and handbook use. Duration remains a study judgment because the intended first-hours wall-clock envelope has not yet been fixed to a numeric threshold.

## Project rule

> The engine renders the world. The simulation owns the world.

## License

Project-owned code and documentation are available under the [MIT License](LICENSE). Third-party assets retain the terms documented in [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
