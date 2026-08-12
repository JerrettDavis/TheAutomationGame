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
    InteractWithFixture,
    InspectFixture,
    TriggerIncident,
    StartProcessCapture,
    CompleteProcessCapture,
    BeginProcessEdit,
    MoveProcessStep,
    AssignProcessStep,
    SetProcessRoutingPolicy,
    ApplyProcessEdit,
    DiscardProcessEdit,
    BeginAutomationRuleEdit,
    SetAutomationRuleEnabled,
    ToggleAutomationRuleCondition,
    SetAutomationRuleAction,
    ApplyAutomationRuleEdit,
    DiscardAutomationRuleEdit,
    SaveAutomationRulePreset,
    RunAutomationRuleComparison,
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
    public string? IncidentId { get; init; }
    public string? IncidentScope { get; init; }
    public string? IncidentObservable { get; init; }
    public string? IncidentEvidence { get; init; }
    public string? IncidentRecovery { get; init; }
    public DishStationIncidentKind IncidentKind { get; init; }
    public int IncidentDurationTicks { get; init; }
    public int IncidentMagnitude { get; init; }
    public string? ProcessName { get; init; }
    public int ProcessArtifactId { get; init; }
    public int ProcessStepId { get; init; }
    public int ProcessStepOffset { get; init; }
    public int ProcessActorId { get; init; }
    public ProcessRoutingPolicy ProcessRoutingPolicy { get; init; }
    public AutomationObservable AutomationObservable { get; init; }
    public AutomationPresetSlot AutomationPresetSlot { get; init; }
    public int ComparisonHorizonTicks { get; init; }

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
        InteractWithDishStationFixtureCommand value => new() { CommandKind = RecordedCommandKind.InteractWithFixture, ExecuteAtTick = value.ExecuteAtTick.Value, Fixture = value.Fixture, DishKind = value.Kind },
        InspectDishStationFixtureCommand value => new() { CommandKind = RecordedCommandKind.InspectFixture, ExecuteAtTick = value.ExecuteAtTick.Value, Fixture = value.Fixture, DishKind = value.Kind },
        SetNewHireEnabledCommand value => new() { CommandKind = RecordedCommandKind.SetNewHireEnabled, ExecuteAtTick = value.ExecuteAtTick.Value, Enabled = value.Enabled },
        TrainNewHireCommand value => new() { CommandKind = RecordedCommandKind.TrainNewHire, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessSpecification = value.Specification },
        ConfigureWasherAutomationCommand value => new() { CommandKind = RecordedCommandKind.ConfigureWasherAutomation, ExecuteAtTick = value.ExecuteAtTick.Value, AutomationPolicy = value.Policy },
        InspectAutomationIncidentCommand value => new() { CommandKind = RecordedCommandKind.InspectAutomationIncident, ExecuteAtTick = value.ExecuteAtTick.Value },
        ReplayAutomationIncidentCommand value => new() { CommandKind = RecordedCommandKind.ReplayAutomationIncident, ExecuteAtTick = value.ExecuteAtTick.Value },
        InjectStickyReadyFaultCommand value => new() { CommandKind = RecordedCommandKind.InjectStickyReadyFault, ExecuteAtTick = value.ExecuteAtTick.Value },
        CompleteIntroCommand value => new() { CommandKind = RecordedCommandKind.CompleteIntro, ExecuteAtTick = value.ExecuteAtTick.Value, GuidanceMode = value.GuidanceMode, ReducedMotion = value.ReducedMotion, HighContrast = value.HighContrast },
        StartShiftTrialCommand value => new() { CommandKind = RecordedCommandKind.StartShiftTrial, ExecuteAtTick = value.ExecuteAtTick.Value },
        TriggerDishStationIncidentCommand value => FromIncident(value),
        StartProcessCaptureCommand value => new() { CommandKind = RecordedCommandKind.StartProcessCapture, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessName = value.Name },
        CompleteProcessCaptureCommand value => new() { CommandKind = RecordedCommandKind.CompleteProcessCapture, ExecuteAtTick = value.ExecuteAtTick.Value },
        BeginProcessEditCommand value => new() { CommandKind = RecordedCommandKind.BeginProcessEdit, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessArtifactId = value.ArtifactId.Value },
        MoveProcessStepCommand value => new() { CommandKind = RecordedCommandKind.MoveProcessStep, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessStepId = value.StepId.Value, ProcessStepOffset = value.Offset },
        AssignProcessStepCommand value => new() { CommandKind = RecordedCommandKind.AssignProcessStep, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessStepId = value.StepId.Value, ProcessActorId = value.Actor.Value },
        SetProcessRoutingPolicyCommand value => new() { CommandKind = RecordedCommandKind.SetProcessRoutingPolicy, ExecuteAtTick = value.ExecuteAtTick.Value, ProcessRoutingPolicy = value.Policy },
        ApplyProcessEditCommand value => new() { CommandKind = RecordedCommandKind.ApplyProcessEdit, ExecuteAtTick = value.ExecuteAtTick.Value },
        DiscardProcessEditCommand value => new() { CommandKind = RecordedCommandKind.DiscardProcessEdit, ExecuteAtTick = value.ExecuteAtTick.Value },
        BeginAutomationRuleEditCommand value => new() { CommandKind = RecordedCommandKind.BeginAutomationRuleEdit, ExecuteAtTick = value.ExecuteAtTick.Value },
        SetAutomationRuleEnabledCommand value => new() { CommandKind = RecordedCommandKind.SetAutomationRuleEnabled, ExecuteAtTick = value.ExecuteAtTick.Value, Enabled = value.Enabled },
        ToggleAutomationRuleConditionCommand value => new() { CommandKind = RecordedCommandKind.ToggleAutomationRuleCondition, ExecuteAtTick = value.ExecuteAtTick.Value, AutomationObservable = value.Observable },
        SetAutomationRuleActionCommand value => new() { CommandKind = RecordedCommandKind.SetAutomationRuleAction, ExecuteAtTick = value.ExecuteAtTick.Value, Action = value.Action },
        ApplyAutomationRuleEditCommand value => new() { CommandKind = RecordedCommandKind.ApplyAutomationRuleEdit, ExecuteAtTick = value.ExecuteAtTick.Value },
        DiscardAutomationRuleEditCommand value => new() { CommandKind = RecordedCommandKind.DiscardAutomationRuleEdit, ExecuteAtTick = value.ExecuteAtTick.Value },
        SaveAutomationRulePresetCommand value => new() { CommandKind = RecordedCommandKind.SaveAutomationRulePreset, ExecuteAtTick = value.ExecuteAtTick.Value, AutomationPresetSlot = value.Slot },
        RunAutomationRuleComparisonCommand value => new() { CommandKind = RecordedCommandKind.RunAutomationRuleComparison, ExecuteAtTick = value.ExecuteAtTick.Value, ComparisonHorizonTicks = value.HorizonTicks },
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
            RecordedCommandKind.InteractWithFixture => new InteractWithDishStationFixtureCommand(tick, Fixture, DishKind),
            RecordedCommandKind.InspectFixture => new InspectDishStationFixtureCommand(tick, Fixture, DishKind),
            RecordedCommandKind.SetNewHireEnabled => new SetNewHireEnabledCommand(tick, Enabled),
            RecordedCommandKind.TrainNewHire => new TrainNewHireCommand(tick, ProcessSpecification),
            RecordedCommandKind.ConfigureWasherAutomation => new ConfigureWasherAutomationCommand(tick, AutomationPolicy),
            RecordedCommandKind.InspectAutomationIncident => new InspectAutomationIncidentCommand(tick),
            RecordedCommandKind.ReplayAutomationIncident => new ReplayAutomationIncidentCommand(tick),
            RecordedCommandKind.InjectStickyReadyFault => new InjectStickyReadyFaultCommand(tick),
            RecordedCommandKind.CompleteIntro => new CompleteIntroCommand(tick, GuidanceMode, ReducedMotion, HighContrast),
            RecordedCommandKind.StartShiftTrial => new StartShiftTrialCommand(tick),
            RecordedCommandKind.TriggerIncident => new TriggerDishStationIncidentCommand(tick, ToIncident()),
            RecordedCommandKind.StartProcessCapture => new StartProcessCaptureCommand(tick, ProcessName ?? throw new InvalidDataException("Recorded process name is missing.")),
            RecordedCommandKind.CompleteProcessCapture => new CompleteProcessCaptureCommand(tick),
            RecordedCommandKind.BeginProcessEdit => new BeginProcessEditCommand(tick, new(ProcessArtifactId)),
            RecordedCommandKind.MoveProcessStep => new MoveProcessStepCommand(tick, new(ProcessStepId), ProcessStepOffset),
            RecordedCommandKind.AssignProcessStep => new AssignProcessStepCommand(tick, new(ProcessStepId), new(ProcessActorId)),
            RecordedCommandKind.SetProcessRoutingPolicy => new SetProcessRoutingPolicyCommand(tick, ProcessRoutingPolicy),
            RecordedCommandKind.ApplyProcessEdit => new ApplyProcessEditCommand(tick),
            RecordedCommandKind.DiscardProcessEdit => new DiscardProcessEditCommand(tick),
            RecordedCommandKind.BeginAutomationRuleEdit => new BeginAutomationRuleEditCommand(tick),
            RecordedCommandKind.SetAutomationRuleEnabled => new SetAutomationRuleEnabledCommand(tick, Enabled),
            RecordedCommandKind.ToggleAutomationRuleCondition => new ToggleAutomationRuleConditionCommand(tick, AutomationObservable),
            RecordedCommandKind.SetAutomationRuleAction => new SetAutomationRuleActionCommand(tick, Action),
            RecordedCommandKind.ApplyAutomationRuleEdit => new ApplyAutomationRuleEditCommand(tick),
            RecordedCommandKind.DiscardAutomationRuleEdit => new DiscardAutomationRuleEditCommand(tick),
            RecordedCommandKind.SaveAutomationRulePreset => new SaveAutomationRulePresetCommand(tick, AutomationPresetSlot),
            RecordedCommandKind.RunAutomationRuleComparison => new RunAutomationRuleComparisonCommand(tick, ComparisonHorizonTicks),
            _ => throw new ArgumentOutOfRangeException(nameof(CommandKind)),
        };
    }

    private static RecordedSimulationCommand FromIncident(TriggerDishStationIncidentCommand command)
    {
        var effect = command.Incident.Effect;
        var magnitude = effect switch
        {
            ProcessDelayIncidentEffect value => value.AddedCycleTicks,
            CapacityLossIncidentEffect value => value.LostSlots,
            DemandSpikeIncidentEffect value => value.IntervalTicks,
            _ => 0,
        };
        var kind = effect is DemandSpikeIncidentEffect demand ? demand.DemandKind : default;
        return new()
        {
            CommandKind = RecordedCommandKind.TriggerIncident,
            ExecuteAtTick = command.ExecuteAtTick.Value,
            IncidentId = command.Incident.Id.Value,
            IncidentScope = command.Incident.Scope,
            IncidentObservable = command.Incident.Observable,
            IncidentEvidence = command.Incident.Evidence,
            IncidentRecovery = command.Incident.Recovery,
            IncidentKind = effect.Kind,
            IncidentDurationTicks = effect.DurationTicks,
            IncidentMagnitude = magnitude,
            DishKind = kind,
        };
    }

    private DishStationIncident ToIncident()
    {
        DishStationIncidentEffect effect = IncidentKind switch
        {
            DishStationIncidentKind.ProcessDelay => new ProcessDelayIncidentEffect(IncidentDurationTicks, IncidentMagnitude),
            DishStationIncidentKind.CapacityLoss => new CapacityLossIncidentEffect(IncidentDurationTicks, IncidentMagnitude),
            DishStationIncidentKind.BadSensor => new BadSensorIncidentEffect(IncidentDurationTicks),
            DishStationIncidentKind.BlockedResource => new BlockedResourceIncidentEffect(IncidentDurationTicks),
            DishStationIncidentKind.WorkerAbsence => new WorkerAbsenceIncidentEffect(IncidentDurationTicks),
            DishStationIncidentKind.DemandSpike => new DemandSpikeIncidentEffect(IncidentDurationTicks, DishKind, IncidentMagnitude),
            _ => throw new ArgumentOutOfRangeException(nameof(IncidentKind)),
        };
        return new(
            new(IncidentId ?? throw new InvalidDataException("Recorded incident ID is missing.")),
            IncidentScope ?? throw new InvalidDataException("Recorded incident scope is missing."),
            IncidentObservable ?? throw new InvalidDataException("Recorded incident observable text is missing."),
            IncidentEvidence ?? throw new InvalidDataException("Recorded incident evidence is missing."),
            IncidentRecovery ?? throw new InvalidDataException("Recorded incident recovery is missing."),
            effect);
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
