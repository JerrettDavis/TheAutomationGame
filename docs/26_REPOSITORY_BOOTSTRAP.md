# Repository Bootstrap

## Goal

Create a Windows-first Stride client around ordinary .NET 10 libraries while preserving a clean engine boundary from the first commit.

## Prerequisites

For the initial Windows development environment:

- Git with LFS;
- .NET 10 SDK;
- Stride Launcher / Stride 4.3;
- Visual Studio 2026, Rider, or another compatible IDE as preferred;
- Blender for 3D source assets.

Stride's current documentation describes the engine/editor as NuGet-packaged and Stride projects as standard C# solutions/projects. Use Game Studio to create the initial game/platform packages, then add ordinary .NET class libraries for the engine-independent code.

## Initial repository

```text
TheAutomationGame/
  TheAutomationGame.sln
  Directory.Build.props
  Directory.Packages.props
  global.json
  README.md
  AGENTS.md

  src/
    Automation.Domain/
    Automation.Simulation/
    Automation.Content/
    Automation.Persistence/
    Automation.Headless/
    Automation.Client.Stride/
    Automation.Client.Stride.Windows/
    Automation.Tools/

  tests/
    Automation.Domain.Tests/
    Automation.Simulation.Tests/
    Automation.Content.Tests/
    Automation.Persistence.Tests/
    Automation.Performance.Tests/

  content/
  assets-src/
  docs/
```

## Bootstrap sequence

### 1. Pin .NET

Create `global.json` targeting the chosen .NET 10 SDK feature band installed on developer/CI machines.

Do not use floating `latest` SDK behavior in CI.

### 2. Create the Stride game

Use Stride Game Studio to create a minimal Windows game named `Automation.Client.Stride` in a temporary/bootstrap folder. Select the modern graphics profile appropriate for the Windows prototype.

Move/merge the generated game and Windows platform packages into `src/` while retaining the `.sdpkg`, Assets, Resources, and platform entry-point files Game Studio expects.

### 3. Create core libraries

Representative commands:

```bash
dotnet new classlib -n Automation.Domain -f net10.0 -o src/Automation.Domain
dotnet new classlib -n Automation.Simulation -f net10.0 -o src/Automation.Simulation
dotnet new classlib -n Automation.Content -f net10.0 -o src/Automation.Content
dotnet new classlib -n Automation.Persistence -f net10.0 -o src/Automation.Persistence
dotnet new console  -n Automation.Headless -f net10.0 -o src/Automation.Headless
dotnet new classlib -n Automation.Tools -f net10.0 -o src/Automation.Tools
```

Create test projects using the team's preferred framework after the test-framework decision is recorded.

### 4. Add references in one direction

```text
Automation.Domain
      ^
      |
Automation.Simulation
      ^        ^
      |        |
Content   Persistence
      ^        ^
       \      /
      Headless

Automation.Client.Stride -> Domain/Simulation/Content/Persistence
```

Do not reference the Stride client from any core library.

### 5. Add build policy

`Directory.Build.props` should initially establish:

- nullable enabled;
- implicit usings enabled or deliberately disabled consistently;
- warnings as errors for project-owned code after bootstrap noise is handled;
- deterministic builds;
- analyzers selected by team;
- language version aligned with .NET/Stride baseline.

### 6. Add package policy

Use central package management (`Directory.Packages.props`) for ordinary NuGet dependencies where compatible with Stride's generated project requirements. Pin versions.

### 7. Add Git LFS

Track large authored binaries such as:

```text
*.blend
*.fbx
*.glb
*.wav
*.psd
*.kra
*.afdesign
```

Do not blindly put every runtime texture in LFS without checking repository behavior; record policy after first asset spike.

### 8. Establish build verification

First CI/local verification:

```bash
dotnet restore
dotnet build -c Release
dotnet test -c Release
```

Stride's generated asset build may require its normal build pipeline and should be validated on the Windows CI agent separately from headless core builds.

### 9. First architectural proof

Create:

```csharp
public readonly record struct SimulationTick(long Value);
public readonly record struct ActorId(int Value);

public sealed class SimulationWorld
{
    public SimulationTick Tick { get; private set; }

    public void Advance() => Tick = new(Tick.Value + 1);
}
```

Run it from both:

- `Automation.Headless`;
- the Stride client.

The same instance type must compile with no conditional Stride reference.

### 10. Add architecture test

Add a test or build script that fails if `Automation.Domain` or `Automation.Simulation` references an assembly/package beginning with `Stride.`.

## First commit exit criteria

- clean clone builds;
- Game Studio opens client project;
- Stride window runs;
- headless runner advances simulation;
- both consume the same core libraries;
- CI builds core/test projects;
- engine-boundary rule is documented and mechanically checked.

## Current status

The ordinary .NET 10 solution, deterministic domain/simulation libraries, headless runner, Stride 4.3 code-only window, test projects, central package policy, SDK pin, and mechanical engine-boundary test are present. The headless benchmark executes 100k synthetic actors and the Stride god view batch-renders a 10k-state projection without presentation ownership. Versioned JSON checkpoints restore deterministic midpoint state and future commands. The first playable intentionally remains a code-native SpriteBatch greybox; a Game Studio-authored content package is a later asset-pipeline migration, not a runtime blocker.
