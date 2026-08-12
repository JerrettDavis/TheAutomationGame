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

public sealed record InteractWithDishStationFixtureCommand(
    SimulationTick ExecuteAtTick,
    DishStationFixture Fixture,
    DishKind Kind) : ISimulationCommand;

public sealed record InspectDishStationFixtureCommand(
    SimulationTick ExecuteAtTick,
    DishStationFixture Fixture,
    DishKind Kind) : ISimulationCommand;

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

public sealed record BeginAutomationRuleEditCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record SetAutomationRuleEnabledCommand(
    SimulationTick ExecuteAtTick,
    bool Enabled) : ISimulationCommand;

public sealed record ToggleAutomationRuleConditionCommand(
    SimulationTick ExecuteAtTick,
    AutomationObservable Observable) : ISimulationCommand;

public sealed record SetAutomationRuleActionCommand(
    SimulationTick ExecuteAtTick,
    DishAction Action) : ISimulationCommand;

public sealed record ApplyAutomationRuleEditCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record DiscardAutomationRuleEditCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record SaveAutomationRulePresetCommand(
    SimulationTick ExecuteAtTick,
    AutomationPresetSlot Slot) : ISimulationCommand;

public sealed record RunAutomationRuleComparisonCommand(
    SimulationTick ExecuteAtTick,
    int HorizonTicks = AutomationPresetComparisonRunner.DefaultHorizonTicks) : ISimulationCommand;

public sealed record StartShiftTrialCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record InjectStickyReadyFaultCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record TriggerDishStationIncidentCommand(
    SimulationTick ExecuteAtTick,
    DishStationIncident Incident) : ISimulationCommand;

public sealed record StartProcessCaptureCommand(
    SimulationTick ExecuteAtTick,
    string Name) : ISimulationCommand;

public sealed record CompleteProcessCaptureCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record BeginProcessEditCommand(
    SimulationTick ExecuteAtTick,
    PlayerProcessArtifactId ArtifactId) : ISimulationCommand;

public sealed record MoveProcessStepCommand(
    SimulationTick ExecuteAtTick,
    ProcessStepId StepId,
    int Offset) : ISimulationCommand;

public sealed record AssignProcessStepCommand(
    SimulationTick ExecuteAtTick,
    ProcessStepId StepId,
    ActorId Actor) : ISimulationCommand;

public sealed record SetProcessRoutingPolicyCommand(
    SimulationTick ExecuteAtTick,
    ProcessRoutingPolicy Policy) : ISimulationCommand;

public sealed record ApplyProcessEditCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;

public sealed record DiscardProcessEditCommand(
    SimulationTick ExecuteAtTick) : ISimulationCommand;
