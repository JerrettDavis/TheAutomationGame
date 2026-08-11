using Automation.Domain;

namespace Automation.Simulation;

public interface ISimulationCommand
{
    SimulationTick ExecuteAtTick { get; }
}

public sealed record CompleteIntroCommand(
    SimulationTick ExecuteAtTick,
    GuidanceMode GuidanceMode,
    bool ReducedMotion = false,
    bool HighContrast = false) : ISimulationCommand;

public sealed record PerformDishActionCommand(
    SimulationTick ExecuteAtTick,
    DishAction Action,
    DishKind Kind) : ISimulationCommand;

public sealed record SetRushCommand(
    SimulationTick ExecuteAtTick,
    bool Enabled) : ISimulationCommand;

public sealed record AddDirtyDishesCommand(
    SimulationTick ExecuteAtTick,
    DishKind Kind,
    int Count) : ISimulationCommand;

public sealed record ConfigureDishSupplyCommand(
    SimulationTick ExecuteAtTick,
    DishState State,
    DishKind Kind,
    int Count) : ISimulationCommand;

public sealed record ResetDishStationCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record InspectProcessCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record ConfirmBottleneckCommand(
    SimulationTick ExecuteAtTick,
    DishState Hypothesis) : ISimulationCommand;

public sealed record ConfigureDishStationLayoutCommand(
    SimulationTick ExecuteAtTick,
    DishStationLayout Layout) : ISimulationCommand;

public sealed record PlaceDishStationFixtureCommand(
    SimulationTick ExecuteAtTick,
    DishStationFixture Fixture,
    FloorCell Cell) : ISimulationCommand;

public sealed record MovePlayerCommand(
    SimulationTick ExecuteAtTick,
    FloorCell Destination) : ISimulationCommand;

public sealed record SetNewHireEnabledCommand(
    SimulationTick ExecuteAtTick,
    bool Enabled) : ISimulationCommand;

public sealed record TrainNewHireCommand(
    SimulationTick ExecuteAtTick,
    DishProcessSpecification Specification) : ISimulationCommand;

public sealed record ConfigureWasherAutomationCommand(
    SimulationTick ExecuteAtTick,
    WasherAutomationPolicy Policy) : ISimulationCommand;

public sealed record InspectAutomationIncidentCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record ReplayAutomationIncidentCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record StartShiftTrialCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record InjectStickyReadyFaultCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;
