# Fixed Session Backlog

> Ordered implementation sessions. Each session has one demonstrable outcome and a deliberate stop point.

Mark status using `TODO`, `ACTIVE`, `BLOCKED`, or `DONE`. Add proof beneath completed sessions rather than deleting them.

## N1 — Comfortable Interaction

### S001 — Direct Player Navigation — DONE

**Value:** Player moves naturally with WASD.

**Deliver:** semantic movement actions; WASD; authoritative movement commands; retain click-to-move; remove `W` scenario shortcut from production path.

**Proof:** release tests + launch + W/A/S/D movement + click move + no quest side effect from W.

**Out:** pathfinding rewrite, gamepad, art.

**Evidence (2026-08-11):**

- Added allocation-free WASD translation to deterministic camera-relative movement intents and neighboring floor destinations.
- Direct movement issues the existing authoritative `MovePlayerCommand`; click-to-move remains on that same simulation command path.
- Removed production A/S/D/W scenario-action bindings. The semantic scenario controls remain available to explicit developer-session drivers.
- Added focused coverage for all four mappings, opposing/diagonal resolution, W having no shift-trial side effect, and authoritative boundary rejection.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 59/59 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native UI smoke skipped: it takes exclusive OS cursor control and opens/resizes visible windows, which is unsafe under the session workstation constraints.
- Human playable acceptance: **PENDING — user-run** using the supplied S001 acceptance steps.
- Follow-up: replace developer-session-only scenario actions displaced from WASD with player-facing contextual interaction in an appropriately scoped future session; do not restore production movement-key shortcuts.

### S002 — Context Interaction — DONE

**Value:** Player can approach a workstation and use one obvious interaction key.

**Deliver:** `E` interact/work, `F` inspect, selected/nearest interaction resolution, visible prompt, disabled reason.

**Proof:** manually walk to washer/rack/service interaction points and work/inspect without scenario hotkeys.

**Out:** full radial menu, controller support.

**Evidence (2026-08-11):**

- Added narrow semantic E/F input translation for interact/work and inspect in Gameplay context.
- Added deterministic selected-in-range then nearest-fixture resolution with stable tie-breaking.
- Added replay-serializable `InteractWithDishStationFixtureCommand` and `InspectDishStationFixtureCommand`; authoritative work rejects out-of-range requests without moving or mutating the player/world.
- Added derived interaction state for visible target/action prompts and concrete disabled reasons (`MOVE CLOSER`, `NO DISH READY`, rack/washer states, or inspection-only service).
- Workstation and service clicks now approach first, then use the same authoritative interaction/inspection commands when in range.
- Focused S002 tests — passed, 12/12 across input, resolution, range, mutation, inspection, service, disabled-reason, and replay cases.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 71/71 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native UI smoke skipped: it takes exclusive OS cursor control and opens/resizes visible windows, which is unsafe under the active workstation constraints. Its service interaction expectation was updated but not executed.
- Human playable acceptance: **PENDING — user-run** using the supplied S002 acceptance steps.

### S003 — Mouse Camera Controls — DONE

**Value:** Camera control no longer competes with movement.

**Deliver:** middle/right-drag pan according to chosen convention, wheel zoom, recenter, clamp behavior, keyboard fallback.

**Proof:** traverse room while independently panning/zooming; movement remains intact.

**Evidence (2026-08-11):**

- Chose the documented preferred middle-drag convention, preserving right-click as an existing movement surface.
- Middle-drag applies device deltas in virtual-canvas units; wheel direction applies fixed zoom steps; `C` and `Home` recenter.
- Camera pan clamps to X `[-220,220]` and Y `[-120,120]`; zoom clamps to `[0.7,1.4]`. Arrow pan and `Z`/`X` zoom remain keyboard fallbacks.
- Camera input is presentation-only and emits no simulation command; authoritative WASD and contextual interaction paths remain separate.
- Focused S003 camera tests — passed, 5/5 across scaled drag, pan clamps, wheel direction/zoom clamps, reset, and simulation-command isolation.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 76/76 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native UI smoke skipped: it calls `SetCursorPos`, synthesizes clicks, and manipulates visible windows, which is unsafe under the active workstation constraints.
- Human playable acceptance: **PENDING — user-run** using the supplied S003 acceptance steps.

### S004 — Input Action Map — DONE

**Value:** Gameplay code consumes logical actions rather than physical keys.

**Deliver:** action identifiers, keyboard bindings, developer-action separation, persistence-ready model.

**Proof:** remap at least one action in test/config and demonstrate gameplay code unchanged.

**Out:** production rebinding screen.

**Evidence (2026-08-11):**

- Added stable logical action/context and engine-neutral keyboard-key identifiers plus a versioned `InputBindingProfile` with string enum IDs for persistence.
- Migrated the existing Stride client to query logical actions for every menu, gameplay, journal, placement, camera, and developer keyboard path. `Stride.Input.Keys` conversion is isolated to one adapter.
- Developer actions are explicitly classified and cannot match unless the existing developer-tools availability policy is true.
- Visible keyboard legends, capability hints, contextual prompts, and guided quest prompts derive their labels from the active binding profile.
- Binding collections and display labels are built once; per-frame action queries use precomputed arrays/strings without collection allocation.
- Focused S004 tests — passed, 5/5: complete defaults/adapter coverage, real movement remap + changed hint with unchanged logical result, developer gating, versioned JSON round-trip, and invalid-profile rejection.
- Source audit — no direct `Stride.Input.Keys.*` reference outside `StrideKeyboardAdapter`; no hard-coded key-shaped copy remains in client UI strings.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 81/81 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native UI smoke skipped because it violates active cursor/focus/window safety constraints.
- Human playable acceptance: **PENDING — user-run** using the supplied S004 regression steps. A production rebinding screen remains intentionally out of scope.

### S005 — Client Screen and Modal Router — DONE

**Value:** Journal, briefing, overlays, and future editors stop accumulating ad-hoc client state branches.

**Deliver:** extract concrete screen/modal routing from existing screens; migrate at least journal + briefing.

**Proof:** both flows behave identically; no unused generic application framework.

**Evidence (2026-08-11):**

- Added one concrete `ClientScreenRouter` for the existing start menu, first-shift briefing, gameplay screen, and current modal set; it is a fixed route model rather than a generic application framework.
- Migrated the briefing lifecycle: new installs/new careers route to briefing, completed briefings route to gameplay, and resumed careers choose briefing or gameplay from the authoritative saved onboarding state.
- Migrated journal open/detail/back/close navigation and the existing mutually-exclusive help and shift-report overlays; the client no longer stores independent visibility booleans for these routes.
- Focused S005 application-flow tests — passed, 8/8 across new/saved entry, saved briefing state, journal/detail navigation, overlay exclusivity, invalid-context isolation, and new-career confirmation.
- Full integration project — passed, 46/46 tests.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 89/89 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native visible E2E/UI smoke skipped: the current native bootstrap shows the Stride window before positioning, targets the primary rather than actual leftmost display, and later calls `SetWindowPos` without `SWP_NOACTIVATE`; the existing UI smoke also moves the real cursor and synthesizes clicks. No unsafe desktop experiment was performed.
- Human playable acceptance: **PENDING — user-run** using the supplied S005 briefing/journal regression steps.
- Follow-up recorded for S007: native window/display settings should stop assuming the primary display and expose a launch path that can establish placement before first show; nonactivating test launch remains a tooling concern, not part of the gameplay router.

### S006 — Interaction HUD Pass — DONE

**Value:** Player can infer what can be done without reading a keyboard legend.

**Deliver:** context prompt, target name/state, disabled reason, compact goal hint, consistent notification hierarchy.

**Proof:** first manual task can be completed with visible UI alone.

**Evidence (2026-08-11):**

- Reworked the compact gameplay footer into an explicit target/state, action, disabled-feedback, and notification hierarchy while retaining journal/help/report access.
- Target and state copy derives from `DishStationInteractionState`: selected dish queue count, range, work action, inspection availability, and block reason remain projections of authoritative simulation state.
- Available actions use the active logical binding profile. Out-of-range targets show WASD movement instead of falsely offering an interaction, service shows inspection-only, and blocked work names the concrete missing/occupied condition.
- Guided goal hints moved into the focused HUD presenter and continue to derive key labels from logical bindings rather than physical-key literals.
- Existing world notifications now receive presentation-only `AMBIENT`, `OPERATIONAL`, `IMPORTANT`, or `CRITICAL` labels and distinct colors; no new simulation notification ontology or gameplay behavior was introduced.
- Focused S006 HUD tests — passed, 9/9: real first-task projection, missing-dish reason, range guidance, service inspection, remapped goal hint, and four representative notification priorities.
- Full integration project — passed, 55/55 tests.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 98/98 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native visible E2E/UI smoke skipped because the current launch/smoke paths cannot guarantee no activation, leftmost placement before first show, or zero cursor injection. No GUI was launched.
- Human first-task HUD acceptance: **PENDING — user-run** using the supplied S006 steps.

### S007 — Settings Foundation — DONE

**Value:** Core play settings persist.

**Deliver:** volume placeholders/controls as supported, UI scale, camera sensitivity, fullscreen/windowed, input binding persistence seam.

**Proof:** change supported settings, restart, observe persistence.

**Evidence (2026-08-11):**

- Added versioned, validated `ClientSettings` plus atomic JSON persistence under local application data (or `AUTOMATION_SETTINGS_PATH`), with safe default fallback for missing, malformed, or incompatible files.
- Added one concrete Settings modal available from the saved-career start menu and gameplay. Keyboard and pointer controls adjust master-volume intent, fitted UI scale, camera sensitivity, and next-launch window mode; changes save immediately and defaults can be restored.
- Master volume is explicitly labeled as a persisted placeholder because the client has no audio output yet. Window-mode changes are explicitly labeled restart-required rather than pretending to mutate the current native window safely.
- UI renders through a separately fitted 75–100% canvas and UI pointer hit testing uses the exact same transform; world projection/picking remain on their existing canvas. Camera drag and wheel response use the persisted 50–200% sensitivity.
- The complete versioned `InputBindingProfile` is stored inside the settings payload and loaded at client startup, establishing persistence without adding the out-of-scope production rebinding screen.
- Windows startup resolves persisted `Windowed`/`BorderlessFullscreen` mode with explicit `--windowed`/`--fullscreen` overrides. Read-only topology inspection found the actual leftmost monitor at work-area `X=-3840`, `3072×1680`; both startup modes now target that work area and post-create placement uses `SWP_NOACTIVATE | SWP_NOZORDER`.
- Non-GUI Windows startup E2E (`--diagnose-startup`) — passed for saved/default borderless and explicit windowed mode; both loaded settings/bindings schema v1 and resolved `leftmost=-3840,0,3072x1680`, then exited before `Game.Run` without creating a window.
- Focused S007 tests — passed, 8/8: full setting+binding restart round trip, corrupt/missing fallback, bounds, invalid schema/value rejection, startup overrides, fitted UI/pointer transform, camera sensitivity, and settings routing.
- Full integration project — passed, 63/63 tests.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 106/106 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native visible E2E/UI smoke skipped: Stride still owns first-show timing, so source changes cannot prove the initial window is leftmost and nonactivating before the callback; existing smoke also injects the real cursor. No GUI was launched.
- Human settings restart acceptance: **PENDING — user-run** using the supplied S007 steps.
- Follow-ups recorded for the human-readiness/QA gate: fixed-layout panels need a reflow pass before supporting UI scale above 100%, and a future native harness must control placement before first show before unattended visible E2E is workstation-safe.

---

## N2 — Presentation Seam

### S008 — Real Asset Import Spike — DONE

**Class:** SPIKE

**Question:** What is the smallest reliable Stride pipeline for loading/rendering a real authored 3D prop with correct transforms, material, selection, and fallback?

**Proof:** one deliberately licensed washer/workstation proxy renders in the existing room; record import constraints and decision.

**Out:** full room conversion.

**Evidence (2026-08-11):**

- Imported Kenney Furniture Kit 2.0's washer GLB under CC0 with local license, source URL, shipping status, and SHA-256 provenance. Stride's canonical asset compiler registered it as `Imported/KenneyFurnitureKit/Washer` and produced the Windows content bundle.
- The existing SpriteBatch room now renders Kenney's matching southwest isometric projection of the same authored model, floor-anchored to the authoritative washer placement. Selection and process-bottleneck state remain presentation-only tint/overlay treatments.
- Missing/unloadable projection data retains the existing primitive washer renderer. No simulation identity, command, save shape, collision, or placement semantics changed.
- Decision: use the `.sdpkg` + `.sdm3d` + source-GLB pipeline for authored Stride models. During the current hybrid renderer, use a pack-provided isometric projection for the live room; native `Model` rendering waits for the deliberate scene/compositor work in S009/S010 rather than creating a parallel one-prop 3D room.
- Import constraints: model source paths are relative to the asset document; a root asset is required for bundling; compiler transitive dependencies required centrally pinned non-vulnerable versions. A from-scratch compile reports benign DataContract-alias warnings for engine-neutral referenced assemblies and a default-GameSettings warning; those projects intentionally remain free of Stride processing per the architecture boundary.
- Focused S008 tests — passed, 3/3: deterministic transform/state tint, embedded projection hash, and GLB 2.0 structure/material/mesh/hash plus Stride registration.
- Non-GUI asset startup E2E (`--diagnose-assets`) — passed: projection present, two compiled bundles totaling 2,330,371 bytes, model URL resolved, actual leftmost work area `-3840,0,3072x1680`; exited before `Game.Run` (`gui=not-started`).
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 109/109 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native visible E2E/UI smoke skipped: Stride still owns first-show timing, so pre-show leftmost/nonactivation safety is not established; the existing smoke injects the real cursor. No GUI was launched.
- Human presentation acceptance: **PENDING — user-run** using the supplied S008 steps.

### S009 — Presentation Catalog — DONE

**Value:** Presentation can change without simulation/content identity changing.

**Deliver:** stable presentation IDs, resolver, fallback entries, washer + worker + item migrated.

**Proof:** swap an asset mapping without changing simulation content or save identity.

**Evidence (2026-08-11):**

- Added client-only stable `PresentationId`, concrete workstation/actor/item definitions, a validated resolver, and typed root fallbacks. No domain, simulation, replay, or persistence type references presentation identity.
- Migrated the authored washer model/projection URL, washer dimensions/colors, new-hire projection color, and plate/glass/tray stack geometry/colors out of renderer literals and into the catalog.
- Missing catalog IDs resolve to their caller-selected typed fallback. An unavailable washer projection explicitly resolves through `presentation.fallback.workstation` and keeps the primitive rendering path.
- Catalog replacement is immutable and setup-time only; render-time resolution is dictionary lookup without per-frame catalog construction.
- Focused S008/S009 tests — passed, 6/6. The swap proof replaces the washer model URL, projection resource, and tint while asserting the authoritative snapshot identity and serialized career JSON remain unchanged and contain no presentation IDs.
- Non-GUI asset/catalog E2E (`--diagnose-assets`) — passed: stable washer, worker, item, and washer-fallback IDs resolved; authored projection and compiled bundles were present; actual leftmost work area was `-3840,0,3072x1680`; exited before `Game.Run` (`gui=not-started`).
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 112/112 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native visible E2E/UI smoke skipped: pre-show leftmost/nonactivation safety remains unproven and the existing smoke injects the real cursor. No GUI was launched.
- Human catalog presentation acceptance: **PENDING — user-run** using the supplied S009 steps.

### S010 — Modular Dish Room Kit — DONE

**Value:** Restaurant becomes an authored 3D place.

**Deliver:** floor, wall, door/opening, counter, washer zone, racks/service opening using reusable modules.

**Proof:** current dish scenario plays in authored room; no simulation changes.

**Evidence (2026-08-11):**

- Added a deterministic client-only `DishRoomModulePlan` with 133 placed modules across nine reusable kinds: 104 floor tiles, two wall runs, a three-piece doorway frame/opening, two counters, washer zone plus washer model, dirty/clean racks, and service pass.
- Added a native Stride `SceneSystem`/default forward compositor layer with reusable procedural cube models/materials, ambient and directional lighting, and the catalog-loaded authored washer model. Missing model data uses a native primitive washer fallback; native scene setup failure retains the complete SpriteBatch room fallback and exposes `room=fallback:<type>` in the window title.
- Fixture presentation modules synchronize from existing `DishStationPlacements`; linear, U-cell, and custom placement commands remain authoritative. Static room-shell modules do not move when gameplay layout changes.
- The orthographic native camera is mathematically aligned with the existing floor projection at default, pan, and zoom settings. Native modules, pointer hit-testing, process overlays, labels, and SpriteBatch player/worker projections therefore share the same authoritative floor-cell anchors.
- No obstacle, collision, walkability, route-generation, simulation, replay, or save-schema behavior changed; S011 remains separate.
- Focused S008–S010 tests — passed, 14/14: required module coverage/unique IDs/valid dimensions, placement synchronization, static shell stability, save invariance, native projection alignment under default/pan/zoom, catalog swap/fallback, and authored asset integrity.
- Non-GUI room/catalog E2E (`--diagnose-assets`) — passed: 133 modules/nine kinds, stable presentation IDs, projection and bundles present, actual leftmost work area `-3840,0,3072x1680`; exited before `Game.Run` (`gui=not-started`).
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 120/120 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at tick 250.
- Native visible E2E/UI smoke skipped: current Stride startup cannot prove leftmost, nonactivating placement before first show; existing smoke injects the real cursor. No GUI was launched.
- Human authored-room acceptance: **PENDING — user-run** using the supplied S010 steps; confirm the title reports `room=native` rather than fallback.

### S011 — Walkability and Obstacles — DONE

**Value:** Characters respect world geometry.

**Deliver:** explicit walkable topology/obstacles + deterministic route generation for click move; direct movement respects same constraints.

**Proof:** route around workstation; blocked tile cannot be entered; deterministic path test.

**Evidence (2026-08-11):**

- Added engine-neutral `DishStationTopology` over the authoritative 13×8 floor and fixture placements. All six workstation footprint cells are blocked, each fixture exposes a deterministic adjacent interaction port, and layout edits reject sealed or disconnected interaction topology.
- Direct `MovePlayerCommand` now accepts only a legal neighboring floor step. It rejects fixture footprints, long-distance movement, out-of-bounds cells, and diagonal corner cutting; authoritative player state and movement metrics remain simulation-owned.
- Ground and workstation clicks generate a deterministic breadth-first route with a fixed neighbor order. The client drains that route as the same `MovePlayerCommand` used by WASD, revalidates each step against current placements, and cancels the route when direct input takes over.
- Existing contextual work remains authoritative at the resolved interaction port. Legacy scenario-driving actions retain useful developer/headless behavior by resolving an authoritative route rather than placing the player on blocked geometry.
- Ontology review recorded in `docs/36_GAMEPLAY_INPUT_CAMERA_SANDBOX.md`: topology is a value/constraint over existing cells, fixtures, and placements—not a new simulation entity or generalized navigation platform.
- Focused S011 tests — passed, 10/10: footprint/port topology, deterministic washer detour, corner-cut rejection, sealed-port detection and placement rejection, authoritative movement rejection, legal click commands, fixture-click port routing, deterministic command sequence, and WASD takeover cancellation.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 130/130 tests.
- Non-GUI topology/catalog E2E (`--diagnose-assets`) — passed: six blocked fixture cells, connected ports, deterministic four-step obstacle detour, 133 room modules/nine kinds, compiled assets, actual leftmost work area `-3840,0,3072x1680`; exited before `Game.Run` (`gui=not-started`).
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed deterministically at tick 250 with authoritative U-shaped layout/player state.
- Native visible E2E/UI smoke skipped: current Stride first-show behavior cannot prove nonactivating leftmost placement before display, and the smoke tool injects the physical cursor. No GUI was launched.
- Human walkability/click-route acceptance: **PENDING — user-run** using the supplied S011 steps.

### S012 — Character Presentation Slice — DONE

**Value:** Player and one worker visibly read as people doing work.

**Deliver:** shared rig or temporary production candidate, idle/walk/work/facing, selection state.

**Proof:** player walks and works; NPC transitions visibly; animation never changes simulation truth.

**Evidence (2026-08-11):**

- Added stable player and new-hire actor presentation variants over one renderer-safe procedural humanoid rig. The shared silhouette now has head/face direction, torso, arms, alternating legs, shadow, labels, work reach, and a persistent player selection ring.
- Added a client-only `DishStationCharacterPresenter` with deterministic idle, walk, and work states plus four screen-relative facings. Player movement follows authoritative `PlayerCell`; successful work commands trigger bounded work presentation only after authority accepts them.
- Worker presentation observes authoritative action count/last action, walks between topology interaction ports, faces its new target, and performs a bounded work pose on arrival. These transitions do not add worker simulation position or change AI, commands, replay, or save schemas.
- Reduced-motion mode snaps visual travel and removes cyclic bob/stride while keeping a readable static work pose and selection state.
- Focused S012 tests — passed, 11/11: four isometric facing directions, player walk/selection, reduced-motion snap, player work/idle lifecycle, unchanged serialized world, worker walk/work transitions, deterministic distinct rig poses, and catalog-backed player/worker variants.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 138/138 tests.
- Non-GUI character/catalog E2E (`--diagnose-assets`) — passed: player and worker IDs resolved and live presenter probes reported `characterStates=Idle,Walk,Work`; room/topology/assets and actual leftmost work area `-3840,0,3072x1680` also resolved; exited before `Game.Run` (`gui=not-started`).
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed deterministically at tick 250; character presentation remained absent from simulation/save identity.
- Native visible E2E/UI smoke skipped: current Stride first-show behavior cannot prove nonactivating leftmost placement before display, and the smoke tool injects the physical cursor. No GUI was launched.
- Human character-animation acceptance: **PENDING — user-run** using the supplied S012 steps.

### S013 — Audio Feedback Slice — DONE

**Value:** Core loop can be read with eyes partly off the HUD.

**Deliver:** ambient room loop + work + washer start/complete + blocked/failure + quest success sounds; volume controls if available.

**Proof:** events align with deterministic state; captions/visual equivalents exist for information-bearing cues.

**Evidence (2026-08-11):**

- Added seven project-authored deterministic PCM sources and Stride sound assets: quiet looping dish-room ambience plus distinct work, washer-start, washer-complete, blocked, failure, and quest-success cues. Provenance records no third-party samples and supplies the deterministic regeneration command.
- Added a client-only audio router over accepted/rejected command results, authoritative simulation notifications, worker action counts, and completed quest counts. Repeated snapshots are deduplicated; routing does not add simulation audio events or modify replay/save identity.
- Connected persisted master volume to live per-instance gain; 0% is a true mute. Missing content/device initialization degrades to captions and silence without blocking the game.
- Every cue emits a visible `SOUND • ...` HUD caption. Existing command details, notification hierarchy, washer state, failure feedback, and progression receipts remain the complete visual equivalents.
- Focused S013 audio/settings tests — passed, 10/10: unique required content URLs, gain and mute math, authoritative full-cycle routing, no duplicate sampling, distinct blocked/failure cues, caption coverage, unchanged serialization, and persisted volume bounds/round-trip.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 142/142 tests.
- Non-playing audio/catalog E2E (`--diagnose-assets`) — passed: all seven compiled URLs discovered (`audioAssets=7/7`), routing probes remained non-playing (`audioPlayback=not-started`), and actual leftmost work area was `-3840,0,3072x1680`; exited before `Game.Run` (`gui=not-started`).
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed deterministically at tick 250; audio remained absent from simulation/save identity.
- Native audible/UI smoke skipped: current Stride first-show behavior cannot prove nonactivating leftmost placement before display, and existing smoke injects the physical cursor. No GUI was launched and no sound was played automatically.
- Human audio/mix acceptance: **PENDING — user-run** using the supplied S013 steps.

---

## N3 — Content Platform

### S014 — Content Schema v1 — DONE

**Value:** Authored data has a stable contract.

**Deliver:** YAML schema/model for IDs, industry, facility, items, workstations, processes, scenario, quest, character references; version field.

**Proof:** tiny valid fixture compiles; invalid IDs/references fail clearly.

**Evidence (2026-08-11):**

- Added the strict `schema_version: 1` YAML contract and immutable runtime models for industry, facility, item, workstation, process, scenario, quest, and character definitions. Semantic IDs use required type prefixes and a globally unique namespace.
- Added a controlled YamlDotNet compiler boundary: private raw DTOs, unsupported-version rejection, required fields, malformed/duplicate ID detection, typed unknown/wrong-reference diagnostics, item-state checks, process step/route/cycle validation, and supported quest metric/operator checks.
- Added deterministic normalization plus an eight-kind manifest and canonical SHA-256. Human-authored strings use unambiguous encoding; item-state/process-step authored order remains semantic while unordered references/routes normalize for hashing.
- Added the real `content/fixtures/schema-v1/minimal-restaurant.yaml` proof bundle and `content/SCHEMA_V1.md`. The bundle compiles one of every v1 kind (8 definitions) to hash `8618a874c2e098f5b18028f50a25457a1065ead3271266a9a69c7ea97df73125`.
- Added the non-GUI command `Automation.Headless --compile-content <path>`; valid compilation prints schema version, per-kind counts, definition total, and hash. Invalid compilation writes source/path diagnostics and returns a nonzero exit code.
- Focused S014 content tests — passed, 11/11: complete immutable fixture, stable hash, unsupported version, malformed ID, duplicate global ID, unknown reference, wrong reference type, missing item state, disallowed cycle, unknown metric, malformed YAML location, and unknown-key rejection.
- Existing live first-shift C# definitions were not migrated or altered; S015 remains the dedicated narrative externalization session.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 153/153 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --compile-content content/fixtures/schema-v1/minimal-restaurant.yaml` — passed with eight definitions and the expected hash.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed deterministically at tick 250, proving the new authoring boundary did not change existing simulation behavior.
- Native UI smoke not applicable and not run. No GUI or audio process was launched.
- Human schema inspection: **PENDING — user-run** using the supplied S014 steps.

### S015 — Externalize First-Shift Narrative — DONE

**Value:** Quest copy/order/outcomes can change without modifying simulation source.

**Deliver:** move first-shift quest metadata/steps/rewards/text into content pipeline while preserving behavior.

**Proof:** deterministic/quest tests match reference; content-only text change appears in client.

**Evidence (2026-08-11):**

- Added the production `content/restaurant/first-shift.yaml` bundle. All eight first-shift quests now author stable runtime IDs, display sequence, title, situation, observable outcome, discovery, unlock rationale, visible XP/capability rewards, and ordered guided tutorial steps outside C#.
- Extended schema-v1 quests with an optional complete narrative block. The strict compiler validates positive sequence/rewards, semantic runtime/capability/input-action tokens, nonempty unique steps, and exact `{binding}` placeholder contracts; all narrative fields participate in canonical hashing.
- Added a first-shift adapter that loads the production YAML as an embedded `Automation.Content` resource and validates exact runtime-quest coverage, contiguous sequences, globally unique stage steps, known capabilities, and coherence with authoritative progression rewards.
- Removed guided tutorial sentence ownership from `GameplayHudPresenter`; all 26 `DishTutorialStage` values resolve authored step text, and semantic input-action placeholders resolve through the active `InputBindingProfile`.
- Removed the quest journal's enum-order assumption. Navigation, selection, numbering, HUD, completion receipts, audio titles, and locked-capability messages consume the authored quest catalog/order.
- Focused S015 tests — passed, 5/5: reference quest metadata/order/rewards, exact 26-stage coverage, YAML-only text change reaching the client presenter, remapped logical-binding substitution, and clear reward-drift rejection. The combined content/HUD focus passed 25/25.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --compile-content content/restaurant/first-shift.yaml` — passed with 15 definitions (8 quests) and hash `8f084819984b26f84d5975feefea62feea0a93cba06e4abe5c6a8c129d2fccd5`.
- The S014 minimal fixture remains compatible and retains hash `8618a874c2e098f5b18028f50a25457a1065ead3271266a9a69c7ea97df73125`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 158/158 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged tick-250 reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- Native UI smoke was not run: S015 is fully provable through the content/compiler/client-presentation seam, and launching the client could not satisfy the no-focus guarantee. No GUI or audio process was launched.
- Human content/client inspection: **PENDING — user-run** using the supplied S015 steps.

### S016 — Externalize Dish Scenario — DONE

**Value:** Facility/process/scenario authoring is data driven.

**Deliver:** externalize dish-station authored definitions that do not belong in simulation code.

**Proof:** old reference run equals compiled-content run for a fixed seed.

**Evidence (2026-08-11):**

- Added the complete schema-v1 `dish_station` scenario block to `content/restaurant/first-shift.yaml`: initial dirty/available inventory, arrivals, rack/washer timing, worker cadence, demand, initial rush/worker knowledge, automation policy, sticky-ready pressure, and initial layout.
- Moved the engine-neutral `DishStationScenarioConfiguration` value into `Automation.Domain` and made every field explicit/required. `DishStationWorld` now requires a validated configuration; simulation contains no YAML/compiler dependency and no production first-shift fallback values.
- Extended `ContentCompilerV1` to validate and compile the authored block directly to `DishStationScenarioConfiguration`. Missing counts/flags, nonpositive timing/capacity, invalid permille, and unsupported dish/knowledge/automation/layout tokens fail at semantic paths; all resolved fields participate in canonical hashing through explicit tokens.
- The production client, new-career reset, Windows headless diagnostics, and `Automation.Headless` defaults now use `DishStationFirstHoursContent.ScenarioConfiguration`. Headless scenario flags remain explicit `with` overrides over authored defaults rather than a duplicate scenario.
- Replay/save behavior remains authoritative: `DishStationReplaySave` stores the fully resolved configuration and restore reconstructs the world from it.
- Focused S016 tests — passed, 4/4: exact legacy-value equivalence, fixed-seed tick-250 snapshot/notification/replay/future equivalence plus save restore, content-only capacity/config/hash change, and targeted invalid-value diagnostics. The combined S014-S016 content focus passed 20/20.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --compile-content content/restaurant/first-shift.yaml` — passed with 15 definitions and hash `0d84eaa217b147037bd3413432da646eb9b22a58cd933aeac5613d8a60c16ca5`.
- The S014 minimal fixture remains compatible and retains hash `8618a874c2e098f5b18028f50a25457a1065ead3271266a9a69c7ea97df73125`.
- Headless authored defaults reported the reference `30/3`, rack `12`, washer `20`, worker `5/4`, glass demand every `15`, sticky-after `2`; explicit CLI overrides reported rack `7`, washer `9`, and tray demand.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 162/162 tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- Native UI smoke was not run: S016 is fully proven headlessly and launching the client could not satisfy the no-focus guarantee. No GUI or audio process was launched.
- Human scenario/content inspection: **PENDING — user-run** using the supplied S016 steps.

### S017 — Content Validation Test Project — DONE

**Value:** Broken content cannot silently enter the build.

**Deliver:** dedicated content tests/linter covering IDs, refs, unreachable quest beats, invalid transitions/configs, duplicate IDs, missing presentation fallbacks where required.

**Proof:** seeded bad fixtures fail with targeted messages.

**Evidence (2026-08-11):**

- Added `tests/Automation.Content.Tests` as a dedicated solution-level content gate. The 11 compiler/schema tests moved out of `Automation.Integration.Tests`; content-only checks no longer depend on the Stride client or broad integration assembly.
- Added eight durable mutation seeds in `content/fixtures/schema-v1/invalid/cases.json`: malformed ID, duplicate global ID, unknown reference, wrong reference type, incompatible process transition, invalid scenario configuration, missing presentation fallback, and unreachable quest beat.
- Every seeded case asserts its own diagnostic source filename, exact semantic path, and targeted message. A generic parse/build failure cannot satisfy the test.
- Extended workstation schema with required `presentation_fallback` and included it in canonical hashes. Both checked-in valid bundles explicitly use `presentation.fallback.workstation`.
- Added process-route semantic validation: connected workstations must accept a common item and the source output state must match the destination input state.
- Moved `DishTutorialStage` to the engine-neutral domain and made the first-shift adapter reject missing and unreachable authored beats against the authoritative stage set before the client can consume them.
- Dedicated content suite — passed, 22/22: 11 schema/compiler checks, eight seeded invalid cases, and three checked-in production/fixture validation tests.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --compile-content content/fixtures/schema-v1/minimal-restaurant.yaml` — passed, new fallback-aware hash `31460a4a78c65f0b2b78ffc24205fb9da4216c6b0686d94cb421a87171cfa0eb`.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --compile-content content/restaurant/first-shift.yaml` — passed, new fallback-aware hash `fdea16d0e161deb644b015f5e5522d1545d282d8c61f3614a5cd6fa7f49e7077`.
- S017 intentionally advances both manifests because the required presentation fallback is consequential authored content; earlier session hashes remain historical evidence for their then-current contracts.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 173/173 tests: Content 22, Domain 21, Simulation 28, Integration 102.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- `git diff --check` — passed; line-ending notices only.
- Native UI smoke was not run: S017 is fully headless and launching the client could not satisfy the no-focus guarantee. No GUI or audio process was launched.
- Human invalid-fixture/diagnostic inspection: **PENDING — user-run** using the supplied S017 steps.

### S018 — Deterministic Template Expansion — DONE

**Value:** Repeated scenario structures can be generated cheaply and reproducibly.

**Deliver:** template interface/model + parameter map + version + named seed + normalized expanded content + content hash.

**Proof:** same inputs produce byte/logically stable result; changed seed only affects declared variable fields.

**Evidence (2026-08-11):**

- Added the strict `template_schema_version: 1` YAML envelope with stable `template.*` ID, positive template version, typed parameter declarations, finite seeded variants, and one ordinary schema-v1 content body.
- Added immutable `ContentTemplateV1`, `IContentTemplateV1`, parameter/variant definitions, provenance, and expansion-result models. Expansion returns normalized YAML, its fully validated immutable catalog/manifest, sorted immutable provenance, and a deterministic expansion SHA-256.
- Added exact parameter-map validation and kinds for token, content ID, nonnegative integer, positive integer, boolean, and text values. Missing, extra, invalid, undeclared, unused, malformed, or unresolved placeholders fail with targeted diagnostics.
- Variable fields must be explicitly declared with finite normalized options. Templates with variants require a named seed; fixed templates reject one. SHA-256 selection uses template ID/version, seed, and field name and does not depend on runtime RNG or dictionary order.
- Every expansion re-enters `ContentCompilerV1`, so generated content cannot bypass S017 ID/reference/transition/configuration/presentation/quest validation.
- Added `Automation.Headless --expand-template <path> --named-seed <name> --parameter name=value`; it reports selected variants, definition count, content hash, and expansion hash without launching a GUI.
- Added `content/templates/proofs/seeded-scenario.template.yaml`: two typed parameters (`facility-slug`, `rack-capacity`) and one declared `demand-kind` seeded variant.
- Focused S018 template tests — passed, 9/9: byte/hash/provenance stability with reordered/canonically equivalent parameters, fixed seeds changing only the declared field, missing/extra/invalid inputs, required/unnecessary seed failures, downstream semantic validation, and undeclared/unused placeholder failures.
- Dedicated content suite — passed, 31/31.
- Fixed CLI expansion `proof-0` selected `demand-kind=tray`, content hash `68e7c7b9f0e836b7dc5d7fa3fc23f337f0c45f9d99d049a81565d0b8dcf9eaf5`, expansion hash `eca6d84d1cbe143d5942c5eef74b908622f247be6e8fb23dde975da1df71f763`.
- Fixed CLI expansion `proof-1` selected `demand-kind=plate`, content hash `a8b74f18e692dfed1d0e9138ba28bb711a7c1f55637c524cebfc4fd698f566d0`, expansion hash `2e599674cf9e262db6fc9079eb19c8e0613783aac140133fa17f417239a2b209`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 182/182 tests: Content 31, Domain 21, Simulation 28, Integration 102.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- `git diff --check` — passed; line-ending notices only.
- Native UI smoke was not run: S018 is fully headless and launching the client could not satisfy the no-focus guarantee. No GUI or audio process was launched.
- Human template/provenance inspection: **PENDING — user-run** using the supplied S018 steps.

### S019 — Workstation Template Family — DONE

**Value:** New equipment does not require hand-authoring every common behavior shell.

**Deliver:** manual, batch, buffer, inspection, service, and transport workstation templates where the domain supports them.

**Proof:** instantiate at least two templates in dish or fixture scenarios and validate behavior.

**Evidence (2026-08-11):**

- Added immutable schema-v1 workstation behavior definitions for manual, batch, buffer, inspection, and service families. A workstation may declare at most one family; behavior values participate in deterministic catalog hashing.
- Added five strict checked-in templates under `content/templates/workstations/`. Identity parameters use semantic tokens, numeric settings use positive integers, FIFO and state-count inspection are fixed to the only current semantics, and batch capacity is fixed/validated at the authoritative world's current single-dish capacity.
- Added family/state coherence validation: manual action transitions match dish rules, batch is `racked -> washed_in_machine`, buffer is `scraped -> racked`, inspection is non-mutating, and service is `available -> dirty`.
- Added the narrow `DishStationWorkstationTemplateAdapter`: generated batch cycle time, buffer capacity, and service demand settings configure the existing authoritative scenario. Manual work and inspection continue through the existing simulation commands; no parallel workstation executor was introduced.
- Transport is explicitly unsupported with a durable reason: current walking telemetry measures handling travel but the domain has no queued work-item movement primitive. S019 did not invent one.
- Focused workstation-family tests — passed, 5/5: deterministic typed expansion of all five families; batch timing and buffer capacity in `DishStationWorld`; manual state transition; inspection without inventory mutation; service consumption; targeted invalid-family diagnostics; explicit transport disposition.
- Dedicated content suite — passed, 36/36.
- Headless batch expansion — passed: content hash `d07fd0cb892de4c127fc7a7cd49f4601c7da1df131d8f9730584ac287fef1227`, expansion hash `9fe708f2d96c2a313ffe252ba40b49eb47371a5b9e27c7a335ba910d11b93431`.
- Headless buffer expansion — passed: content hash `0862b778098d681dfd930a368328dd89d5fa94e3d403e77d95320fd9ed5dad05`, expansion hash `bcfdc0540232aac5d4b4de243660b2120f9ce2ecc19f021baf1ced497edcb338`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 187/187 tests: Content 36, Domain 21, Simulation 28, Integration 102.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- `git diff --check` — passed; line-ending notices only.
- Native UI/e2e was not run: first-show focus behavior cannot satisfy the no-focus guarantee. No GUI or audio process was launched, and no monitor placement was required.
- Human template inspection: **PENDING — user-run** using the supplied S019 steps.

### S020 — Incident Template Family — DONE

**Value:** Failures can be authored as reusable pressure tests.

**Deliver:** delay, capacity loss, bad observation/sensor, blocked resource, worker absence, demand spike templates.

**Proof:** fixed seed produces reproducible incident timeline and trace.

**Evidence (2026-08-12):**

- Added the ninth schema-v1 definition kind, `incident.*`, with stable ID, industry, trigger tick, scope, immediate observable, discoverable evidence, recovery description, positive duration, and exactly one typed effect. Incident definitions participate in graph validation, normalized counts, and deterministic catalog hashing.
- Added six strict checked-in templates under `content/templates/incidents/`: process delay, rack-capacity loss, sticky reported-ready sensor, blocked washer, new-hire absence, and demand spike. Each exposes only family-relevant typed parameters plus one finite seeded `trigger-tick` variant.
- Added the closed engine-neutral incident ontology in `Automation.Domain` and recorded the decision in ADR-0008. Content adapts definitions to scheduled domain incidents; neither domain nor simulation references YAML, the content compiler, or Stride.
- Added replay-serializable `TriggerDishStationIncidentCommand`. `DishStationWorld` owns authoritative start/end tick semantics, rejects overlapping instances of one family, applies effects to existing washer timing, rack admission, readiness, availability, worker cadence, and demand rules, and exposes bounded active/lifecycle trace snapshots.
- Added headless `--run-incident` execution over ordinary template expansion. Fixed `incident-proof-42` selects trigger tick 3, starts at tick 3, recovers at tick 6, and ends with zero active incidents and two trace entries.
- Focused S020 tests — passed, 5/5 methods covering all six families: deterministic typed expansions, seed isolation to the declared trigger field, every authoritative effect plus recovery, fixed-seed world/replay equivalence, and targeted invalid effect diagnostics.
- Dedicated content suite — passed, 41/41.
- Minimal all-kinds schema fixture — passed with nine definitions and hash `83ccfdee852c5fe03d6de94486724b453b0b368bef370b27ade7afdc0220c332`.
- Deterministic demand-spike run (executed twice with byte-identical console output) — content hash `1a599addcb33e0cc410b955ea72af445038e43d4c0aef91109d48552ab1751b3`, expansion hash `693356cec1817a649b3311c4d6c8848767e8865f7ecc0251a364644a11083541`, lifecycle `Started@3 -> Recovered@6`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 192/192 tests: Content 41, Domain 21, Simulation 28, Integration 102.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- `git diff --check` — passed; line-ending notices only.
- Native UI/e2e was not run: first-show focus behavior cannot satisfy the no-focus guarantee. No GUI or audio process was launched, and no monitor placement was required.
- Human incident-template/trace inspection: **PENDING — user-run** using the supplied S020 steps.

---

## N4 — Player Tools

### S021 — Process Capture Model — DONE

**Value:** The game can turn observed/manual work into an explicit player-owned process artifact.

**Deliver:** process capture entities/events, provenance, current/baseline version.

**Proof:** perform a small workflow and inspect captured ordered steps.

**Evidence (2026-08-12):**

- Added engine-neutral process-capture IDs, ordered steps, provenance, player ownership, immutable process versions, active-session state, completed artifacts, and lifecycle events in `Automation.Domain`.
- Added explicit replay-serialized `StartProcessCaptureCommand` and `CompleteProcessCaptureCommand`. Capture completion requires at least one successful step; blank names, nested sessions, missing sessions, and empty completion are rejected without creating artifacts.
- Capture hooks the existing authoritative `Perform` path after success. Each step records sequence, observed tick, player actor, concrete workstation, dish action/kind, and authoritative input/output states. Failed attempts, washer completion, service demand, automation, incident effects, and new-hire work do not become authored manual steps.
- Completing the first capture creates deterministic artifact ID 1 owned by player actor 0, with immutable baseline v1 and current v1 sharing the same five-step sequence and capture provenance. Editing/version derivation remains S022.
- Replay proof reconstructs both an in-progress two-step capture and its subsequently completed five-step artifact, including events, IDs, provenance, versions, and command journal.
- Added `Automation.Headless --capture-demo`. Repeated fixed-seed runs produced byte-identical output for `Scrape -> Rack -> StartWasher -> Unload -> DryAndRestock`, preserving ticks `2,3,4,25,26` and transitions `Dirty -> Scraped -> Racked -> Washing -> WashedInMachine -> CleanWet -> Available`.
- Recorded the bounded ontology and ownership/versioning decision in ADR-0009. No generic workflow graph, editor, route mutation, or client-owned truth was introduced.
- Focused S021 process-capture tests — passed, 4/4: complete ordered workflow, failed/non-player exclusion, lifecycle validation, and active/completed replay equivalence.
- Simulation suite — passed, 32/32.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 196/196 tests: Content 41, Domain 21, Simulation 32, Integration 102.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- `git diff --check` — passed; line-ending notices only.
- Native UI/e2e was not run: first-show focus behavior cannot satisfy the no-focus guarantee. No GUI or audio process was launched, and no monitor placement was required.
- Human captured-artifact inspection: **PENDING — user-run** using the supplied S021 steps.

### S022 — Process Editor v1 — DONE

**Value:** Player can change a process without editing files/code.

**Deliver:** view steps, reorder supported steps, edit routing/assignment fields supported by current model, validate, apply.

**Proof:** alter dish flow, rerun, observe changed measurable outcome.

**Evidence (2026-08-12):**

- Added explicit simulation-owned process edit drafts copied from an artifact's current version. Stable step IDs survive reordering; draft steps expose player/new-hire assignment and one concrete routing policy: captured order, plates first, or glasses first.
- Added replay-serialized begin, move, assign, set-routing, apply, and discard commands. Invalid/no draft, unknown artifacts/steps/actors/policies, stale versions, edge moves, and nested draft/capture states reject without mutating applied truth.
- Apply validates nonempty unique contiguous steps and adjacent authoritative state compatibility. The asynchronous washer start/unload handoff is explicitly supported; a deliberately swapped Rack/StartWasher order produces `Step 3 expects Racked, but preceding step 1 produces Scraped` and cannot apply.
- Valid apply preserves baseline v1, derives current v2 with edit provenance, closes the draft, and marks the owned artifact applied. New-hire execution considers only actions assigned to actor 1 and resolves plate/glass choice from the applied routing policy.
- Deterministic rerun proof compares equivalent plate/glass work: plates-first yields one glass shortage; glasses-first yields zero and records glass completion by `NewHireWork` before the demand check.
- Added a player-facing paused Process Editor modal. `H` starts/finishes capture; after capture, `Enter` opens the editor; `Up/Down` selects, `Q/E` reorders, `A` toggles assignment, `R` cycles routing, `Enter/Space` validates/applies, and `Esc` discards. The modal shows baseline/current/draft versions, full ordered transition rows, assignments, routing, and targeted validation status.
- Added semantic `ProcessEditor` input context, gameplay-only capture/editor-open actions, gameplay-only modal routing, and a separately testable client simulation-pause policy. No OS input or GUI automation is needed for regression coverage.
- Added `Automation.Headless --process-editor-demo`: its fixed run visibly rejects the invalid reorder, applies glass-first current v2 with all five steps assigned to actor 1, completes two dishes, and reaches zero shortages. Repeated runs produced byte-identical output.
- Recorded draft/version/application ownership in ADR-0010 and updated control/architecture documentation.
- Focused S022 simulation tests — passed, 4/4: invalid reorder diagnostics, immutable baseline/current v2, routing/service outcome, and draft/applied replay reconstruction.
- Focused client/input/modal/presenter/pause coverage — passed within 20 selected integration cases.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 206/206 tests: Content 41, Domain 21, Simulation 36, Integration 108.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%.
- `git diff --check` — passed; line-ending notices only.
- Native UI/e2e was not run: first-show focus behavior cannot satisfy the no-focus guarantee. No GUI or audio process was launched, and no monitor placement was required.
- Human playable editor acceptance: **PENDING — user-run** using the supplied S022 steps.

### S023 — Automation IR v1 — DONE

**Value:** Automation has deterministic, inspectable semantics independent of UI.

**Deliver:** minimal `ValueRef`, predicate, condition composition, effect/action, rule, trace/result model.

**Proof:** tests evaluate rules deterministically and emit an explainable trace.

**Delivered evidence (2026-08-12):**

- Added an engine-neutral, closed automation IR with typed Boolean/integer values; constants and named observables; compare, `all`, `any`, and `not`; stable enabled rules; ordered dish-action effects; structural validation; and immutable input/predicate/effect/outcome traces.
- Compiled both reported-ready and corroborated-ready washer policies into stable rules. Live selected effects enter the existing authoritative `Perform` boundary, and captured-incident replay uses the same evaluator. The authoritative snapshot exposes a bounded 24-entry rule-trace history.
- Added `Automation.Headless --automation-ir-demo`; the unsafe fixed input selects one effect under reported-ready and none under corroborated-ready. Two runs were byte-identical: 20 lines, SHA-256 `0A32B053955D294F2318B19A3013ECBB21E6241111A1AF842F6609AEF6256A07`.
- Focused domain coverage — passed, 4/4: composition/trace order, disabled rules, targeted invalid-rule diagnostics, and immutable ordered outcomes.
- Focused simulation coverage — passed, 4/4: policy compilation, authoritative execution/outcome, shared live/replay evaluation, and deterministic traces.
- ADR-0011 records the restricted capability boundary and deferred S024 authoring work.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 214/214 tests: Content 41, Domain 25, Simulation 40, Integration 108.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%, with one reported-ready incident and one corroborated-ready prevention.
- `git diff --check` — passed; line-ending notices only.
- Native UI/e2e was not run: safe first-show focus behavior is not established. No GUI process was launched; human trace inspection remains **PENDING — user-run** through the headless demo.

### S024 — Automation Rule Editor v1 — DONE

**Value:** Player creates one real automation through the game.

**Deliver:** condition/action editor over IR, validation, enable/disable, trace view.

**Proof:** replace one existing scenario automation shortcut with a player-authored rule.

**Delivered evidence (2026-08-12):**

- Added one simulation-owned player washer-rule draft at stable ID `automation.rule.dish-station.player-start-washer`, with editable enabled state and rack-present/reported-ready/physical-ready conditions over the closed Start Washer effect.
- Added replay-serialized begin, set-enabled, toggle-condition, set-action, apply, and discard commands. Invalid drafts retain path-specific diagnostics and cannot apply; replay saves reconstruct both applied rules and open drafts.
- The live world and captured-incident replay now evaluate the applied player rule directly through the S023 evaluator. Selected starts still execute through authoritative `Perform`, with outcome evidence attached to the trace.
- Added the paused Automation Rule Editor on semantic key `6`. Its presenter shows enabled state, editable conditions, the bounded action, targeted validation, observed inputs, predicate results, selected effect, and authoritative command outcome. Visible quest, handbook, README, and input-profile hints were updated.
- Replaced the canonical first-shift and developer smoke policy-selection shortcuts with player draft/apply command sequences. The first run applies reported-ready; refinement adds PhysicalReady to the same rule.
- Focused simulation editor coverage — passed, 4/4: live authoritative application, invalid draft diagnostics, same-rule safety refinement, and applied/open-draft replay reconstruction.
- Focused client/input/router coverage — passed within 25 selected tests, including editor presentation, modal pause/routing, semantic binding, v2→v3 binding migration, and existing Process Editor compatibility.
- `Automation.Headless --automation-editor-demo` — passed twice with byte-identical eight-line output, SHA-256 `189FDC9DD8B82944607385C0BE6EF69B117BDC6929CA353CF53F91B41870CE9A`; reported-ready replay matched, refined physical-ready replay did not.
- Production content validation — passed with updated deterministic first-shift manifest `f4890ef9cf4181ca043d69fa36f1d21e84e3c0104d4b11aab7b29e94cfdc41e2`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 223/223 tests: Content 41, Domain 25, Simulation 44, Integration 113.
- `dotnet run --project src/Automation.Headless -c Release -- --ticks 250 --seed 42` — passed at the unchanged gameplay reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%; the active rule is the stable player rule, with one unsafe incident and one refined-rule prevention.
- `git diff --check` — passed; line-ending notices only.
- ADR-0012 records draft ownership, the closed authoring capability, compatibility boundary, and deferred multi-rule/preset work.
- Native UI/e2e was not run: the smoke driver controls the shared cursor and safe non-activating first-show behavior remains unproven. No GUI was launched; human playable acceptance is **PENDING — user-run** using the supplied S024 steps.

### S025 — Presets and A/B Compare — DONE

**Value:** Experimentation becomes first class.

**Deliver:** baseline snapshot/preset, variant, replay both under same scenario seed, compare key metrics and incident outcomes.

**Proof:** player can answer whether a change improved throughput/reliability and inspect why.

**Delivered evidence (2026-08-12):**

- Added immutable simulation-owned `Baseline` and `Variant` rule-preset slots plus replay-serialized save-preset and run-comparison commands. Saving a slot invalidates stale results; presets and evidence reconstruct through replay saves.
- Added a paired authoritative comparison runner. Both arms use the same validated scenario configuration, seed, horizon, demand, and deterministic support operator; each installs its captured rule through the S024 edit/apply capability in an isolated `DishStationWorld`.
- Comparison evidence includes completed dishes, shortages, automated starts, unsafe incidents, prevented unsafe requests, and each arm's first reported-ready/physical-not-ready evaluator evidence. Reliability ranks before shortages and throughput, so an unsafe speedup cannot win.
- Proved the experiment does not mutate live tick, dishes, completion/shortage counters, active rule, or live automation counters.
- Extended the paused Automation Editor: `B` saves baseline, `V` saves variant, and `R` runs the same-seed trial. The presenter shows preset identities, shared controls, side-by-side metrics/deltas, verdict, and the differing PhysicalReady predicate. Input profiles migrate v2/v3→v4 without losing existing remaps.
- Migrated the canonical first-shift and developer smoke choreography to save reported-ready as baseline, physical-ready as variant, and run the paired comparison.
- Focused simulation comparison coverage — passed, 5/5: immutable slots, identical controls/outcome gain, invalid requests, replay reconstruction, and live-world isolation.
- Focused editor/input/router coverage — passed within 27 selected integration tests.
- `Automation.Headless --automation-compare-demo` — passed twice with byte-identical seven-line output, SHA-256 `414DD122F612D6F3A4616EF618A02D1FC760E2366A4184B1CB347A7EDB9ED4F3`. Baseline: 1 completion, 7 shortages, 1 incident; variant: 5 completions, 3 shortages, 0 incidents, 5 prevented requests; verdict `VariantBetter` with PhysicalReady false as the causal predicate.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 230/230 tests: Content 41, Domain 25, Simulation 49, Integration 115.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged live reference state: 8 completed, 10 shortages, 7/8 quests, 2,500 XP, `OwnTheShift` at 50%. Its same-seed 100-tick comparison reports completions `2→4`, shortages `4→2`, incidents `1→0`, prevented `0→4`, verdict `VariantBetter`.
- `git diff --check` — passed; line-ending notices only.
- ADR-0013 records experimental controls, metric/verdict ordering, live-world isolation, and the bounded deterministic scope.
- Native UI/e2e was not run: the smoke driver controls the shared cursor and safe non-activating first-show behavior remains unproven. No GUI was launched; human playable comparison acceptance is **PENDING — user-run** using the supplied S025 steps.

N4 Player Tools is complete through process capture/editing, restricted automation semantics, player rule authoring, and controlled preset comparison.

---

## N5 — Restaurant Production Slice

### S026 — Character Schema and Restaurant Cast — DONE

**Value:** Named people replace anonymous tutorial functions.

**Deliver:** character definitions for Avery, Ray, Jules, Tessa, Devon; role, motivations, knowledge, authority, relationships, presentation refs.

**Proof:** existing quest participants resolve through stable character IDs.

**Evidence (2026-08-12):**

- Schema-v1 characters now require motivation, stable known-fact/blind-spot/authority IDs, directional typed relationships, and primary/fallback presentation references. Quests require explicit nonempty participant IDs; compilation rejects missing/off-roster participants and self, duplicate, missing, or cross-industry relationships.
- The production roster defines Avery Chen, Ray Morales, Jules Martin, Tessa Brooks, and Devon Price. All five belong to the first-shift scenario, and all eight quests carry their intended stable participant subsets through `DishStationFirstHoursContent`.
- Journal quest detail resolves participant display names and role labels from the compiled catalog instead of embedding identities in presentation code.
- All checked-in schema-v1 fixtures/templates were migrated. Production compilation passed with 19 definitions (5 characters, 8 quests), hash `84b4ee8ddfa2bd9c540593352f4542a70d75ff9894086d72fae0a3aef708affc`; the minimal fixture passed with hash `947cf1d85ff60b1f44a3457c8cd28dc54f53e5aad88c2de7ffb0be7929b3c2cf`.
- `--character-roster-demo` printed five complete characters and every quest mapping identically across two runs, output SHA-256 `9f48a6979a5d7a8c5cc82e01163a0527830bd945d1cbeefd594493d36b0facf2`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 237/237 tests: Content 46, Domain 25, Simulation 49, Integration 117.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%, and the controlled comparison remained `VariantBetter`.
- ADR-0014 records stable identity, scenario roster ownership, directional relationship constraints, quest participation, presentation fallback, and explicit pre-alpha compatibility handling.
- Native GUI/e2e was not run: a visible first-show cannot be proven non-activating, and the smoke driver controls the shared cursor. No GUI was launched; human journal/cast acceptance is **PENDING — user-run** using the supplied S026 steps.

### S027 — Contextual Dialogue and Barks — DONE

**Value:** Characters react to the system instead of delivering detached exposition.

**Deliver:** trigger conditions, cooldown/priorities, authored short lines, quest/dialogue events.

**Proof:** one queue pressure, one incident, and one success state trigger contextually appropriate lines.

**Evidence (2026-08-12):**

- Character content now supports optional globally stable `dialogue.` bark IDs with participating quest, closed semantic trigger, priority, cooldown ticks, and a bounded short line. Compilation rejects malformed trigger/priority/cooldown, duplicate IDs, unknown quests, and speakers outside the quest participant list.
- The authoritative world records engine-neutral narrative events at the real first glass-pressure transition, unsafe automation request, and successful live reliability window. Replay reconstructs the exact event sequence; dialogue prose and speaker selection remain outside simulation.
- Deterministic routing applies per-bark cooldown, selects highest priority, and breaks ties by stable bark ID. The client resolves the selected stable speaker through the character catalog and presents the person's name, role, and line in a bounded overlay.
- Production authors Tessa's service-pressure line, Devon's reported-vs-physical incident line, and Avery's shift-success line. `--dialogue-demo --ticks 300 --seed 42` resolved them at ticks 30, 236, and 285 and produced byte-identical output across two runs, SHA-256 `a4af77281f5e4ada439d334f50fd02ac1d61da3a146420fee3ba675ab51b230f`.
- Production schema-v1 compilation passed with 19 definitions and hash `f5bbefb64ddbb18ae15049c887c7ea505d424d577c0220846380d211c1439fc1`; the optional-bark minimal fixture remained compatible at `947cf1d85ff60b1f44a3457c8cd28dc54f53e5aad88c2de7ffb0be7929b3c2cf`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 244/244 tests: Content 52, Domain 25, Simulation 49, Integration 118.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed at the unchanged reference state: 7/8 quests complete, 2,500 XP, `OwnTheShift` active at 50%, and the controlled comparison remained `VariantBetter`.
- ADR-0015 records the authoritative-event/content-router/presentation boundary and the intentionally closed S027 trigger family.
- Native GUI/e2e was not run: a visible first-show cannot be proven non-activating, and the smoke driver controls the shared cursor. No GUI was launched; human contextual-bark acceptance is **PENDING — user-run** using the supplied S027 steps.

### S028 — First-Shift Narrative Pass — DONE

**Value:** Existing mechanics become a coherent first chapter.

**Deliver:** revise briefing, transitions, character beats, motivation, debriefs, remove debug-language leakage.

**Proof:** complete first shift end-to-end; no required developer shortcuts.

**Evidence (2026-08-12):**

- Scenario schema-v1 now supports optional complete chapter narrative with title, briefing pages, debrief summary, and questions. The first-shift adapter requires exactly three workplace briefing pages and three debrief questions; the generic minimal fixture remains compatible without the optional block.
- The existing start flow and shift report consume compiled narrative through `FirstShiftNarrativePresenter`. A content-only mutation reaches briefing, debrief, and clean production window-title presentation without changing client logic.
- All eight quest situations, objectives, discoveries, unlock rationales, and guided steps were revised as one Rossi's first-shift arc. Avery frames outcomes and handoff authority; Ray contributes physical/tacit knowledge; Jules exposes transfer gaps; Tessa represents timed service demand; Devon distinguishes the Ready report from the occupied washer.
- Critical-path notifications and production UI now use workplace language rather than simulation/renderer/playtest/raw-tick phrasing. Detailed window-title diagnostics remain available only with the automation control file or explicit developer opt-in; useful developer tools remain isolated from the chapter path.
- The reusable `DishStationFirstShiftReferenceRun` completes using ordinary work, evidence, layout, delegation, rule editing, replay, comparison, and shift-handoff commands. It waits for naturally produced glass supply and contains zero `AddDirtyDishes`, `ConfigureDishSupply`, `ResetDishStation`, `InjectStickyReadyFault`, or legacy `ConfigureWasherAutomation` commands.
- `--narrative-demo --ticks 330 --seed 42` printed all briefing/quest/character/debrief beats and finished at tick 300 with 8/8 quests, a passed 3/3 handoff, and `developerCommands=0`. Two runs were byte-identical, SHA-256 `d19d75e2df08e2560514585c0fa162084dde2d9150ff1eec9d9bf6fcb6e1fa62`.
- Production content compilation passed with 19 definitions and hash `d075e71876e2870b2db07198283c0cc1a754e9a085ef4db8b8280d2267b6e663`; the minimal fixture remained `947cf1d85ff60b1f44a3457c8cd28dc54f53e5aad88c2de7ffb0be7929b3c2cf`.
- `dotnet build TheAutomationGame.sln -c Release` — passed, 0 warnings/errors.
- `dotnet test TheAutomationGame.sln -c Release --no-build` — passed, 250/250 tests: Content 55, Domain 25, Simulation 49, Integration 121.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` — passed with 7/8 quests complete at `ShiftReview`, naturally preparing supply before the scheduled handoff; comparison remained `VariantBetter`. The 330-tick proof completes the chapter.
- Replay reconstructs shift status/report, all quest outcomes, and narrative events. Authored and critical-path runtime prose is guarded against developer-language regression.
- ADR-0016 records chapter-copy ownership, clean production presentation, explicit optional-schema compatibility, and the no-shortcut reference proof.
- Native GUI/e2e was not run: a visible first-show cannot be proven non-activating, and the smoke driver controls the shared cursor. No GUI was launched; human full-chapter narrative acceptance is **PENDING — user-run** using the supplied S028 steps.

### S029 — Lightweight Shift Economy — DONE

**Value:** Improvements have visible tradeoffs.

**Deliver:** labor time/cost, waste/downtime/throughput value, one equipment or staffing cost, shift summary.

**Proof:** at least two viable choices have measurably different cost/performance profiles.

**Evidence (2026-08-12):**

- The production first shift now authors a complete engine-neutral economy rate set for completed-dish value, labor time/cost, enabled-worker staffing, tray rework, service-shortage downtime, automation-incident downtime, and flow-cell investment. Schema-v1 rejects incomplete/invalid blocks while scenarios without the optional block retain explicit defaults.
- `DishStationWorld` derives economy only from accepted work actions and authoritative ticks/consequences. The live snapshot separately exposes player/worker actions, labor/staffed ticks, rework, shortages, incidents, throughput, flow-cell purchase, each cost cause, total cost, and net value. The completed shift report freezes the same snapshot; replay and JSON save reconstruction reproduce it exactly.
- The gameplay HUD projects live value/cost/net, and the completed first-shift report projects the full causal scorecard. Presentation cannot post economy entries or mutate simulation truth.
- The bounded `--economy-compare-demo` uses the same authored scenario, seed 42, commands, and 120-tick horizon. Both choices were viable with zero shortages. Staffed linear completed 3 dishes, traveled 109 worker steps, produced value 360, cost labor/staffing/investment `72/120/0`, total 192, net 168. Staffed flow cell completed 4, traveled 65 steps, produced value 480, cost `90/120/180`, total 390, net 90. Two runs were byte-identical, SHA-256 `f5a4108e040717e89112c7da284a7eb6156d51f3f13bceed35ef2d8b1d395ece`.
- Production schema-v1 compilation passed with 19 definitions and SHA-256 `db0640550a1a48eb3da1b7f708c0ca02a36372da1f2b9afb6b8948afb017c5e7`; the no-economy compatibility fixture retains explicit defaults.
- Focused economy/content/simulation/integration validation passed 12/12. `dotnet build TheAutomationGame.sln -c Release` passed with 0 warnings/errors. `dotnet test TheAutomationGame.sln -c Release --no-build` passed 259/259: Content 58, Domain 26, Simulation 53, Integration 122.
- `dotnet run --project src/Automation.Headless -c Release --no-build -- --ticks 250 --seed 42` passed with 7/8 quests complete at `ShiftReview`; live economy was value 960, total cost 1,499, net -539. The 330-tick proof passed the shift at tick 300 and froze scorecard value 1,320, total cost 1,668, net -348 while later live operation continued independently.
- ADR-0017 records the authoritative rate/count/projection boundary, one-time first-shift investment semantics, replay-derived persistence, and explicit exclusion of a general ledger.
- `git diff --check` passed. Native GUI/e2e was not run: a visible first-show cannot be proven nonactivating, and the smoke driver controls the shared cursor. No GUI was launched; live-summary and scorecard acceptance are **PENDING — user-run** using the supplied S029 steps.

### S030 — Two Stations, One Problem — DONE

**Value:** Player encounters interchangeable routing policies naturally.

**Deliver:** second station/problem variation, policy choice or refactor opportunity, measurable consequences.

**Proof:** player can use two different routing strategies in equivalent interface/decision slot.

**Evidence (2026-08-12):**

- Authored a distinct `scenario.restaurant.two-stations` and `quest.restaurant.two-stations.one-problem`. The main dish room requests glasses, the patio requests plates, both begin in the same routing decision slot with glass-first, and the common trial horizon is five authoritative ticks. Schema-v1 validates the optional concrete block and includes it in the deterministic content hash.
- Added engine-neutral station IDs/profiles/configuration plus `TwoStationRoutingWorld`. Explicit set, copy, and run-trial commands own all consequential changes. Trials use real `DishStationWorld` runs and expose completion, shortage, work, travel, throughput value, cost, and net value; replay restores policies, copy count, and complete trial history.
- The deterministic seed-42 headless episode first copies glass-first from main to patio: both stations complete one dish, main has zero shortages, patio has one, and combined net is 120. Fitting patio to plates-first reruns the same horizon with both supplied, zero shortages, and combined net 200. Repeated output was byte-identical with SHA-256 `2fc321483ff626665d0366f50a31a7f459cfb83b4dcbdc0fe05af3046e2da6ab`.
- Added the post-shift `7` routing board. Left/right selects either station in the same visible decision slot, up/down changes its policy, `C` copies main to patio, `Enter` runs both stations, and `Esc` closes. The first-shift simulation pauses under the modal; presentation reads immutable routing snapshots and does not calculate outcomes.
- The board withholds the authored discovery until the latest authoritative trial has zero shortages with distinct fitted policies. Player-facing copy avoids naming Strategy before S031/S032.
- Native Release semantic smoke passed `EpisodeComplete`, copied/reran the mismatched trial (`1` shortage), fitted patio/reran (`0` shortages), validated playtest evidence and career resume, and retained [the visually inspected outcome frame](screenshots/first-shift/two-stations.png). Render-thread capture keeps reviewer artifacts available in detached Windows sessions; default smoke mode still retains OS-pointer coverage on an interactive desktop.
- ADR-0018 records the concrete two-station boundary, command/replay authority, per-action trial cost, deferred generic abstraction, and current career-save exclusion.
- `dotnet build TheAutomationGame.sln -c Release --no-restore` passed with 0 warnings/errors. `dotnet test TheAutomationGame.sln -c Release --no-build` passed 273/273: Content 61, Domain 28, Simulation 57, Integration 127. Production content compiled to 21 definitions with SHA-256 `33d12d1f5612c3140d6e5bcf76ec6e6fc01ca4182efe0abf413dc972e2e0df64`.

### S031 — Pattern Knowledge and Codex Foundation — DONE

**Value:** Game records a reusable concept from lived play.

**Deliver:** PatternDefinition/Knowledge/Evidence minimal model + Codex shell.

**Proof:** Strategy page shows player's own restaurant evidence.

**Evidence (2026-08-12):**

- Added engine-neutral semantic IDs, problem signatures, immutable evidence, evidence-cited lifecycle milestones, per-pattern knowledge, and a career profile. Validation rejects malformed IDs, duplicate records/milestones, direct synthetic recognition, and missing citations; the model has no Stride dependency.
- Schema-v1 now validates pattern definitions and references. Production content authors a hidden restaurant routing concept with the pre-name title **Reusable Routing Choice**, a two-evidence threshold, required application, and the S030 primary quest. The conventional catalog ID is not player-facing before naming.
- A concrete persistence-layer recognizer translates the authoritative S030 replay into two stable records: the copied-policy patio shortage establishes `Encountered`; the fitted zero-shortage result establishes `Applied`; together they conclude `Recognized`. Re-running it is idempotent and it never concludes `Named`.
- The versioned career envelope atomically persists the first-shift replay, two-station replay, and pattern profile. It upgrades legacy first-shift JSON, validates semantic IDs and journal uniqueness, and restores the complete evidence history on Continue.
- Added the paused `8` Pattern Codex. It unlocks only after recognition and projects the authored pre-name title plus the player's own problem, move, and consequence records. Both [the initial recognized page](screenshots/first-shift/pattern-codex.png) and [the resumed-career page](screenshots/first-shift/pattern-codex-resumed.png) were visually inspected; each explicitly withholds the conventional name.
- Native Release semantic smoke passed the complete S001–S031 journey and resumed with `routingTrials=2`, `routingShortages=0`, and `codex=recognized:2`. The deterministic `--pattern-knowledge-demo` ran byte-identically with SHA-256 `07676ed9cef38a5bdc33d6778650d8ff7be18184ef604a9f915dcfe06730495f` and reported recognized `true`, named `false`, two persisted evidence records, and two replay trials.
- ADR-0019 records evidence ownership, the concrete recognition boundary, career-envelope migration, pre-name presentation, and the deliberate deferral of generic recognition rules and naming.
- Production content compiled to 22 definitions with SHA-256 `0caeb0b816375a197d23078f158b3c3c1166aea4e8e07636e02dafb2a1fd5a99`.
- `dotnet build TheAutomationGame.sln -c Release --no-restore` passed with 0 warnings/errors. `dotnet test TheAutomationGame.sln -c Release --no-build` passed 288/288: Content 63, Domain 31, Simulation 57, Integration 137.

### S032 — Name the Pattern — DONE

**Value:** Strategy is named only after use/recognition.

**Deliver:** recognition beat, conventional name reveal, structure/tradeoff page, no quiz gate.

**Proof:** new profile reaches reveal only after qualifying evidence.

**Evidence (2026-08-12):**

- Wrote the episode before implementation: the starting profile is recognized but unnamed; the player reviews their lived records and explicitly acknowledges the recurring shape; the terminal profile is named and saved. The action is not a vocabulary question, answer score, XP gate, or simulation mutation.
- Extended schema-v1 pattern content with the conventional/display names, reflection prompt and acknowledgement, intent, three concrete restaurant roles, benefits, and costs. Validation requires complete unique reveal copy while keeping the pre-name title free of conventional vocabulary. Production content compiles to 22 definitions with SHA-256 `453b08f71ebe194f35fe854dc1ba1950add91024c84bd33bdf33841442b717e3`.
- Added persisted `PatternKnowledgeConclusion` citations and lifecycle order: `Named` requires `Recognized`, `Mastered` requires `Named`, and every conclusion must cite existing player evidence. Career schema 2 retains those citations and migrates schema-1 recognition to its applied record.
- Added `PatternNamingService` as the explicit application boundary. It rejects an empty/unrecognized profile, names recognized Strategy knowledge idempotently, cites `restaurant.two-stations.fitted`, and leaves both authoritative restaurant worlds unchanged.
- The deterministic `--pattern-naming-demo` begins recognized/unnamed, records reflection, prints Strategy intent/structure/benefits/costs, round-trips the career, and ends recognized/named with two evidence records and two routing trials. Repeated normalized output was byte-identical with SHA-256 `a47dac469636e65baf28835a4c83bf93be86f4814bd259b4010cac7db694c57a`.
- The paused `8` Codex now accepts `Enter` reflection only after recognition, saves immediately, and reveals the named page with the player's shortage and zero-shortage outcomes still visible. Native Release semantic smoke passed the complete journey and resumed with `routingTrials=2`, `routingShortages=0`, and `codex=named:2`.
- Both [the reveal frame](screenshots/first-shift/strategy-pattern.png) and [the resumed named page](screenshots/first-shift/strategy-pattern-resumed.png) were inspected at original resolution after correcting unsupported glyphs, text collisions, and awkward wraps.
- ADR-0020 records the evidence-gated application boundary, authored reveal content, conclusion migration, and deliberate absence of quiz or simulation-side naming.
- `dotnet build TheAutomationGame.sln -c Release --no-restore` passed with 0 warnings/errors. `dotnet test TheAutomationGame.sln -c Release --no-build` passed 294/294: Content 63, Domain 31, Simulation 57, Integration 143.

### S033 — Vendor and Outsourcing Side Arc — DONE

**Value:** Player learns that automation can transfer work and risk rather than erase it.

**Deliver:** vendor pitch, SLA/contract choices, integration boundary, failure/support incident, make/buy comparison.

**Proof:** at least two choices are viable and produce distinct operational risks.

**Evidence (2026-08-12):**

- Wrote the episode first: Strategy-named starting state, Sam's pitch, three viable proposals, one shared rare-tray boundary mismatch, explicit normal/incident outcomes, distinct retained-ownership risks, and comparison rather than a scored correct answer.
- Added engine-neutral proposal IDs, sourcing/boundary/knowledge ownership, SLA, visibility, fallback, and cost terms plus a concrete `VendorOutsourcingConfiguration`. The three closed bundles are in-house, managed vendor, and observable vendor adapter; invalid cross-bundle terms fail validation.
- Authored Sam Rivera, `scenario.restaurant.buy-the-box`, `quest.restaurant.buy-the-box`, and all three contracts in schema-v1. Production content now compiles to 25 definitions with SHA-256 `5132ef78fcfecd13d110da5c9c4e423b4ae3a55f70547108ca06697593514c49`.
- `VendorOutsourcingWorld` accepts explicit select/run commands and replays exactly. Each proposal receives eight service requests and the `exception`→`special` mismatch at tick 3. In-house is positive-net with one miss and team-owned diagnosis; managed vendor has the best normal net but four misses and vendor-only initial diagnosis; observable vendor pays more, uses two manual fallbacks, misses zero, and shares the trace. All three remain positive-net.
- Repeated normalized `--vendor-demo` output was byte-identical with SHA-256 `5ba56b341d611fd0c26349a165994d15dff40ade45604463cb0f463cbc38f95b`, reporting three viable proposals and three distinct risk profiles.
- Career schema 3 persists the vendor command journal and reconstructs both trials/traces; schema-2 and raw first-shift saves migrate to an empty optional side arc.
- Added the paused `9` **Buy the Box** board. It shows Sam's authored pitch, each proposal's source/boundary/SLA/trace/fallback/knowledge/cost/risk, and the selected authoritative incident trace. Native Release semantic smoke ran managed and observable proposals, resumed at `vendor=ObservableVendor:2:2`, and retained the visually inspected [managed incident](screenshots/first-shift/vendor-managed-incident.png), [completed comparison](screenshots/first-shift/vendor-comparison.png), and [resumed comparison](screenshots/first-shift/vendor-comparison-resumed.png).
- ADR-0021 records the concrete outsourcing ontology, fair vendor behavior, fixed-input comparison authority, career migration, and deliberate deferral of a generic procurement/contract system.
- `dotnet build TheAutomationGame.sln -c Release --no-restore` passed with 0 warnings/errors. `dotnet test TheAutomationGame.sln -c Release --no-build` passed 308/308: Content 65, Domain 33, Simulation 61, Integration 149.

### S034 — Restaurant Art and Audio Polish — DONE

**Value:** First chapter reaches internal vertical-slice presentation bar.

**Deliver:** complete required production or approved-alpha assets for room/equipment/items/cast/UI/audio/VFX.

**Proof:** no shipping-facing placeholders in first chapter unless explicitly accepted and tracked.

**Delivered:**

- Wrote the presentation acceptance episode before implementation: the same authoritative shift must communicate room purpose, station/item/cast identity, operating state, consequence, and accessible audio without adding simulation truth.
- Replaced the obsolete placeholder wish list with the restaurant approved-alpha register. Nine concrete surfaces cover room, station family, imported washer, item family, world cast, narrative identities, UI, audio, and VFX; each records source/license, accepted limitation, and replacement trigger.
- Added the executable `RestaurantAlphaAssetAudit`. It rejects missing categories, duplicate IDs, critical placeholder/fallback-only status, absent provenance/license, undocumented alpha debt, missing audio/VFX equivalents, and incomplete idle/ready/active/complete/blocked/selected/interactable coverage.
- Polished the native room with work zones, utility trim, lighting, basin/splash guard, drain surface, open dirty/clean racks, service top, and equipment-specific overlays while preserving engine-neutral fixture anchors and the complete SpriteBatch fallback.
- Added non-color plate/glass/tray silhouettes; deterministic `IDLE`/`READY`/`RUN`/`DONE`/`ATTN` washer presentation; reduced-motion-safe pulses; and six distinct content-ID-resolved cast badges for Avery, Ray, Jules, Tessa, Devon, and Sam.
- Expanded project-authored audio from seven to nine compiled mono assets with a washer running loop and UI confirmation. Authoritative washer snapshots now reconcile loop start/stop across ordinary completion, reset, mute, and resume; every information-bearing event retains text/state equivalence.
- Updated the closest asset/audio plan, current-state audit, README, provenance/third-party notice, screenshot gallery, and ADR-0022. No domain, simulation, content schema, command, save, or replay type changed.

**Validation:**

- Focused presentation/audio/room/cast/audit tests passed 21/21. WAV validation checks every accepted source is RIFF PCM, mono, 22,050 Hz, and 16-bit; operational tests prove distinct item silhouettes and washer attention priority.
- Non-playing Release asset diagnosis passed: `audioAssets=9/9`, `roomModules=161`, `roomKinds=13`, `blockedFixtures=6`, `portsConnected=True`, `detourSteps=4`, `gui=not-started`.
- Full Release suite passed 316/316: Content 65, Domain 33, Simulation 61, Integration 157.
- Native Release semantic smoke completed `EpisodeComplete`, passed the 3/3 live reliability window, resumed the level-7 career, and reported `[room=native] [assets=alpha] [audio=ready]`. The inspected [approved-alpha running frame](screenshots/first-shift/restaurant-approved-alpha.png) shows the polished room, equipment silhouettes, player, washer `RUN` state, and visible running-audio caption; the inspected [Devon incident frame](screenshots/first-shift/shift-window-running.png) shows the named-cast badge and critical response without clipping. The [Sam vendor frame](screenshots/first-shift/vendor-comparison.png) also retains a clean badge/header layout.

### S035 — Restaurant Human Readiness Gate — TODO

**Class:** PLAYTEST

**Value:** Validate comprehension rather than implementation confidence.

**Deliver:** run at least five first-hours sessions using `43_QUALITY_PLAYTEST_RELEASE_GATES.md`; aggregate observed friction and comprehension.

**Proof:** evidence report with pass/fail per readiness criterion and prioritized follow-up sessions.

---

## N6 — Warehouse Reuse Proof

### S036 — Warehouse Scenario Skeleton — TODO

**Value:** Second industry exists headlessly using common primitives.

**Deliver:** receiving facility, package item family, receiving/buffer/storage/inspection/hold process, fixed seed.

**Proof:** headless scenario completes with useful metrics and no restaurant-specific domain dependency.

### S037 — Walkable Manual Receiving — TODO

**Value:** Warehouse is playable before automation.

**Deliver:** authored/procedural presentation shell, player receives/scans/moves/inspects packages.

**Proof:** complete one inbound flow manually.

### S038 — Command Exposure — TODO

**Value:** Work actions become queueable/auditable when connectivity/execution is delayed.

**Deliver:** narrative incident + mechanics demonstrating action-as-command without naming pattern immediately.

**Proof:** queue, inspect, execute/retry a captured operation.

### S039 — Chain of Responsibility Exposure — TODO

**Value:** Exceptions move through bounded handlers rather than one giant decision point.

**Deliver:** worker → lead → inventory/safety/vendor escalation chain with different responsibilities.

**Proof:** at least two exception types stop at different handlers.

### S040 — Iterator and Composite Exposure — TODO

**Value:** Player traverses different inventory orderings and treats shipment/pallet/case/item hierarchies uniformly where appropriate.

**Proof:** one traversal-order decision + one recursive hold/inspect operation.

### S041 — Factory Method Exposure — TODO

**Value:** Package/workflow type selects appropriate concrete handling implementation through a creation seam.

**Proof:** two incoming types create distinct handling paths without caller knowing concrete construction detail.

### S042 — Warehouse Reliability Arc — TODO

**Value:** Introduce optional retry/idempotency/dead-letter style side content through operational failure.

**Proof:** failure is reproducible, multiple remedies exist, trace shows consequences.

### S043 — Second-Industry Reuse Audit — TODO

**Class:** PLATFORM

**Value:** Generalize only what two industries prove is common.

**Deliver:** identify duplicate/shared shapes, remove restaurant naming leakage, document legitimate industry-specific concepts, add architecture tests where valuable.

**Proof:** both industries pass; shared abstractions have two concrete callers; no speculative third-use framework.

---

## After S043

Do not pre-expand this file into hundreds of tiny implementation sessions. Use `33_PRODUCT_ROADMAP.md`, `40_CAMPAIGN_STORY_CHARACTERS_PERSONAS.md`, and `41_PATTERN_LEARNING_AND_PATTERNKIT.md` to generate the next fixed tranche after the warehouse reuse audit has revealed the real shared model.
