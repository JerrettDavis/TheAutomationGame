# Architecture

## Architectural goal

Build a reusable simulation platform that can support 2D-like isometric presentation, full 3D presentation, headless execution, deterministic scenario replay, content authoring, and eventual modding without coupling the game rules to Stride.

## Hard boundary

> `Automation.Domain`, `Automation.Simulation`, and content models must not reference Stride types.

Prohibited outside client/adapters:

- `Entity`
- `Scene`
- `TransformComponent`
- `ModelComponent`
- Stride input APIs
- Stride asset handles
- renderer/physics-specific identity

## Proposed solution

```text
src/
  Automation.Domain/
    Identity/
    Actors/
    Work/
    Processes/
    Resources/
    Organizations/
    Automation/
    Knowledge/

  Automation.Simulation/
    World/
    Scheduling/
    Systems/
    Commands/
    Events/
    Time/
    Random/
    Spatial/
    Metrics/

  Automation.Content/
    Definitions/
    Loading/
    Validation/
    Compilation/

  Automation.Persistence/
    Saves/
    Snapshots/
    Replay/
    Migrations/

  Automation.Headless/
    Program.cs
    ScenarioRunner/
    BenchmarkRunner/

  Automation.Client.Stride/
    Bootstrap/
    Presentation/
    Input/
    Camera/
    UI/
    Audio/
    AssetMapping/

  Automation.Tools/
    ContentCompiler/
    ScenarioEditor/
    TraceViewer/
```

## Architectural style

The simulation should be **data-oriented and system-driven**, but we should not begin by writing a generic ECS framework.

Use:

- typed IDs;
- contiguous or chunked stores for high-count state;
- stateless or minimally stateful systems that operate over stores;
- explicit command/event boundaries;
- immutable definitions where practical;
- pooled buffers for hot paths;
- allocations outside steady-state tick loops.

Use ordinary C# objects where scale does not matter. Profile before creating specialized storage.

## Tick model

Rendering and simulation are independent.

Initial target:

```text
rendering              variable, 60+ FPS
movement/animation     20-30 Hz where simulation-owned
work interactions      10 Hz
slow facility systems   1 Hz
strategic/economic      scheduled/aggregated
```

The exact rates are configurable and may use phase scheduling rather than global loops.

## Command model

External intent enters as commands:

```csharp
public interface ISimulationCommand
{
    long ExecuteAtTick { get; }
}
```

Examples:

- assign worker;
- move equipment;
- change process definition;
- enable automation;
- approve policy;
- start observation;
- alter schedule.

Commands are validated and produce state transitions/events.

## Event model

Events describe meaningful completed facts:

- `WorkItemQueued`
- `MachineJammed`
- `EmployeeImprovised`
- `PaymentAccepted`
- `ProcessCompleted`
- `AssumptionInvalidated`

Not every internal mutation must become a durable domain event. Avoid turning the runtime into ceremonial event sourcing.

## Effects

Integrations or presentation side effects are emitted explicitly when useful. In headless mode, effects can be simulated or ignored.

## Render snapshots

The Stride client consumes a presentation snapshot/delta rather than traversing simulation internals.

```text
Simulation Thread(s)
      |
      | PresentationSnapshot
      v
Client Presentation
      |
      v
Stride Entities / Instances / UI
```

The presentation layer may interpolate transforms and use level-of-detail representations.

## Spatial model

Simulation position must use engine-neutral primitives. Initially:

```csharp
public readonly record struct SimPosition(float X, float Y, float Z);
```

If performance demands it, convert to SoA/chunked storage. Spatial queries should be abstracted from rendering and from any future physics choice.

## Physics

Do not make full rigid-body physics authoritative for ordinary process simulation. Use simplified deterministic movement/collision for most actors and objects. Stride/Bepu physics can provide presentation and selected interactions where physical simulation genuinely matters.

## Headless requirement

Every feature must be testable without launching Stride unless its purpose is presentation/input/audio.

A command such as the following should eventually work:

```bash
dotnet run --project src/Automation.Headless -- \
  run content/scenarios/restaurant/dish-station.yaml \
  --days 30 \
  --seed 42
```

## Dependency direction

```text
Domain <- Simulation <- Headless
  ^           ^
  |           |
Content    Persistence
  ^           ^
   \         /
    Client.Stride
```

Client-specific adapters depend inward; core libraries never depend outward.

## Concurrency

Start deterministic and simple. Parallelize proven hotspots by partitioning independent systems or chunks. Parallel processing must not make outcomes nondeterministic without an explicit design decision.

## Performance philosophy

Optimize in this order:

1. simulation fidelity and algorithmic necessity;
2. avoid work that does not need to run;
3. schedule at appropriate frequencies;
4. aggregate distant/inactive simulation;
5. improve data locality;
6. eliminate hot-path allocation;
7. parallelize;
8. use SIMD or specialized native code only where measurement justifies it.

## Architectural acceptance criteria

The first architecture spike passes when:

- 100k synthetic actors can execute a simple work loop headlessly;
- client rendering can display a subset without simulation ownership leaking into Stride;
- identical seed + command stream produces identical relevant outcomes;
- a save/snapshot can restore the simulation;
- core unit tests load no Stride assemblies.
