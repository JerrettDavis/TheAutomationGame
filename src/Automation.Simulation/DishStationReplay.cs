using Automation.Domain;

namespace Automation.Simulation;

public enum CommandInvocationMode
{
    Immediate,
    Scheduled,
}

public enum RecordedCommandKind
{
    PerformDishAction,
    SetRush,
    AddDirtyDishes,
    ConfigureDishSupply,
    ResetDishStation,
    InspectProcess,
    ConfirmBottleneck,
    ConfigureLayout,
    PlaceFixture,
    MovePlayer,
    SetNewHireEnabled,
    TrainNewHire,
    ConfigureWasherAutomation,
    InspectAutomationIncident,
    ReplayAutomationIncident,
    InjectStickyReadyFault,
    CompleteIntro,
    StartShiftTrial,
}

public sealed record RecordedSimulationCommand
{
    public required RecordedCommandKind CommandKind { get; init; }
    public long ExecuteAtTick { get; init; }
    public DishAction Action { get; init; }
    public DishKind DishKind { get; init; }
    public int Count { get; init; }
    public DishState DishState { get; init; }
    public bool Enabled { get; init; }
    public DishProcessSpecification ProcessSpecification { get; init; }
    public WasherAutomationPolicy AutomationPolicy { get; init; }
    public DishStationLayout Layout { get; init; }
    public DishStationFixture Fixture { get; init; }
    public FloorCell Cell { get; init; }
    public GuidanceMode GuidanceMode { get; init; }
    public bool ReducedMotion { get; init; }
    public bool HighContrast { get; init; }

    public static RecordedSimulationCommand FromCommand(ISimulationCommand command) => command switch
    {
        PerformDishActionCommand value => new() { CommandKind = RecordedCommandKind.PerformDishAction, ExecuteAtTick = value.ExecuteAtTick.Value, Action = value.Action, DishKind = value.Kind },
        SetRushCommand value => new() { CommandKind = RecordedCommandKind.SetRush, ExecuteAtTick = value.ExecuteAtTick.Value, Enabled = value.Enabled },
        AddDirtyDishesCommand value => new() { CommandKind = RecordedCommandKind.AddDirtyDishes, ExecuteAtTick = value.ExecuteAtTick.Value, DishKind = value.Kind, Count = value.Count },
        ConfigureDishSupplyCommand value => new() { CommandKind = RecordedCommandKind.ConfigureDishSupply, ExecuteAtTick = value.ExecuteAtTick.Value, DishState = value.State, DishKind = value.Kind, Count = value.Count },
        ResetDishStationCommand value => new() { CommandKind = RecordedCommandKind.ResetDishStation, ExecuteAtTick = value.ExecuteAtTick.Value },
        InspectProcessCommand value => new() { CommandKind = RecordedCommandKind.InspectProcess, ExecuteAtTick = value.ExecuteAtTick.Value },
        ConfirmBottleneckCommand value => new() { CommandKind = RecordedCommandKind.ConfirmBottleneck, ExecuteAtTick = value.ExecuteAtTick.Value, DishState = value.Hypothesis },
        ConfigureDishStationLayoutCommand value => new() { CommandKind = RecordedCommandKind.ConfigureLayout, ExecuteAtTick = value.ExecuteAtTick.Value, Layout = value.Layout },
        PlaceDishStationFixtureCommand value => new() { CommandKind = RecordedCommandKind.PlaceFixture, ExecuteAtTick = value.ExecuteAtTick.Value, Fixture = value.Fixture, Cell = value.Cell },
        MovePlayerCommand value => new() { CommandKind = RecordedCommandKind.MovePlayer, ExecuteAtTick = value.ExecuteAtTick.Value, Cell = value.Destination },
        SetNewHireEnabledCommand value => new() { CommandKind = RecordedCommandKind.SetNewHireEnabled, ExecuteAtTick = value.ExecuteAtTick.Value, Enabled = value.Enabled },
        TrainNewHireCommand value => new() { CommandKind = RecordedCommandKind.TrainNewHire, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessSpecification = value.Specification },
        ConfigureWasherAutomationCommand value => new() { CommandKind = RecordedCommandKind.ConfigureWasherAutomation, ExecuteAtTick = value.ExecuteAtTick.Value, AutomationPolicy = value.Policy },
        InspectAutomationIncidentCommand value => new() { CommandKind = RecordedCommandKind.InspectAutomationIncident, ExecuteAtTick = value.ExecuteAtTick.Value },
        ReplayAutomationIncidentCommand value => new() { CommandKind = RecordedCommandKind.ReplayAutomationIncident, ExecuteAtTick = value.ExecuteAtTick.Value },
        InjectStickyReadyFaultCommand value => new() { CommandKind = RecordedCommandKind.InjectStickyReadyFault, ExecuteAtTick = value.ExecuteAtTick.Value },
        CompleteIntroCommand value => new() { CommandKind = RecordedCommandKind.CompleteIntro, ExecuteAtTick = value.ExecuteAtTick.Value, GuidanceMode = value.GuidanceMode, ReducedMotion = value.ReducedMotion, HighContrast = value.HighContrast },
        StartShiftTrialCommand value => new() { CommandKind = RecordedCommandKind.StartShiftTrial, ExecuteAtTick = value.ExecuteAtTick.Value },
        _ => throw new ArgumentOutOfRangeException(nameof(command), command.GetType().Name, "Command is not replay-serializable."),
    };

    public ISimulationCommand ToCommand()
    {
        var tick = new SimulationTick(ExecuteAtTick);
        return CommandKind switch
        {
            RecordedCommandKind.PerformDishAction => new PerformDishActionCommand(tick, Action, DishKind),
            RecordedCommandKind.SetRush => new SetRushCommand(tick, Enabled),
            RecordedCommandKind.AddDirtyDishes => new AddDirtyDishesCommand(tick, DishKind, Count),
            RecordedCommandKind.ConfigureDishSupply => new ConfigureDishSupplyCommand(tick, DishState, DishKind, Count),
            RecordedCommandKind.ResetDishStation => new ResetDishStationCommand(tick),
            RecordedCommandKind.InspectProcess => new InspectProcessCommand(tick),
            RecordedCommandKind.ConfirmBottleneck => new ConfirmBottleneckCommand(tick, DishState),
            RecordedCommandKind.ConfigureLayout => new ConfigureDishStationLayoutCommand(tick, Layout),
            RecordedCommandKind.PlaceFixture => new PlaceDishStationFixtureCommand(tick, Fixture, Cell),
            RecordedCommandKind.MovePlayer => new MovePlayerCommand(tick, Cell),
            RecordedCommandKind.SetNewHireEnabled => new SetNewHireEnabledCommand(tick, Enabled),
            RecordedCommandKind.TrainNewHire => new TrainNewHireCommand(tick, ProcessSpecification),
            RecordedCommandKind.ConfigureWasherAutomation => new ConfigureWasherAutomationCommand(tick, AutomationPolicy),
            RecordedCommandKind.InspectAutomationIncident => new InspectAutomationIncidentCommand(tick),
            RecordedCommandKind.ReplayAutomationIncident => new ReplayAutomationIncidentCommand(tick),
            RecordedCommandKind.InjectStickyReadyFault => new InjectStickyReadyFaultCommand(tick),
            RecordedCommandKind.CompleteIntro => new CompleteIntroCommand(tick, GuidanceMode, ReducedMotion, HighContrast),
            RecordedCommandKind.StartShiftTrial => new StartShiftTrialCommand(tick),
            _ => throw new ArgumentOutOfRangeException(nameof(CommandKind)),
        };
    }
}

public sealed record RecordedCommandInvocation(
    long InvokedAtTick,
    CommandInvocationMode Mode,
    RecordedSimulationCommand Command);

public sealed record DishStationReplaySave(
    int SchemaVersion,
    int Seed,
    DishStationScenarioConfiguration Scenario,
    long SavedAtTick,
    RecordedCommandInvocation[] CommandInvocations)
{
    public const int CurrentSchemaVersion = 1;
}
