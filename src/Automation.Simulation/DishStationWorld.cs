using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Simulation;

public sealed class DishStationWorld
{
    private readonly Queue<ISimulationCommand> pendingCommands = new();
    private readonly List<RecordedCommandInvocation> commandJournal = new(64);
    private bool replaying;
    private readonly List<WorldNotification> notifications = new(32);
    private readonly List<DishStationNarrativeEvent> narrativeEvents = new(8);
    private readonly List<DishTransitionEntry> dishTransitions = new(24);
    private DishTransitionEntry[] dishTransitionSnapshot = [];
    private readonly DishCounts[] dishes = new DishCounts[Enum.GetValues<DishState>().Length];
    private readonly long[] plateItemTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly long[] glassItemTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly long[] trayItemTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly int[] maxQueueDepth = new int[Enum.GetValues<DishState>().Length];
    private readonly long[] completedPlateResidenceTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly long[] completedGlassResidenceTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly long[] completedTrayResidenceTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly int[] completedPlateVisits = new int[Enum.GetValues<DishState>().Length];
    private readonly int[] completedGlassVisits = new int[Enum.GetValues<DishState>().Length];
    private readonly int[] completedTrayVisits = new int[Enum.GetValues<DishState>().Length];
    private readonly long[] maxPlateResidenceTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly long[] maxGlassResidenceTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly long[] maxTrayResidenceTicks = new long[Enum.GetValues<DishState>().Length];
    private readonly Queue<long>[] plateStageEntries = CreateStageEntryQueues();
    private readonly Queue<long>[] glassStageEntries = CreateStageEntryQueues();
    private readonly Queue<long>[] trayStageEntries = CreateStageEntryQueues();
    private SimulationTick washerCompletesAt;
    private DishKind washingKind;
    private bool tutorialStarted;
    private int arrivalsUntilGlass;
    private readonly ActorId newHireId = new(1);
    private DishProcessSpecification newHireSpecification;
    private SimulationTick newHireActsAt;
    private DishAction? newHireLastAction;
    private DishKind? newHireLastKind;
    private int newHireActionsCompleted;
    private int newHirePlateActions;
    private int newHireGlassActions;
    private int newHireTrayActions;
    private int trayReworkIncidents;
    private int playerWorkActions;
    private int staffedTicks;
    private bool flowCellInvestmentPurchased;
    private bool newHireEnabled;
    private bool omittedPriorityObserved;
    private bool workerDeliveredGlass;
    private DishStationLayout layout;
    private DishStationPlacements placements;
    private FloorCell playerCell;
    private int sandboxMovementSteps;
    private int playerTravelSteps;
    private int newHireTravelSteps;
    private int baselineRouteSteps;
    private int layoutComparisonStartSteps;
    private int validatedRouteSteps;
    private WasherAutomationPolicy automationPolicy;
    private AutomationRule activeAutomationRule = DishStationAutomationRules.ForPolicy(WasherAutomationPolicy.Off);
    private AutomationRuleEditDraft? activeAutomationRuleEdit;
    private AutomationRulePreset? automationBaselinePreset;
    private AutomationRulePreset? automationVariantPreset;
    private AutomationComparisonResult? latestAutomationComparison;
    private bool automationHalted;
    private bool stickyReadySignal;
    private int automatedStarts;
    private int automationIncidents;
    private int preventedUnsafeStarts;
    private bool safetyBlockActive;
    private readonly List<AutomationTraceEntry> automationTrace = new(24);
    private AutomationTraceEntry[] automationTraceSnapshot = [];
    private readonly List<AutomationRuleTraceEntry> automationRuleTrace = new(24);
    private AutomationRuleTraceEntry[] automationRuleTraceSnapshot = [];
    private AutomationIncidentRecord? automationIncident;
    private int automationReplayCount;
    private bool automationHasReplay;
    private WasherAutomationPolicy lastReplayPolicy;
    private bool lastReplayWouldStart;
    private bool automationRegressionPassed;
    private uint faultRandomState;
    private bool introComplete;
    private GuidanceMode guidanceMode = GuidanceMode.Guided;
    private bool reducedMotion;
    private bool highContrast;
    private readonly bool[] completedQuests = new bool[Enum.GetValues<DishStationQuestId>().Length];
    private int careerExperience;
    private long careerStartedAtTick = -1;
    private readonly long[] questStartedAtTicks = Enumerable.Repeat(-1L, Enum.GetValues<DishStationQuestId>().Length).ToArray();
    private readonly long[] questCompletedAtTicks = Enumerable.Repeat(-1L, Enum.GetValues<DishStationQuestId>().Length).ToArray();
    private ShiftTrialStatus shiftTrialStatus;
    private int shiftTrialSuccessfulDemandChecks;
    private int shiftTrialAttempts;
    private int shiftTrialBaselineShortages;
    private int shiftTrialBaselineAutomationIncidents;
    private long shiftTrialStartedAtTick = -1;
    private long shiftTrialCompletedAtTick = -1;
    private ShiftReportSnapshot shiftReport;
    private readonly List<ActiveDishStationIncident> activeIncidents = new(6);
    private readonly List<DishStationIncidentTraceEntry> incidentTrace = new(24);
    private ActiveDishStationIncidentSnapshot[] activeIncidentSnapshot = [];
    private DishStationIncidentTraceEntry[] incidentTraceSnapshot = [];
    private int incidentProcessDelayTicks;
    private int incidentCapacityLoss;
    private bool incidentBadSensor;
    private bool incidentBlockedResource;
    private bool incidentWorkerAbsent;
    private bool incidentDemandSpike;
    private DishKind incidentDemandKind;
    private int incidentDemandIntervalTicks;
    private readonly ActorId playerActorId = new(0);
    private int nextProcessCaptureId = 1;
    private int nextProcessArtifactId = 1;
    private MutableProcessCapture? activeProcessCapture;
    private readonly List<PlayerOwnedProcessArtifact> processArtifacts = new(4);
    private readonly List<ProcessCaptureEvent> processCaptureEvents = new(24);
    private PlayerOwnedProcessArtifact[] processArtifactSnapshot = [];
    private ProcessCaptureEvent[] processCaptureEventSnapshot = [];
    private MutableProcessEdit? activeProcessEdit;
    private PlayerProcessArtifactId? appliedProcessArtifactId;

    private const int ShiftTrialTargetDemandChecks = 3;

    public DishStationWorld(int seed, DishStationScenarioConfiguration configuration)
    {
        Seed = seed;
        ArgumentNullException.ThrowIfNull(configuration);
        Configuration = configuration.Validate();
        ApplyScenarioStart();
    }

    public int Seed { get; }
    public DishStationScenarioConfiguration Configuration { get; }
    public SimulationTick Tick { get; private set; }
    public bool RushEnabled { get; private set; }
    public bool WasherRunning { get; private set; }
    public bool WasherOccupied => WasherRunning || At(DishState.WashedInMachine).Total > 0;
    public int Completed { get; private set; }
    public int ServiceShortages { get; private set; }
    public DishTutorialStage TutorialStage { get; private set; } = DishTutorialStage.RestockFirstDish;
    public DishState? BottleneckHypothesis { get; private set; }
    public DishKind LastShortageKind { get; private set; } = DishKind.Glass;
    public bool NewHireEnabled => newHireEnabled;
    public bool WasherPhysicalReady => !WasherOccupied && !incidentBlockedResource;
    public bool WasherReportedReady => incidentBadSensor || stickyReadySignal || WasherPhysicalReady;
    public DishStationLayout Layout => layout;
    public DishStationPlacements Placements => placements;
    public DishStationTopology Topology => new(placements);
    public FloorCell PlayerCell => playerCell;
    public IReadOnlyList<WorldNotification> Notifications => notifications;
    public bool IntroComplete => introComplete;
    public int CareerExperience => careerExperience;

    public DishCounts At(DishState state) => dishes[(int)state];

    public void Schedule(ISimulationCommand command)
    {
        RecordCommandInvocation(command, CommandInvocationMode.Scheduled);
        pendingCommands.Enqueue(command);
    }

    public CommandResult ExecuteNow(ISimulationCommand command)
    {
        if (command.ExecuteAtTick.Value > Tick.Value)
        {
            Schedule(command);
            return CommandResult.Accepted("Scheduled.");
        }

        RecordCommandInvocation(command, CommandInvocationMode.Immediate);
        return Execute(command);
    }

    public DishStationReplaySave CreateReplaySave() => new(
        DishStationReplaySave.CurrentSchemaVersion,
        Seed,
        Configuration,
        Tick.Value,
        commandJournal.ToArray());

    public static DishStationWorld Restore(DishStationReplaySave save)
    {
        ArgumentNullException.ThrowIfNull(save);
        if (save.SchemaVersion != DishStationReplaySave.CurrentSchemaVersion)
        {
            throw new NotSupportedException($"Replay schema {save.SchemaVersion} is not supported.");
        }
        if (save.SavedAtTick < 0) throw new ArgumentOutOfRangeException(nameof(save.SavedAtTick));

        var world = new DishStationWorld(save.Seed, save.Scenario) { replaying = true };
        var invocationIndex = 0;
        ReplayInvocationsAtCurrentTick(world, save.CommandInvocations, ref invocationIndex);
        while (world.Tick.Value < save.SavedAtTick)
        {
            world.Advance();
            ReplayInvocationsAtCurrentTick(world, save.CommandInvocations, ref invocationIndex);
        }
        if (world.Tick.Value != save.SavedAtTick || invocationIndex != save.CommandInvocations.Length)
        {
            throw new InvalidOperationException("Replay command chronology does not match its saved tick.");
        }

        world.replaying = false;
        world.commandJournal.Clear();
        world.commandJournal.AddRange(save.CommandInvocations);
        return world;
    }

    private static void ReplayInvocationsAtCurrentTick(DishStationWorld world, RecordedCommandInvocation[] invocations, ref int index)
    {
        while (index < invocations.Length && invocations[index].InvokedAtTick == world.Tick.Value)
        {
            var invocation = invocations[index++];
            var command = invocation.Command.ToCommand();
            if (invocation.Mode == CommandInvocationMode.Scheduled) world.Schedule(command);
            else world.ExecuteNow(command);
        }
        if (index < invocations.Length && invocations[index].InvokedAtTick < world.Tick.Value)
        {
            throw new InvalidOperationException("Replay command invocations are not ordered by simulation tick.");
        }
    }

    private void RecordCommandInvocation(ISimulationCommand command, CommandInvocationMode mode)
    {
        if (replaying) return;
        commandJournal.Add(new(Tick.Value, mode, RecordedSimulationCommand.FromCommand(command)));
    }

    public void Advance()
    {
        Tick += 1;
        ExpireIncidents();
        if (!tutorialStarted)
        {
            tutorialStarted = true;
            Notify("Clock In", "Avery needs one clean plate before service opens. Ray points you toward the dirty landing.");
        }

        while (pendingCommands.TryPeek(out var command) && command.ExecuteAtTick.Value <= Tick.Value)
        {
            pendingCommands.Dequeue();
            var result = Execute(command);
            if (!result.Success)
            {
                Notify("Action blocked", result.Message);
            }
        }

        if (WasherRunning && Tick.Value >= washerCompletesAt.Value)
        {
            WasherRunning = false;
            Move(DishState.Washing, DishState.WashedInMachine, washingKind, DishTransitionCause.WasherCycle);
            Notify("Cycle complete", $"The {washingKind.ToString().ToLowerInvariant()} is clean but still in the washer. Unload it before starting another cycle.");
        }

        AdvanceNewHire();
        AdvanceAutomation();

        if (EffectiveRushEnabled && Tick.Value % EffectiveDemandIntervalTicks == 0)
        {
            ConsumeForService(EffectiveDemandKind);
        }

        AdvanceShiftTrial();

        if (Tick.Value % Configuration.ArrivalIntervalTicks == 0)
        {
            var kind = --arrivalsUntilGlass <= 0 ? DishKind.Glass : DishKind.Plate;
            if (kind == DishKind.Glass)
            {
                arrivalsUntilGlass = Configuration.GlassEveryArrivals;
            }

            Add(DishState.Dirty, kind);
        }

        SampleQueuePressure();
        UpdateProgression();
    }

    public DishStationSnapshot Snapshot()
    {
        var telemetry = new StageTelemetry[dishes.Length];
        for (var i = 0; i < telemetry.Length; i++)
        {
            telemetry[i] = new(
                plateItemTicks[i],
                glassItemTicks[i],
                trayItemTicks[i],
                maxQueueDepth[i],
                completedPlateResidenceTicks[i],
                completedGlassResidenceTicks[i],
                completedTrayResidenceTicks[i],
                completedPlateVisits[i],
                completedGlassVisits[i],
                completedTrayVisits[i],
                maxPlateResidenceTicks[i],
                maxGlassResidenceTicks[i],
                maxTrayResidenceTicks[i],
                OldestAge(plateStageEntries[i]),
                OldestAge(glassStageEntries[i]),
                OldestAge(trayStageEntries[i]));
        }

        return new(
            Tick,
            dishes.ToArray(),
            telemetry,
            FindBottleneck(),
            WasherRunning,
            WasherOccupied,
            EffectiveRushEnabled,
            Completed,
            ServiceShortages,
            TutorialStage,
            BottleneckHypothesis,
            CaptureNewHireSnapshot(),
            CaptureLayoutSnapshot(),
            CaptureAutomationSnapshot(),
            new(activeIncidentSnapshot, incidentTraceSnapshot),
            CaptureProcessSnapshot(),
            CaptureOnboardingSnapshot(),
            CaptureShiftTrialSnapshot(),
            CaptureEconomySnapshot(),
            shiftReport,
            CaptureProgressionSnapshot(),
            dishTransitionSnapshot,
            narrativeEvents.ToArray(),
            notifications.Count == 0 ? null : notifications[^1]);
    }

    private CommandResult Execute(ISimulationCommand command)
    {
        var result = command switch
        {
            CompleteIntroCommand intro => CompleteIntro(intro.GuidanceMode, intro.ReducedMotion, intro.HighContrast),
            PerformDishActionCommand action => Perform(action.Action, action.Kind),
            SetRushCommand rush => SetRush(rush.Enabled),
            AddDirtyDishesCommand add when add.Count > 0 => AddDirty(add.Kind, add.Count),
            AddDirtyDishesCommand => CommandResult.Rejected("Count must be positive."),
            ConfigureDishSupplyCommand configure => ConfigureSupply(configure.State, configure.Kind, configure.Count),
            ResetDishStationCommand => Reset(),
            InspectProcessCommand => InspectProcess(),
            ConfirmBottleneckCommand confirm => ConfirmBottleneck(confirm.Hypothesis),
            ConfigureDishStationLayoutCommand configure => ConfigureLayout(configure.Layout),
            PlaceDishStationFixtureCommand place => PlaceFixture(place.Fixture, place.Cell),
            MovePlayerCommand move => MovePlayer(move.Destination),
            InteractWithDishStationFixtureCommand interact => InteractWithFixture(interact.Fixture, interact.Kind),
            InspectDishStationFixtureCommand inspect => InspectFixture(inspect.Fixture, inspect.Kind),
            SetNewHireEnabledCommand worker => SetNewHireEnabled(worker.Enabled),
            TrainNewHireCommand training => TrainNewHire(training.Specification),
            ConfigureWasherAutomationCommand automation => ConfigureAutomation(automation.Policy),
            InspectAutomationIncidentCommand => InspectAutomationIncident(),
            ReplayAutomationIncidentCommand => ReplayAutomationIncident(),
            BeginAutomationRuleEditCommand => BeginAutomationRuleEdit(),
            SetAutomationRuleEnabledCommand enabled => SetAutomationRuleEnabled(enabled.Enabled),
            ToggleAutomationRuleConditionCommand condition => ToggleAutomationRuleCondition(condition.Observable),
            SetAutomationRuleActionCommand action => SetAutomationRuleAction(action.Action),
            ApplyAutomationRuleEditCommand => ApplyAutomationRuleEdit(),
            DiscardAutomationRuleEditCommand => DiscardAutomationRuleEdit(),
            SaveAutomationRulePresetCommand preset => SaveAutomationRulePreset(preset.Slot),
            RunAutomationRuleComparisonCommand comparison => RunAutomationRuleComparison(comparison.HorizonTicks),
            StartShiftTrialCommand => StartShiftTrial(),
            InjectStickyReadyFaultCommand => InjectStickyReadyFault(),
            TriggerDishStationIncidentCommand incident => TriggerIncident(incident.Incident),
            StartProcessCaptureCommand capture => StartProcessCapture(capture.Name),
            CompleteProcessCaptureCommand => CompleteProcessCapture(),
            BeginProcessEditCommand edit => BeginProcessEdit(edit.ArtifactId),
            MoveProcessStepCommand move => MoveProcessStep(move.StepId, move.Offset),
            AssignProcessStepCommand assign => AssignProcessStep(assign.StepId, assign.Actor),
            SetProcessRoutingPolicyCommand routing => SetProcessRoutingPolicy(routing.Policy),
            ApplyProcessEditCommand => ApplyProcessEdit(),
            DiscardProcessEditCommand => DiscardProcessEdit(),
            _ => CommandResult.Rejected($"Unknown command {command.GetType().Name}."),
        };
        if (result.Success) UpdateProgression();
        return result;
    }

    private CommandResult Perform(DishAction action, DishKind kind, DishTransitionCause cause = DishTransitionCause.PlayerWork)
    {
        var performedByNewHire = cause == DishTransitionCause.NewHireWork;
        if (cause == DishTransitionCause.PlayerWork)
        {
            var topology = Topology;
            var interactionPort = topology.InteractionPort(FixtureFor(action));
            var path = topology.FindPath(playerCell, interactionPort);
            if (path.Length == 0) return CommandResult.Rejected("No walkable route reaches that workstation.");
            sandboxMovementSteps += path.Length - 1;
            playerCell = interactionPort;
        }
        var source = DishStationRules.RequiredState(action);
        if (At(source).For(kind) <= 0)
        {
            return CommandResult.Rejected($"No {kind.ToString().ToLowerInvariant()} is {source.ToString().ToLowerInvariant()}.");
        }

        if (action == DishAction.StartWasher)
        {
            if (incidentBlockedResource)
                return CommandResult.Rejected("The washer is unavailable during the active incident.");
            if (WasherOccupied)
            {
                return CommandResult.Rejected(WasherRunning ? "The washer is already running." : "Unload the clean dish before starting another cycle.");
            }

            RecordTravel(action, cause);
            Move(source, DishState.Washing, kind, cause);
            WasherRunning = true;
            washingKind = kind;
            washerCompletesAt = Tick + Configuration.WasherCycleTicks + incidentProcessDelayTicks;
            Notify("Washer started", "The cycle is underway. Watch what waits while the machine is occupied.");
            CapturePlayerStep(action, kind, source, DishState.Washing, cause);
            if (cause == DishTransitionCause.PlayerWork) playerWorkActions++;
            return CommandResult.Accepted("Washer started.");
        }

        if (action == DishAction.Rack && At(DishState.Racked).Total >= EffectiveRackCapacity)
        {
            return CommandResult.Rejected($"The rack is at its {EffectiveRackCapacity}-dish capacity.");
        }

        var destination = DishStationRules.ResultState(action);
        RecordTravel(action, cause);
        Move(source, destination, kind, cause);
        if (action == DishAction.DryAndRestock)
        {
            Completed++;
            if (performedByNewHire && kind == DishKind.Glass) workerDeliveredGlass = true;
            if (!performedByNewHire) Notify("Dish available", $"One clean {kind.ToString().ToLowerInvariant()} returned to service.");
            if (TutorialStage == DishTutorialStage.RestockFirstDish)
            {
                TutorialStage = DishTutorialStage.EnableDinnerRush;
                Notify("Dinner is next", "Avery has the first plate. Let Tessa open dinner service and watch which supply runs short.");
            }
            else if (kind == LastShortageKind && TutorialStage == DishTutorialStage.ValidateBottleneck)
            {
                validatedRouteSteps = playerTravelSteps - layoutComparisonStartSteps;
                TutorialStage = DishTutorialStage.AwaitValidationDemand;
                Notify("Layout evidence", $"The baseline route cost {baselineRouteSteps} steps; the flow-cell route cost {validatedRouteSteps}. A clean {kind.ToString().ToLowerInvariant()} reached service—now watch whether demand consumes it.");
            }
            else if (performedByNewHire && kind == DishKind.Tray && TutorialStage == DishTutorialStage.ValidateRareTray)
            {
                TutorialStage = DishTutorialStage.OfferAutomation;
                Notify("Rare tray validated", "Jules returned the uncommon tray without rework. Avery is ready to approve a washer-start rule.");
            }
        }
        else if (!performedByNewHire && action == DishAction.Scrape)
        {
            Notify("Work has state", "The dish changed because of your action. Move it to the rack station next.");
        }
        else if (!performedByNewHire && action == DishAction.Rack)
        {
            Notify("Ready for the machine", "The rack is staged. Start the washer and watch the machine become occupied.");
        }
        else if (!performedByNewHire && action == DishAction.Unload)
        {
            Notify("Drying area", "The wet dish is out of the machine. Dry and restock it next.");
        }

        CapturePlayerStep(action, kind, source, destination, cause);
        if (cause == DishTransitionCause.PlayerWork) playerWorkActions++;
        return CommandResult.Accepted($"{action} completed.");
    }

    private CommandResult SetRush(bool enabled)
    {
        RushEnabled = enabled;
        if (enabled && TutorialStage == DishTutorialStage.EnableDinnerRush)
        {
            TutorialStage = DishTutorialStage.AwaitServiceShortage;
        }
        Notify(enabled ? "Dinner service" : "Service paused", enabled
            ? $"Tessa now requests clean {DishPlural(Configuration.DemandKind).ToLowerInvariant()} on the dinner cadence. Watch the item service actually needs."
            : "Tessa has paused new requests; work already in the station remains.");
        return CommandResult.Accepted(enabled ? "Rush enabled." : "Rush disabled.");
    }

    private CommandResult AddDirty(DishKind kind, int count)
    {
        dishes[(int)DishState.Dirty] = dishes[(int)DishState.Dirty].Add(kind, count);
        TrackEntries(DishState.Dirty, kind, count);
        Notify("God mode", $"Added {count} dirty {kind.ToString().ToLowerInvariant()}(s).");
        return CommandResult.Accepted("Dishes added.");
    }

    private CommandResult ConfigureSupply(DishState state, DishKind kind, int count)
    {
        if (state is not (DishState.Dirty or DishState.Available))
        {
            return CommandResult.Rejected("God setup can directly configure only dirty return or available service supply.");
        }

        if (count < 0)
        {
            return CommandResult.Rejected("Count cannot be negative.");
        }

        var current = dishes[(int)state];
        dishes[(int)state] = kind switch
        {
            DishKind.Plate => current with { Plates = count },
            DishKind.Glass => current with { Glasses = count },
            DishKind.Tray => current with { Trays = count },
            _ => current,
        };
        SetTrackedCount(state, kind, count);
        Notify("God mode", $"Set {state.ToString().ToLowerInvariant()} {kind.ToString().ToLowerInvariant()} supply to {count}.");
        return CommandResult.Accepted("Supply configured.");
    }

    private CommandResult Reset()
    {
        pendingCommands.Clear();
        commandJournal.Clear();
        notifications.Clear();
        narrativeEvents.Clear();
        Array.Clear(dishes);
        Array.Clear(plateItemTicks);
        Array.Clear(glassItemTicks);
        Array.Clear(trayItemTicks);
        Array.Clear(maxQueueDepth);
        Array.Clear(completedPlateResidenceTicks);
        Array.Clear(completedGlassResidenceTicks);
        Array.Clear(completedTrayResidenceTicks);
        Array.Clear(completedPlateVisits);
        Array.Clear(completedGlassVisits);
        Array.Clear(completedTrayVisits);
        Array.Clear(maxPlateResidenceTicks);
        Array.Clear(maxGlassResidenceTicks);
        Array.Clear(maxTrayResidenceTicks);
        ClearStageEntries();
        Tick = new(0);
        RushEnabled = false;
        WasherRunning = false;
        washerCompletesAt = new(0);
        washingKind = default;
        Completed = 0;
        ServiceShortages = 0;
        TutorialStage = DishTutorialStage.RestockFirstDish;
        BottleneckHypothesis = null;
        LastShortageKind = DishKind.Glass;
        newHireSpecification = default;
        newHireActsAt = new(0);
        newHireLastAction = null;
        newHireLastKind = null;
        newHireActionsCompleted = 0;
        newHirePlateActions = 0;
        newHireGlassActions = 0;
        newHireTrayActions = 0;
        trayReworkIncidents = 0;
        playerWorkActions = 0;
        staffedTicks = 0;
        flowCellInvestmentPurchased = false;
        newHireEnabled = false;
        omittedPriorityObserved = false;
        workerDeliveredGlass = false;
        layout = DishStationLayout.Linear;
        placements = DishStationPlacements.Linear;
        playerCell = Topology.InteractionPort(DishStationFixture.Scrape);
        sandboxMovementSteps = 0;
        playerTravelSteps = 0;
        newHireTravelSteps = 0;
        baselineRouteSteps = 0;
        layoutComparisonStartSteps = 0;
        validatedRouteSteps = 0;
        automationPolicy = default;
        activeAutomationRule = DishStationAutomationRules.ForPolicy(WasherAutomationPolicy.Off);
        activeAutomationRuleEdit = null;
        automationBaselinePreset = null;
        automationVariantPreset = null;
        latestAutomationComparison = null;
        automationHalted = false;
        stickyReadySignal = false;
        automatedStarts = 0;
        automationIncidents = 0;
        preventedUnsafeStarts = 0;
        safetyBlockActive = false;
        automationTrace.Clear();
        automationTraceSnapshot = [];
        automationRuleTrace.Clear();
        automationRuleTraceSnapshot = [];
        automationIncident = null;
        automationReplayCount = 0;
        automationHasReplay = false;
        lastReplayPolicy = default;
        lastReplayWouldStart = false;
        automationRegressionPassed = false;
        activeIncidents.Clear();
        incidentTrace.Clear();
        activeIncidentSnapshot = [];
        incidentTraceSnapshot = [];
        incidentProcessDelayTicks = 0;
        incidentCapacityLoss = 0;
        incidentBadSensor = false;
        incidentBlockedResource = false;
        incidentWorkerAbsent = false;
        incidentDemandSpike = false;
        incidentDemandKind = default;
        incidentDemandIntervalTicks = 0;
        nextProcessCaptureId = 1;
        nextProcessArtifactId = 1;
        activeProcessCapture = null;
        processArtifacts.Clear();
        processCaptureEvents.Clear();
        processArtifactSnapshot = [];
        processCaptureEventSnapshot = [];
        activeProcessEdit = null;
        appliedProcessArtifactId = null;
        introComplete = false;
        guidanceMode = GuidanceMode.Guided;
        reducedMotion = false;
        highContrast = false;
        Array.Clear(completedQuests);
        careerExperience = 0;
        careerStartedAtTick = -1;
        Array.Fill(questStartedAtTicks, -1);
        Array.Fill(questCompletedAtTicks, -1);
        shiftTrialStatus = ShiftTrialStatus.NotStarted;
        shiftTrialSuccessfulDemandChecks = 0;
        shiftTrialAttempts = 0;
        shiftTrialBaselineShortages = 0;
        shiftTrialBaselineAutomationIncidents = 0;
        shiftTrialStartedAtTick = -1;
        shiftTrialCompletedAtTick = -1;
        shiftReport = default;
        dishTransitions.Clear();
        dishTransitionSnapshot = [];
        tutorialStarted = false;
        ApplyScenarioStart();
        if (!replaying)
        {
            commandJournal.Add(new(0, CommandInvocationMode.Immediate, RecordedSimulationCommand.FromCommand(new ResetDishStationCommand(new(0)))));
        }
        Notify("Scenario reset", "Dish station restored to the Clock In starting state.");
        return CommandResult.Accepted("Scenario reset.");
    }

    private void ApplyScenarioStart()
    {
        dishes[(int)DishState.Dirty] = Configuration.InitialDirty;
        dishes[(int)DishState.Available] = Configuration.InitialAvailable;
        TrackConfiguredCounts(DishState.Dirty, Configuration.InitialDirty);
        TrackConfiguredCounts(DishState.Available, Configuration.InitialAvailable);
        RushEnabled = Configuration.InitialRushEnabled;
        LastShortageKind = Configuration.DemandKind;
        layout = Configuration.InitialLayout;
        placements = PlacementsFor(layout);
        flowCellInvestmentPurchased = layout == DishStationLayout.UShapedCell;
        playerCell = Topology.InteractionPort(DishStationFixture.Scrape);
        newHireEnabled = Configuration.InitialNewHireEnabled;
        newHireSpecification = Configuration.InitialNewHireSpecification;
        newHireActsAt = newHireEnabled ? Tick + 1 : new(0);
        automationPolicy = Configuration.InitialAutomationPolicy;
        activeAutomationRule = DishStationAutomationRules.ForPolicy(automationPolicy);
        activeAutomationRuleEdit = null;
        arrivalsUntilGlass = Math.Min(Configuration.GlassEveryArrivals, 2 + Math.Abs(Seed % Configuration.GlassEveryArrivals));
        faultRandomState = unchecked((uint)Seed * 747_796_405u + 2_891_336_453u);
        if (faultRandomState == 0) faultRandomState = 0x9E3779B9u;
    }

    private void TrackConfiguredCounts(DishState state, DishCounts counts)
    {
        TrackEntries(state, DishKind.Plate, counts.Plates);
        TrackEntries(state, DishKind.Glass, counts.Glasses);
        TrackEntries(state, DishKind.Tray, counts.Trays);
    }

    private void ConsumeForService(DishKind kind)
    {
        if (At(DishState.Available).For(kind) > 0)
        {
            Move(DishState.Available, DishState.Dirty, kind, DishTransitionCause.ServiceDemand);
            if (shiftTrialStatus == ShiftTrialStatus.Running) shiftTrialSuccessfulDemandChecks++;
            if (kind == LastShortageKind && TutorialStage == DishTutorialStage.AwaitValidationDemand)
            {
                TutorialStage = DishTutorialStage.InviteNewHire;
                Notify("Hypothesis supported", $"Tessa received the {kind.ToString().ToLowerInvariant()} from the shorter route. Avery is sending Jules to help; decide what knowledge must travel with the work.");
            }
            else if (kind == DishKind.Glass && workerDeliveredGlass && TutorialStage == DishTutorialStage.ValidateDelegation)
            {
                TutorialStage = DishTutorialStage.ObserveRareTray;
                Add(DishState.Dirty, DishKind.Tray);
                Notify("The Rare Tray", "Jules restored glass service. Ray notices an uncommon tray arriving; watch whether the shared process covers what he knows.");
            }
            return;
        }

        ServiceShortages++;
        LastShortageKind = kind;
        if (TutorialStage == DishTutorialStage.AwaitServiceShortage)
        {
            TutorialStage = DishTutorialStage.InspectShortage;
            Narrate(DishStationNarrativeEventKind.QueuePressure, DishStationQuestId.FindTheConstraint);
            Notify($"Where Did the {DishPlural(kind)} Go?", $"Tessa is out of clean {DishPlural(kind).ToLowerInvariant()}. Inspect where that work is waiting before changing the station.");
        }
        else if (TutorialStage == DishTutorialStage.ObserveNewHire)
        {
            TutorialStage = DishTutorialStage.DocumentGlassPriority;
            Notify("Specification gap", "Jules followed the shared flow exactly, but Tessa still waited for glasses. Ray's rush priority was never transferred.");
        }
        else
        {
            Notify("Service is waiting", $"Tessa has no clean {kind.ToString().ToLowerInvariant()}. Check where that work is waiting.");
        }
    }

    private void Move(DishState from, DishState to, DishKind kind, DishTransitionCause cause)
    {
        TrackDeparture(from, kind);
        dishes[(int)from] = dishes[(int)from].Remove(kind);
        dishes[(int)to] = dishes[(int)to].Add(kind);
        TrackEntries(to, kind, 1);
        if (dishTransitions.Count == 24) dishTransitions.RemoveAt(0);
        dishTransitions.Add(new(Tick, kind, from, to, cause));
        dishTransitionSnapshot = dishTransitions.ToArray();
    }

    private void Add(DishState state, DishKind kind)
    {
        dishes[(int)state] = dishes[(int)state].Add(kind);
        TrackEntries(state, kind, 1);
    }

    private void SampleQueuePressure()
    {
        for (var i = 0; i < dishes.Length; i++)
        {
            var counts = dishes[i];
            plateItemTicks[i] += counts.Plates;
            glassItemTicks[i] += counts.Glasses;
            trayItemTicks[i] += counts.Trays;
            maxQueueDepth[i] = Math.Max(maxQueueDepth[i], counts.Total);
        }
    }

    private DishState? FindBottleneck()
    {
        DishState? candidate = null;
        long highestPressure = 0;
        foreach (var state in Enum.GetValues<DishState>())
        {
            if (state == DishState.Available) continue;
            var index = (int)state;
            var pressure = plateItemTicks[index] + glassItemTicks[index] + trayItemTicks[index];
            if (pressure <= highestPressure) continue;
            highestPressure = pressure;
            candidate = state;
        }

        return candidate;
    }

    private DishState? FindConstraintFor(DishKind kind)
    {
        DishState? candidate = null;
        long greatestOldestAge = -1;
        long greatestPressure = -1;
        foreach (var state in Enum.GetValues<DishState>())
        {
            if (state == DishState.Available || At(state).For(kind) <= 0) continue;
            var index = (int)state;
            var oldestAge = OldestAge(Entries(state, kind));
            var pressure = kind switch
            {
                DishKind.Plate => plateItemTicks[index],
                DishKind.Glass => glassItemTicks[index],
                DishKind.Tray => trayItemTicks[index],
                _ => 0,
            };
            if (oldestAge < greatestOldestAge || (oldestAge == greatestOldestAge && pressure <= greatestPressure)) continue;
            greatestOldestAge = oldestAge;
            greatestPressure = pressure;
            candidate = state;
        }

        return candidate;
    }

    private CommandResult InspectProcess()
    {
        if (TutorialStage == DishTutorialStage.InspectShortage)
        {
            TutorialStage = DishTutorialStage.ChooseBottleneck;
            var leader = FindBottleneck()?.ToString() ?? "no queue";
            Notify("Form a hypothesis", $"The strongest waiting signal is at {leader}. Choose the workstation you believe is holding back {LastShortageKind.ToString().ToLowerInvariant()} flow.");
            return CommandResult.Accepted("Shortage evidence inspected; choose a bottleneck hypothesis.");
        }

        Notify("Observation recorded", "The process view preserves where work waited, how long it waited, and the deepest queue seen this shift.");
        return CommandResult.Accepted("Process inspected.");
    }

    private CommandResult ConfirmBottleneck(DishState hypothesis)
    {
        if (TutorialStage != DishTutorialStage.ChooseBottleneck)
        {
            return CommandResult.Rejected("Inspect a service shortage before confirming a bottleneck hypothesis.");
        }

        var constraint = FindConstraintFor(LastShortageKind);
        if (constraint != hypothesis)
        {
            Notify("Hypothesis not supported", $"The oldest waiting {LastShortageKind.ToString().ToLowerInvariant()} is not at {hypothesis}. Compare age and queue pressure, then try again.");
            return CommandResult.Rejected("Current evidence does not support that workstation.");
        }

        BottleneckHypothesis = hypothesis;
        baselineRouteSteps = playerTravelSteps;
        TutorialStage = DishTutorialStage.ImproveLayout;
        Notify("Improve the route", $"The evidence supports {hypothesis}. Ray walked {baselineRouteSteps} steps through the first complete route; shorten the handoffs, then send one {LastShortageKind.ToString().ToLowerInvariant()} through again.");
        return CommandResult.Accepted("Hypothesis accepted for validation.");
    }

    private CommandResult ConfigureLayout(DishStationLayout requestedLayout)
    {
        if (requestedLayout == DishStationLayout.Custom)
            return CommandResult.Rejected("Custom layouts are created by placing individual fixtures.");
        var requestedPlacements = PlacementsFor(requestedLayout);
        var requestedTopology = new DishStationTopology(requestedPlacements);
        if (!requestedTopology.AllInteractionPortsConnected())
            return CommandResult.Rejected("That layout disconnects one or more workstation interaction ports.");
        layout = requestedLayout;
        placements = requestedPlacements;
        if (requestedLayout == DishStationLayout.UShapedCell) flowCellInvestmentPurchased = true;
        playerCell = requestedTopology.ResolveWalkable(playerCell);
        if (TutorialStage == DishTutorialStage.ImproveLayout && requestedLayout == DishStationLayout.UShapedCell)
        {
            layoutComparisonStartSteps = playerTravelSteps;
            validatedRouteSteps = 0;
            TutorialStage = DishTutorialStage.ValidateBottleneck;
            Notify("Flow cell arranged", "Dirty landing, scrape, rack, washer, and return now form a shorter U-shaped route. Run the same dish states again and compare handling effort.");
        }
        else
        {
            Notify("Layout configured", requestedLayout == DishStationLayout.UShapedCell
                ? "U-shaped flow cell enabled."
                : "Linear station layout restored.");
        }

        return CommandResult.Accepted("Dish-station layout configured.");
    }

    private CommandResult PlaceFixture(DishStationFixture fixture, FloorCell cell)
    {
        if (!cell.IsInsideDishStation)
            return CommandResult.Rejected($"{cell.X},{cell.Y} is outside the dish-station floor.");
        if (placements.IsOccupied(cell, fixture))
            return CommandResult.Rejected("Another fixture already occupies that floor cell.");
        if (cell == playerCell)
            return CommandResult.Rejected("Move away before placing a fixture on that floor cell.");
        if ((fixture is DishStationFixture.Washer or DishStationFixture.Unload) && WasherOccupied)
            return CommandResult.Rejected("Unload the washer before relocating its work area.");

        var candidatePlacements = placements.With(fixture, cell);
        var candidateTopology = new DishStationTopology(candidatePlacements);
        if (!candidateTopology.AllInteractionPortsConnected())
            return CommandResult.Rejected("That placement disconnects one or more workstation interaction ports.");
        placements = candidatePlacements;
        layout = DishStationLayout.Custom;
        Notify("Layout changed", $"{FixtureLabel(fixture)} moved to {cell.X},{cell.Y}. The estimated handoff route is now {placements.EstimatedRouteSteps} steps.");
        return CommandResult.Accepted($"{FixtureLabel(fixture)} placed at {cell.X},{cell.Y}.");
    }

    private CommandResult MovePlayer(FloorCell destination)
    {
        if (!destination.IsInsideDishStation)
            return CommandResult.Rejected($"{destination.X},{destination.Y} is outside the dish-station floor.");
        if (destination == playerCell) return CommandResult.Accepted("Already at that floor cell.");
        var topology = Topology;
        if (!topology.IsWalkable(destination))
            return CommandResult.Rejected($"{destination.X},{destination.Y} is blocked by a workstation footprint.");
        if (!topology.CanStep(playerCell, destination))
            return CommandResult.Rejected("Movement must follow one unobstructed neighboring floor step.");
        playerCell = destination;
        sandboxMovementSteps++;
        return CommandResult.Accepted("Walked 1 floor step.");
    }

    public DishStationInteractionState InteractionAt(DishStationFixture fixture, DishKind kind)
    {
        if (!Enum.IsDefined(fixture)) throw new ArgumentOutOfRangeException(nameof(fixture));
        var cell = Topology.InteractionPort(fixture);
        var distance = playerCell.DistanceTo(cell);
        if (distance != 0)
            return new(fixture, cell, distance, ActionForFixture(fixture), null, 0, DishStationInteractionBlockReason.MoveCloser);
        if (fixture == DishStationFixture.Service)
            return new(fixture, cell, distance, null, null, At(DishState.Available).For(kind), DishStationInteractionBlockReason.InspectionOnly);

        var action = ActionForFixture(fixture)!.Value;
        var required = DishStationRules.RequiredState(action);
        var count = At(required).For(kind);
        if (count <= 0)
            return new(fixture, cell, distance, action, required, count, DishStationInteractionBlockReason.NoDishReady);
        if (action == DishAction.StartWasher && WasherOccupied)
            return new(fixture, cell, distance, action, required, count,
                WasherRunning ? DishStationInteractionBlockReason.WasherRunning : DishStationInteractionBlockReason.WasherNeedsUnload);
        if (action == DishAction.Rack && At(DishState.Racked).Total >= EffectiveRackCapacity)
            return new(fixture, cell, distance, action, required, count, DishStationInteractionBlockReason.RackFull);
        return new(fixture, cell, distance, action, required, count, DishStationInteractionBlockReason.None);
    }

    private CommandResult InteractWithFixture(DishStationFixture fixture, DishKind kind)
    {
        if (!Enum.IsDefined(fixture)) return CommandResult.Rejected("That fixture does not exist.");
        var interaction = InteractionAt(fixture, kind);
        if (!interaction.CanWork) return CommandResult.Rejected(InteractionBlockedMessage(interaction, kind));
        return Perform(interaction.WorkAction!.Value, kind);
    }

    private CommandResult InspectFixture(DishStationFixture fixture, DishKind kind)
    {
        if (!Enum.IsDefined(fixture)) return CommandResult.Rejected("That fixture does not exist.");
        var interaction = InteractionAt(fixture, kind);
        if (!interaction.CanInspect)
            return CommandResult.Rejected($"Move {interaction.Distance} floor step{(interaction.Distance == 1 ? "" : "s")} closer to inspect {FixtureLabel(fixture).ToLowerInvariant()}.");
        if (fixture == DishStationFixture.Service)
        {
            var available = At(DishState.Available);
            return CommandResult.Accepted($"Service supply: P{available.Plates} G{available.Glasses} T{available.Trays}; {ServiceShortages} shortages.");
        }

        var required = interaction.RequiredState!.Value;
        var counts = At(required);
        var readiness = interaction.CanWork ? $"{kind} is ready for work." : InteractionBlockedMessage(interaction, kind);
        return CommandResult.Accepted($"{FixtureLabel(fixture)}: {required} P{counts.Plates} G{counts.Glasses} T{counts.Trays}. {readiness}");
    }

    private static string InteractionBlockedMessage(DishStationInteractionState interaction, DishKind kind) => interaction.WorkBlockReason switch
    {
        DishStationInteractionBlockReason.MoveCloser => $"Move {interaction.Distance} floor step{(interaction.Distance == 1 ? "" : "s")} closer to {FixtureLabel(interaction.Fixture).ToLowerInvariant()}.",
        DishStationInteractionBlockReason.InspectionOnly => "Service has no workstation action; inspect its supply instead.",
        DishStationInteractionBlockReason.NoDishReady => $"No {kind.ToString().ToLowerInvariant()} is {interaction.RequiredState!.Value.ToString().ToLowerInvariant()}.",
        DishStationInteractionBlockReason.RackFull => "The rack is at capacity.",
        DishStationInteractionBlockReason.WasherRunning => "The washer is already running.",
        DishStationInteractionBlockReason.WasherNeedsUnload => "Unload the clean dish before starting another cycle.",
        _ => "Work is available.",
    };

    private CommandResult SetNewHireEnabled(bool enabled)
    {
        newHireEnabled = enabled;
        if (enabled)
        {
            newHireActsAt = Tick + 1;
            if (TutorialStage == DishTutorialStage.InviteNewHire)
            {
                TutorialStage = DishTutorialStage.TrainNewHire;
                Notify("Jules joins the station", "Jules is ready to help but knows only what the crew shares. Transfer the basic dish flow before handing over work.");
            }
            else
            {
                Notify("New hire enabled", "The delegated worker is available.");
            }
        }
        else
        {
            Notify("New hire paused", "Delegated work is paused; existing world state is unchanged.");
        }

        return CommandResult.Accepted(enabled ? "New hire enabled." : "New hire paused.");
    }

    private CommandResult TrainNewHire(DishProcessSpecification specification)
    {
        if (!newHireEnabled)
        {
            return CommandResult.Rejected("Enable the new hire before transferring a process.");
        }

        if (!specification.FlowDocumented)
        {
            return CommandResult.Rejected("The worker needs an explicit dish-flow procedure.");
        }

        newHireSpecification = specification;
        newHireActsAt = Tick + 1;
        if (TutorialStage == DishTutorialStage.TrainNewHire)
        {
            TutorialStage = specification.RushGlassPriorityDocumented
                ? DishTutorialStage.ValidateDelegation
                : DishTutorialStage.ObserveNewHire;
            Notify("Process transferred", specification.RushGlassPriorityDocumented
                ? "Jules now has the basic flow and Ray's rush-glass priority. Watch whether Tessa receives what she needs."
                : "Jules now has the basic dish flow. Watch what happens when dinner demand changes the priority.");
        }
        else if (TutorialStage == DishTutorialStage.DocumentGlassPriority && specification.RushGlassPriorityDocumented)
        {
            TutorialStage = DishTutorialStage.ValidateDelegation;
            workerDeliveredGlass = false;
            Notify("Knowledge made explicit", "Ray's rush-glass priority is now shared with Jules. Watch whether the next choice serves Tessa sooner.");
        }
        else if (TutorialStage == DishTutorialStage.DocumentRareTray && specification.RareTrayHandlingDocumented)
        {
            TutorialStage = DishTutorialStage.ValidateRareTray;
            Notify("Rare knowledge captured", "Ray's uncommon-tray orientation is now part of the shared work. Let Jules retry it.");
        }
        else
        {
            Notify("Training updated", specification.RushGlassPriorityDocumented
                ? "The new hire now knows the rush glass-priority rule."
                : "The new hire knows only the happy-path dish flow.");
        }

        return CommandResult.Accepted("New-hire process updated.");
    }

    private void AdvanceNewHire()
    {
        if (newHireEnabled) staffedTicks++;
        var appliedProcess = AppliedProcessVersion();
        if (!newHireEnabled || incidentWorkerAbsent ||
            (appliedProcess is null && !newHireSpecification.FlowDocumented) || Tick.Value < newHireActsAt.Value) return;
        newHireActsAt = Tick + WorkerIntervalForCurrentLayout();

        var glassHasWork = HasProcessWork(DishKind.Glass);
        DishKind? workedKind = null;
        if ((TutorialStage is DishTutorialStage.ObserveRareTray or DishTutorialStage.ValidateRareTray) && HasProcessWork(DishKind.Tray) && TryPerformNewHire(DishKind.Tray))
        {
            workedKind = DishKind.Tray;
        }
        else
        {
            var preferGlass = appliedProcess?.RoutingPolicy switch
            {
                ProcessRoutingPolicy.GlassesFirst => glassHasWork,
                ProcessRoutingPolicy.PlatesFirst => false,
                _ => EffectiveRushEnabled && newHireSpecification.RushGlassPriorityDocumented && glassHasWork,
            };
            var primary = preferGlass ? DishKind.Glass : DishKind.Plate;
            var secondary = preferGlass ? DishKind.Plate : DishKind.Glass;
            workedKind = TryPerformNewHire(primary) ? primary : TryPerformNewHire(secondary) ? secondary : (DishKind?)null;
        }
        if (workedKind is null) return;

        if (EffectiveRushEnabled && !newHireSpecification.RushGlassPriorityDocumented && workedKind == DishKind.Plate && glassHasWork && !omittedPriorityObserved)
        {
            omittedPriorityObserved = true;
            Notify("Observed behavior", "Jules chose a plate while Tessa waited for glasses. The shared flow was followed; Ray's rush priority was absent.");
        }
    }

    private bool TryPerformNewHire(DishKind kind)
    {
        DishAction? action = null;
        if (At(DishState.CleanWet).For(kind) > 0) action = DishAction.DryAndRestock;
        else if (At(DishState.WashedInMachine).For(kind) > 0) action = DishAction.Unload;
        else if (At(DishState.Racked).For(kind) > 0 && WasherPhysicalReady && !automationPolicy.Enabled) action = DishAction.StartWasher;
        else if (At(DishState.Scraped).For(kind) > 0) action = DishAction.Rack;
        else if (At(DishState.Dirty).For(kind) > 0) action = DishAction.Scrape;
        if (action is null) return false;
        var appliedProcess = AppliedProcessVersion();
        if (appliedProcess is not null && !IsAssignedToNewHire(appliedProcess, action.Value))
            return false;

        if (kind == DishKind.Tray && action == DishAction.Rack && !newHireSpecification.RareTrayHandlingDocumented)
        {
            RecordTravel(DishAction.Rack, DishTransitionCause.NewHireWork);
            Move(DishState.Scraped, DishState.Dirty, DishKind.Tray, DishTransitionCause.NewHireWork);
            newHireLastAction = action;
            newHireLastKind = kind;
            newHireActionsCompleted++;
            newHireTrayActions++;
            trayReworkIncidents++;
            if (TutorialStage == DishTutorialStage.ObserveRareTray)
            {
                TutorialStage = DishTutorialStage.DocumentRareTray;
                Notify("Rare tray rework", "Jules used the ordinary rack orientation. The uncommon tray returned dirty because Ray's exception was not in the shared process.");
            }
            return true;
        }

        var result = Perform(action.Value, kind, DishTransitionCause.NewHireWork);
        if (!result.Success) return false;
        newHireLastAction = action;
        newHireLastKind = kind;
        newHireActionsCompleted++;
        if (kind == DishKind.Plate) newHirePlateActions++;
        else if (kind == DishKind.Glass) newHireGlassActions++;
        else newHireTrayActions++;
        return true;
    }

    private PlayerProcessVersion? AppliedProcessVersion()
    {
        if (appliedProcessArtifactId is not { } id) return null;
        for (var index = 0; index < processArtifacts.Count; index++)
            if (processArtifacts[index].Id == id) return processArtifacts[index].Current;
        return null;
    }

    private bool IsAssignedToNewHire(PlayerProcessVersion process, DishAction action)
    {
        foreach (var step in process.Steps)
            if (step.Action == action && step.AssignedActor == newHireId) return true;
        return false;
    }

    private bool HasProcessWork(DishKind kind) =>
        At(DishState.Dirty).For(kind) > 0 ||
        At(DishState.Scraped).For(kind) > 0 ||
        At(DishState.Racked).For(kind) > 0 ||
        At(DishState.Washing).For(kind) > 0 ||
        At(DishState.WashedInMachine).For(kind) > 0 ||
        At(DishState.CleanWet).For(kind) > 0;

    private NewHireSnapshot CaptureNewHireSnapshot() => new(
        newHireId,
        newHireEnabled,
        newHireSpecification,
        newHireActionsCompleted,
        newHirePlateActions,
        newHireGlassActions,
        newHireTrayActions,
        trayReworkIncidents,
        newHireLastAction,
        newHireLastKind,
        omittedPriorityObserved);

    private DishStationLayoutSnapshot CaptureLayoutSnapshot() => new(
        layout,
        playerTravelSteps,
        newHireTravelSteps,
        baselineRouteSteps,
        validatedRouteSteps,
        placements,
        playerCell,
        sandboxMovementSteps,
        placements.EstimatedRouteSteps);

    private static DishStationPlacements PlacementsFor(DishStationLayout stationLayout) => stationLayout switch
    {
        DishStationLayout.UShapedCell => DishStationPlacements.UShapedCell,
        _ => DishStationPlacements.Linear,
    };

    private static DishStationFixture FixtureFor(DishAction action) => action switch
    {
        DishAction.Scrape => DishStationFixture.Scrape,
        DishAction.Rack => DishStationFixture.Rack,
        DishAction.StartWasher => DishStationFixture.Washer,
        DishAction.Unload => DishStationFixture.Unload,
        DishAction.DryAndRestock => DishStationFixture.DryRestock,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };

    private static DishAction? ActionForFixture(DishStationFixture fixture) => fixture switch
    {
        DishStationFixture.Scrape => DishAction.Scrape,
        DishStationFixture.Rack => DishAction.Rack,
        DishStationFixture.Washer => DishAction.StartWasher,
        DishStationFixture.Unload => DishAction.Unload,
        DishStationFixture.DryRestock => DishAction.DryAndRestock,
        DishStationFixture.Service => null,
        _ => throw new ArgumentOutOfRangeException(nameof(fixture)),
    };

    private static string FixtureLabel(DishStationFixture fixture) => fixture switch
    {
        DishStationFixture.DryRestock => "Dry and restock",
        _ => fixture.ToString(),
    };

    private void RecordTravel(DishAction action, DishTransitionCause cause)
    {
        var steps = TravelSteps(action, layout);
        if (cause == DishTransitionCause.PlayerWork) playerTravelSteps += steps;
        else if (cause == DishTransitionCause.NewHireWork) newHireTravelSteps += steps;
    }

    private int TravelSteps(DishAction action, DishStationLayout stationLayout) => stationLayout switch
    {
        DishStationLayout.Linear => action switch
        {
            DishAction.Scrape => 4,
            DishAction.Rack => 6,
            DishAction.StartWasher => 3,
            DishAction.Unload => 2,
            DishAction.DryAndRestock => 7,
            _ => 0,
        },
        DishStationLayout.UShapedCell => action switch
        {
            DishAction.Scrape => 2,
            DishAction.Rack => 3,
            DishAction.StartWasher => 2,
            DishAction.Unload => 1,
            DishAction.DryAndRestock => 2,
            _ => 0,
        },
        DishStationLayout.Custom => action switch
        {
            DishAction.Scrape => 0,
            DishAction.Rack => placements.Scrape.DistanceTo(placements.Rack),
            DishAction.StartWasher => placements.Rack.DistanceTo(placements.Washer),
            DishAction.Unload => placements.Washer.DistanceTo(placements.Unload),
            DishAction.DryAndRestock => placements.Unload.DistanceTo(placements.DryRestock) + placements.DryRestock.DistanceTo(placements.Service),
            _ => 0,
        },
        _ => 0,
    };

    private int WorkerIntervalForCurrentLayout()
    {
        if (layout == DishStationLayout.Linear) return Configuration.WorkerActionIntervalTicks;
        var excessRoute = Math.Max(0, placements.EstimatedRouteSteps - DishStationPlacements.UShapedCell.EstimatedRouteSteps);
        return Math.Min(Configuration.WorkerActionIntervalTicks, Configuration.FlowCellWorkerActionIntervalTicks + excessRoute / 2);
    }

    private CommandResult ConfigureAutomation(WasherAutomationPolicy policy)
    {
        automationPolicy = policy;
        activeAutomationRule = DishStationAutomationRules.ForPolicy(policy);
        activeAutomationRuleEdit = null;
        if (!policy.Enabled || policy.RequirePhysicalReady)
        {
            automationHalted = false;
        }
        RecordAutomationTrace(AutomationTraceOutcome.PolicyConfigured, null);

        if (TutorialStage == DishTutorialStage.OfferAutomation && policy.Enabled)
        {
            TutorialStage = DishTutorialStage.ObserveAutomation;
            Notify("Automatic Start", policy.RequirePhysicalReady
                ? "The controller requires both reported and physical readiness. Observe its operation."
                : "The controller now starts a present rack whenever the machine reports ready. Observe several cycles.");
        }
        else if (TutorialStage == DishTutorialStage.RefineAutomation && policy.Enabled && policy.RequirePhysicalReady)
        {
            TutorialStage = DishTutorialStage.ValidateAutomation;
            Notify("Rule refined", "Automatic start now corroborates the ready signal with physical machine state. Re-run the sticky-signal condition.");
        }
        else
        {
            Notify("Automation configured", policy.Enabled
                ? policy.RequirePhysicalReady ? "Corroborated-ready automation enabled." : "Reported-ready automation enabled."
                : "Automatic start disabled; manual fallback remains available.");
        }

        return CommandResult.Accepted("Washer automation configured.");
    }

    private CommandResult BeginAutomationRuleEdit()
    {
        if (activeAutomationRuleEdit is not null) return CommandResult.Rejected("An automation rule draft is already active.");
        activeAutomationRuleEdit = DishStationAutomationRuleEditor.Begin(activeAutomationRule);
        Notify("Automation draft opened", "Edit the washer rule's enabled state, readiness conditions, and Start Washer action, then validate and apply it.");
        return CommandResult.Accepted("Automation rule draft opened.");
    }

    private CommandResult SetAutomationRuleEnabled(bool enabled)
    {
        if (activeAutomationRuleEdit is not { } draft) return CommandResult.Rejected("No automation rule draft is active.");
        activeAutomationRuleEdit = DishStationAutomationRuleEditor.SetEnabled(draft, enabled);
        return CommandResult.Accepted(enabled ? "Draft enabled." : "Draft disabled.");
    }

    private CommandResult ToggleAutomationRuleCondition(AutomationObservable observable)
    {
        if (activeAutomationRuleEdit is not { } draft) return CommandResult.Rejected("No automation rule draft is active.");
        if (!Enum.IsDefined(observable)) return CommandResult.Rejected("Unknown automation observable.");
        activeAutomationRuleEdit = DishStationAutomationRuleEditor.ToggleCondition(draft, observable);
        return CommandResult.Accepted($"{observable} condition toggled.");
    }

    private CommandResult SetAutomationRuleAction(DishAction action)
    {
        if (activeAutomationRuleEdit is not { } draft) return CommandResult.Rejected("No automation rule draft is active.");
        activeAutomationRuleEdit = DishStationAutomationRuleEditor.SetAction(draft, action);
        return CommandResult.Accepted("Draft action updated.");
    }

    private CommandResult ApplyAutomationRuleEdit()
    {
        if (activeAutomationRuleEdit is not { } draft) return CommandResult.Rejected("No automation rule draft is active.");
        if (!draft.Diagnostics.IsDefaultOrEmpty) return CommandResult.Rejected(draft.Diagnostics[0].Message);

        activeAutomationRule = DishStationAutomationRuleEditor.Compile(draft);
        automationPolicy = DishStationAutomationRuleEditor.PolicyFor(activeAutomationRule);
        automationHalted = false;
        safetyBlockActive = false;
        activeAutomationRuleEdit = null;
        RecordAutomationTrace(AutomationTraceOutcome.PolicyConfigured, null);

        if (TutorialStage == DishTutorialStage.OfferAutomation && activeAutomationRule.Enabled)
        {
            TutorialStage = DishTutorialStage.ObserveAutomation;
            Notify("Player rule applied", "Your rule starts a present rack from the readiness conditions you selected. Observe its live trace.");
        }
        else if (TutorialStage == DishTutorialStage.RefineAutomation && activeAutomationRule.Enabled &&
                 automationPolicy.RequirePhysicalReady)
        {
            TutorialStage = DishTutorialStage.ValidateAutomation;
            Notify("Player rule refined", "Your rule now corroborates the reported signal with physical machine state. Re-run the sticky-signal condition.");
        }
        else
        {
            Notify("Player rule applied", activeAutomationRule.Enabled
                ? "The edited washer rule is active."
                : "The edited washer rule is disabled; manual fallback remains available.");
        }
        return CommandResult.Accepted("Automation rule applied.");
    }

    private CommandResult DiscardAutomationRuleEdit()
    {
        if (activeAutomationRuleEdit is null) return CommandResult.Rejected("No automation rule draft is active.");
        activeAutomationRuleEdit = null;
        return CommandResult.Accepted("Automation rule draft discarded.");
    }

    private CommandResult SaveAutomationRulePreset(AutomationPresetSlot slot)
    {
        if (!Enum.IsDefined(slot)) return CommandResult.Rejected("Unknown automation preset slot.");
        if (!activeAutomationRule.Enabled) return CommandResult.Rejected("Enable and apply a rule before saving a preset.");
        var preset = new AutomationRulePreset(slot, Tick, activeAutomationRule);
        if (slot == AutomationPresetSlot.Baseline) automationBaselinePreset = preset;
        else automationVariantPreset = preset;
        latestAutomationComparison = null;
        Notify($"{slot} preset saved", $"Saved applied rule {activeAutomationRule.Id} for controlled comparison.");
        return CommandResult.Accepted($"{slot} automation preset saved.");
    }

    private CommandResult RunAutomationRuleComparison(int horizonTicks)
    {
        if (automationBaselinePreset is not { } baseline) return CommandResult.Rejected("Save a baseline preset first.");
        if (automationVariantPreset is not { } variant) return CommandResult.Rejected("Save a variant preset first.");
        if (horizonTicks is < 1 or > AutomationPresetComparisonRunner.MaximumHorizonTicks)
            return CommandResult.Rejected($"Comparison horizon must be between 1 and {AutomationPresetComparisonRunner.MaximumHorizonTicks} ticks.");
        latestAutomationComparison = AutomationPresetComparisonRunner.Run(Seed, Configuration, horizonTicks, baseline, variant);
        Notify("Controlled comparison complete", latestAutomationComparison.Summary);
        return CommandResult.Accepted("Automation comparison complete.");
    }

    private void AdvanceAutomation()
    {
        if (!activeAutomationRule.Enabled || automationHalted) return;
        var kind = ChooseAutomatedRack();
        if (kind is null)
        {
            safetyBlockActive = false;
            return;
        }

        var reportedReadyAtDecision = WasherReportedReady;
        var physicalReadyAtDecision = WasherPhysicalReady;
        var evaluation = AutomationRuleEvaluator.Evaluate(activeAutomationRule,
            new(At(DishState.Racked).Total, reportedReadyAtDecision, physicalReadyAtDecision));

        if (!physicalReadyAtDecision)
        {
            if (!evaluation.ConditionMatched)
            {
                RecordAutomationRuleTrace(evaluation.Trace);
                if (reportedReadyAtDecision && !safetyBlockActive)
                {
                    safetyBlockActive = true;
                    preventedUnsafeStarts++;
                    RecordAutomationTrace(AutomationTraceOutcome.UnsafeStartPrevented, kind);
                    if (TutorialStage == DishTutorialStage.ValidateAutomation)
                    {
                        TutorialStage = DishTutorialStage.ValidateRegression;
                        Notify("Devon's check held", "The Ready light stayed on, but the physical washer check refused another start. Retest the captured incident next.");
                    }
                }
                else if (!reportedReadyAtDecision)
                {
                    safetyBlockActive = false;
                }
                return;
            }

            automationIncidents++;
            automationHalted = true;
            automationIncident ??= new(Tick, kind.Value, automationPolicy, reportedReadyAtDecision, physicalReadyAtDecision);
            RecordAutomationRuleTrace(AutomationRuleEvaluator.WithOutcomes(evaluation.Trace,
            [
                new(0, evaluation.SelectedEffects[0], false, "Physical washer state rejected the requested start."),
            ]));
            RecordAutomationTrace(AutomationTraceOutcome.UnsafeStartRequested, kind);
            if (TutorialStage == DishTutorialStage.ObserveAutomation)
            {
                TutorialStage = DishTutorialStage.InvestigateAutomation;
                Narrate(DishStationNarrativeEventKind.AutomationIncident, DishStationQuestId.InvestigateTheSignal);
            }
            Notify("Washer start stopped", "The rule requested another start because Ready was lit, but Devon can see the previous clean rack is still inside. Automatic starts are halted.");
            return;
        }

        if (!evaluation.ConditionMatched || evaluation.SelectedEffects is not [IssueDishActionAutomationEffect { Action: DishAction.StartWasher }])
        {
            RecordAutomationRuleTrace(evaluation.Trace);
            return;
        }
        safetyBlockActive = false;
        var result = Perform(DishAction.StartWasher, kind.Value, DishTransitionCause.Automation);
        RecordAutomationRuleTrace(AutomationRuleEvaluator.WithOutcomes(evaluation.Trace,
        [
            new(0, evaluation.SelectedEffects[0], result.Success, result.Message),
        ]));
        if (result.Success)
        {
            automatedStarts++;
            RecordAutomationTrace(AutomationTraceOutcome.AutomaticStart, kind, reportedReadyAtDecision, physicalReadyAtDecision);
            if (!stickyReadySignal && ShouldTriggerStickyReadyFault()) stickyReadySignal = true;
        }
    }

    private bool ShouldTriggerStickyReadyFault()
    {
        if (Configuration.StickyReadyFaultAfterAutomatedStarts > 0 &&
            automatedStarts >= Configuration.StickyReadyFaultAfterAutomatedStarts) return true;
        if (Configuration.StickyReadyFaultPermillePerStart == 0) return false;

        var value = faultRandomState;
        value ^= value << 13;
        value ^= value >> 17;
        value ^= value << 5;
        faultRandomState = value;
        return value % 1000 < Configuration.StickyReadyFaultPermillePerStart;
    }

    private DishKind? ChooseAutomatedRack()
    {
        if (EffectiveRushEnabled && At(DishState.Racked).Glasses > 0) return DishKind.Glass;
        if (At(DishState.Racked).Trays > 0) return DishKind.Tray;
        if (At(DishState.Racked).Plates > 0) return DishKind.Plate;
        if (At(DishState.Racked).Glasses > 0) return DishKind.Glass;
        return null;
    }

    private CommandResult InspectAutomationIncident()
    {
        if (automationIncidents == 0)
        {
            return CommandResult.Rejected("No automation incident has been recorded.");
        }

        if (TutorialStage == DishTutorialStage.InvestigateAutomation)
        {
            TutorialStage = DishTutorialStage.ReplayAutomation;
            Notify("Report and reality diverged", $"The panel reported Ready={automationIncident?.ReportedReady}, while Devon's physical check was Ready={automationIncident?.PhysicalReady}. Retest that captured decision next.");
        }
        else
        {
            Notify("Incident inspected", $"ReportedReady={WasherReportedReady}; PhysicalReady={WasherPhysicalReady}; halted={automationHalted}.");
        }

        RecordAutomationTrace(AutomationTraceOutcome.IncidentInspected, automationIncident?.Kind);
        return CommandResult.Accepted("Automation incident inspected.");
    }

    private CommandResult ReplayAutomationIncident()
    {
        if (automationIncident is not { } incident)
        {
            return CommandResult.Rejected("No automation incident has been captured for replay.");
        }

        var evaluation = AutomationRuleEvaluator.Evaluate(activeAutomationRule,
            new(1, incident.ReportedReady, incident.PhysicalReady));
        var wouldStart = evaluation.ConditionMatched;
        RecordAutomationRuleTrace(evaluation.Trace);
        automationReplayCount++;
        automationHasReplay = true;
        lastReplayPolicy = automationPolicy;
        lastReplayWouldStart = wouldStart;
        RecordAutomationTrace(wouldStart ? AutomationTraceOutcome.ReplayWouldStart : AutomationTraceOutcome.ReplayPrevented, incident.Kind);
        if (!wouldStart && preventedUnsafeStarts > 0) automationRegressionPassed = true;

        if (TutorialStage == DishTutorialStage.ReplayAutomation && wouldStart)
        {
            TutorialStage = DishTutorialStage.RefineAutomation;
            Notify("Failure reproduced", $"The original rule requests another start from ReportedReady={incident.ReportedReady} even though Devon's physical check is Ready={incident.PhysicalReady}. Refine the rule now.");
        }
        else if (TutorialStage == DishTutorialStage.ValidateRegression && !wouldStart)
        {
            TutorialStage = DishTutorialStage.ShiftReview;
            Notify("Captured failure rejected", "The corrected rule refused the exact unsafe request. Prepare clean glasses, then let Avery hand you the live shift.");
        }
        else
        {
            Notify("Incident replay", wouldStart
                ? "The selected policy reproduces the unsafe start request."
                : "The selected policy prevents the recorded unsafe request.");
        }

        return CommandResult.Accepted(wouldStart ? "Replay reproduced the unsafe request." : "Replay prevented the unsafe request.");
    }

    private CommandResult StartShiftTrial()
    {
        if (TutorialStage != DishTutorialStage.ShiftReview)
            return CommandResult.Rejected("Complete the regression proof before starting the reliability window.");
        if (!newHireEnabled || newHireSpecification != DishProcessSpecification.FullyDocumented)
            return CommandResult.Rejected("The reliability window requires an enabled worker with the complete shared process.");
        if (!automationPolicy.Enabled || !automationPolicy.RequirePhysicalReady)
            return CommandResult.Rejected("The reliability window requires the corroborated-ready automation policy.");
        if (At(DishState.Available).For(Configuration.DemandKind) == 0)
            return CommandResult.Rejected($"Stage at least one clean {Configuration.DemandKind.ToString().ToLowerInvariant()} before opening the window.");

        shiftTrialStatus = ShiftTrialStatus.Running;
        shiftTrialSuccessfulDemandChecks = 0;
        shiftTrialAttempts++;
        shiftTrialBaselineShortages = ServiceShortages;
        shiftTrialBaselineAutomationIncidents = automationIncidents;
        shiftTrialStartedAtTick = Tick.Value;
        shiftTrialCompletedAtTick = -1;
        RushEnabled = true;
        TutorialStage = DishTutorialStage.ValidateShift;
        Notify("Avery hands over the shift", $"Tessa will make {ShiftTrialTargetDemandChecks} service checks. Keep the crew supplied without another shortage or unsafe washer request.");
        return CommandResult.Accepted("Live reliability window started.");
    }

    private void AdvanceShiftTrial()
    {
        if (shiftTrialStatus != ShiftTrialStatus.Running) return;
        if (ServiceShortages > shiftTrialBaselineShortages || automationIncidents > shiftTrialBaselineAutomationIncidents)
        {
            shiftTrialStatus = ShiftTrialStatus.Failed;
            TutorialStage = DishTutorialStage.ShiftReview;
            Notify("Shift handoff interrupted", ServiceShortages > shiftTrialBaselineShortages
                ? "Tessa waited for clean supply. Recover the queue, stage glasses, and ask Avery to restart the handoff."
                : "The washer rule made another unsafe request. Review Devon's physical check before Avery restarts the handoff.");
            return;
        }

        if (shiftTrialSuccessfulDemandChecks < ShiftTrialTargetDemandChecks) return;
        shiftTrialStatus = ShiftTrialStatus.Passed;
        shiftTrialCompletedAtTick = Tick.Value;
        shiftReport = new(
            true,
            Tick.Value,
            Completed,
            ServiceShortages,
            baselineRouteSteps,
            validatedRouteSteps,
            placements.EstimatedRouteSteps,
            newHireActionsCompleted,
            trayReworkIncidents,
            automatedStarts,
            automationIncidents,
            preventedUnsafeStarts,
            CaptureEconomySnapshot());
        TutorialStage = DishTutorialStage.EpisodeComplete;
        Narrate(DishStationNarrativeEventKind.ShiftSucceeded, DishStationQuestId.OwnTheShift);
        Notify("Shift owned", "Tessa's three service checks passed without a shortage or unsafe washer request. Avery leaves the station in your hands.");
    }

    private DishStationEconomySnapshot CaptureEconomySnapshot()
    {
        var rates = Configuration.Economy;
        var laborTicks = checked((playerWorkActions + newHireActionsCompleted) * rates.LaborTicksPerWorkAction);
        var laborCost = checked(laborTicks * rates.LaborCostPerTick);
        var staffingCost = checked(staffedTicks * rates.StaffingCostPerEnabledTick);
        var wasteCost = checked(trayReworkIncidents * rates.TrayReworkCost);
        var shortageDowntimeCost = checked(ServiceShortages * rates.ServiceShortageDowntimeCost);
        var incidentDowntimeCost = checked(automationIncidents * rates.AutomationIncidentDowntimeCost);
        var downtimeCost = checked(shortageDowntimeCost + incidentDowntimeCost);
        var investmentCost = flowCellInvestmentPurchased ? rates.FlowCellInvestmentCost : 0;
        var throughputValue = checked(Completed * rates.CompletedDishValue);
        var totalCost = checked(laborCost + staffingCost + wasteCost + downtimeCost + investmentCost);
        return new(
            playerWorkActions,
            newHireActionsCompleted,
            laborTicks,
            staffedTicks,
            trayReworkIncidents,
            ServiceShortages,
            automationIncidents,
            flowCellInvestmentPurchased,
            throughputValue,
            laborCost,
            staffingCost,
            wasteCost,
            shortageDowntimeCost,
            incidentDowntimeCost,
            downtimeCost,
            investmentCost,
            totalCost,
            checked(throughputValue - totalCost));
    }

    private CommandResult InjectStickyReadyFault()
    {
        stickyReadySignal = true;
        Notify("God mode", "Injected sticky washer-ready signal.");
        return CommandResult.Accepted("Sticky-ready fault injected.");
    }

    private CommandResult CompleteIntro(GuidanceMode mode, bool useReducedMotion, bool useHighContrast)
    {
        if (!Enum.IsDefined(mode)) return CommandResult.Rejected("Unknown guidance mode.");
        guidanceMode = mode;
        reducedMotion = useReducedMotion;
        highContrast = useHighContrast;
        if (!introComplete)
        {
            careerStartedAtTick = Tick.Value;
            questStartedAtTicks[(int)DishStationQuestId.ClockIn] = Tick.Value;
        }
        introComplete = true;
        return CommandResult.Accepted($"Career started with {mode.ToString().ToLowerInvariant()} guidance.");
    }

    private void UpdateProgression()
    {
        foreach (var quest in Enum.GetValues<DishStationQuestId>())
        {
            if (completedQuests[(int)quest] || !HasReachedQuestOutcome(quest)) continue;
            if (questStartedAtTicks[(int)quest] < 0) questStartedAtTicks[(int)quest] = Math.Max(0, careerStartedAtTick);
            completedQuests[(int)quest] = true;
            questCompletedAtTicks[(int)quest] = Tick.Value;
            careerExperience += DishStationProgressionRules.ExperienceReward(quest);
            var next = (int)quest + 1;
            if (next < questStartedAtTicks.Length && questStartedAtTicks[next] < 0) questStartedAtTicks[next] = Tick.Value;
        }
    }

    private bool HasReachedQuestOutcome(DishStationQuestId quest)
    {
        var completionStage = quest switch
        {
            DishStationQuestId.ClockIn => DishTutorialStage.EnableDinnerRush,
            DishStationQuestId.FindTheConstraint => DishTutorialStage.ImproveLayout,
            DishStationQuestId.ImproveTheFlow => DishTutorialStage.InviteNewHire,
            DishStationQuestId.TransferTheWork => DishTutorialStage.ObserveRareTray,
            DishStationQuestId.CaptureTheException => DishTutorialStage.OfferAutomation,
            DishStationQuestId.InvestigateTheSignal => DishTutorialStage.RefineAutomation,
            DishStationQuestId.ProveTheFix => DishTutorialStage.ShiftReview,
            DishStationQuestId.OwnTheShift => DishTutorialStage.EpisodeComplete,
            _ => DishTutorialStage.EpisodeComplete,
        };
        return TutorialStage >= completionStage;
    }

    private OnboardingSnapshot CaptureOnboardingSnapshot() => new(introComplete, guidanceMode, reducedMotion, highContrast);

    private CareerProgressionSnapshot CaptureProgressionSnapshot()
    {
        var quests = new DishStationQuestProgress[completedQuests.Length];
        DishStationQuestId? activeQuest = null;
        foreach (var quest in Enum.GetValues<DishStationQuestId>())
        {
            var complete = completedQuests[(int)quest];
            if (!complete && activeQuest is null) activeQuest = quest;
            var startedAt = questStartedAtTicks[(int)quest];
            var completedAt = questCompletedAtTicks[(int)quest];
            var elapsed = startedAt < 0 ? 0 : (completedAt >= 0 ? completedAt : Tick.Value) - startedAt;
            quests[(int)quest] = new(quest, complete, complete ? 100 : QuestProgressPercent(quest), startedAt, completedAt, elapsed);
        }

        var capabilities = Enum.GetValues<DishStationQuestId>()
            .Where(quest => completedQuests[(int)quest])
            .Select(DishStationProgressionRules.CapabilityReward)
            .ToArray();
        var level = DishStationProgressionRules.LevelForExperience(careerExperience);
        var levelStart = DishStationProgressionRules.ExperienceForLevel(level);
        var nextLevel = level >= DishStationProgressionRules.MaximumLevel ? 0 : DishStationProgressionRules.ExperienceForLevel(level + 1);
        var careerEndTick = questCompletedAtTicks[^1] >= 0 ? questCompletedAtTicks[^1] : Tick.Value;
        var activeTicks = careerStartedAtTick < 0 ? 0 : careerEndTick - careerStartedAtTick;
        return new(level, careerExperience, levelStart, nextLevel, activeQuest, activeTicks, quests, capabilities);
    }

    private int QuestProgressPercent(DishStationQuestId quest)
    {
        var (start, end) = quest switch
        {
            DishStationQuestId.ClockIn => (DishTutorialStage.RestockFirstDish, DishTutorialStage.EnableDinnerRush),
            DishStationQuestId.FindTheConstraint => (DishTutorialStage.EnableDinnerRush, DishTutorialStage.ImproveLayout),
            DishStationQuestId.ImproveTheFlow => (DishTutorialStage.ImproveLayout, DishTutorialStage.InviteNewHire),
            DishStationQuestId.TransferTheWork => (DishTutorialStage.InviteNewHire, DishTutorialStage.ObserveRareTray),
            DishStationQuestId.CaptureTheException => (DishTutorialStage.ObserveRareTray, DishTutorialStage.OfferAutomation),
            DishStationQuestId.InvestigateTheSignal => (DishTutorialStage.OfferAutomation, DishTutorialStage.RefineAutomation),
            DishStationQuestId.ProveTheFix => (DishTutorialStage.RefineAutomation, DishTutorialStage.ShiftReview),
            DishStationQuestId.OwnTheShift => (DishTutorialStage.ShiftReview, DishTutorialStage.EpisodeComplete),
            _ => (DishTutorialStage.RestockFirstDish, DishTutorialStage.EpisodeComplete),
        };
        var distance = Math.Max(1, (int)end - (int)start);
        return Math.Clamp(((int)TutorialStage - (int)start) * 100 / distance, 0, 99);
    }

    private AutomationSnapshot CaptureAutomationSnapshot() => new(
        automationPolicy,
        WasherReportedReady,
        WasherPhysicalReady,
        stickyReadySignal,
        automationHalted,
        automatedStarts,
        automationIncidents,
        preventedUnsafeStarts,
        CaptureAutomationIncidentSnapshot(),
        automationTraceSnapshot,
        automationRuleTraceSnapshot,
        activeAutomationRule,
        activeAutomationRuleEdit,
        new(automationBaselinePreset, automationVariantPreset, latestAutomationComparison));

    private ShiftTrialSnapshot CaptureShiftTrialSnapshot() => new(
        shiftTrialStatus,
        shiftTrialSuccessfulDemandChecks,
        ShiftTrialTargetDemandChecks,
        shiftTrialAttempts,
        shiftTrialStartedAtTick,
        shiftTrialCompletedAtTick);

    private AutomationIncidentSnapshot CaptureAutomationIncidentSnapshot()
    {
        if (automationIncident is not { } incident)
        {
            return new(false, default, default, default, false, false, automationReplayCount, automationHasReplay, lastReplayPolicy, lastReplayWouldStart, automationRegressionPassed);
        }

        return new(true, incident.OccurredAt, incident.Kind, incident.OriginalPolicy, incident.ReportedReady, incident.PhysicalReady, automationReplayCount, automationHasReplay, lastReplayPolicy, lastReplayWouldStart, automationRegressionPassed);
    }

    private void RecordAutomationTrace(AutomationTraceOutcome outcome, DishKind? kind, bool? reportedReady = null, bool? physicalReady = null)
    {
        if (automationTrace.Count == 24) automationTrace.RemoveAt(0);
        automationTrace.Add(new(Tick, outcome, kind, reportedReady ?? WasherReportedReady, physicalReady ?? WasherPhysicalReady, automationPolicy));
        automationTraceSnapshot = automationTrace.ToArray();
    }

    private void RecordAutomationRuleTrace(AutomationRuleEvaluationTrace evaluation)
    {
        if (automationRuleTrace.Count == 24) automationRuleTrace.RemoveAt(0);
        automationRuleTrace.Add(new(Tick, evaluation));
        automationRuleTraceSnapshot = automationRuleTrace.ToArray();
    }

    private ProcessCaptureSnapshot CaptureProcessSnapshot() => new(
        activeProcessCapture is null
            ? null
            : new(activeProcessCapture.Id, activeProcessCapture.Name, activeProcessCapture.StartedAt,
                activeProcessCapture.Steps.ToImmutableArray()),
        processArtifactSnapshot,
        processCaptureEventSnapshot,
        activeProcessEdit?.Snapshot(),
        appliedProcessArtifactId);

    private CommandResult StartProcessCapture(string name)
    {
        if (activeProcessCapture is not null) return CommandResult.Rejected("A process capture is already active.");
        if (string.IsNullOrWhiteSpace(name)) return CommandResult.Rejected("Process name is required.");
        var normalizedName = name.Trim();
        if (normalizedName.Length > 80) return CommandResult.Rejected("Process name cannot exceed 80 characters.");
        var id = new ProcessCaptureId(nextProcessCaptureId++);
        activeProcessCapture = new(id, normalizedName, Tick);
        RecordProcessCaptureEvent(new(Tick, id, ProcessCaptureEventKind.Started, null, null));
        Notify("Process capture started", $"Perform '{normalizedName}' manually; successful work will become ordered process steps.");
        return CommandResult.Accepted("Process capture started.");
    }

    private CommandResult CompleteProcessCapture()
    {
        if (activeProcessCapture is not { } capture) return CommandResult.Rejected("No process capture is active.");
        if (capture.Steps.Count == 0) return CommandResult.Rejected("Perform at least one successful work action before completing capture.");
        var provenance = new ProcessCaptureProvenance(
            capture.Id, ProcessCaptureSource.ManualPlayerWork, Seed, playerActorId, capture.StartedAt, Tick);
        var version = new PlayerProcessVersion(1, capture.Steps.ToImmutableArray(), provenance,
            ProcessRoutingPolicy.CapturedOrder, null);
        var artifact = new PlayerOwnedProcessArtifact(new(nextProcessArtifactId++), playerActorId, capture.Name, version, version);
        processArtifacts.Add(artifact);
        processArtifactSnapshot = processArtifacts.ToArray();
        RecordProcessCaptureEvent(new(Tick, capture.Id, ProcessCaptureEventKind.Completed, null, null));
        activeProcessCapture = null;
        Notify("Process captured", $"'{artifact.Name}' now has {artifact.Current.Steps.Length} ordered steps at baseline/current v1.");
        return CommandResult.Accepted("Process artifact created.");
    }

    private void CapturePlayerStep(
        DishAction action,
        DishKind kind,
        DishState input,
        DishState output,
        DishTransitionCause cause)
    {
        if (cause != DishTransitionCause.PlayerWork || activeProcessCapture is not { } capture) return;
        var step = new CapturedProcessStep(
            new(capture.Steps.Count + 1),
            capture.Steps.Count + 1,
            Tick,
            playerActorId,
            FixtureFor(action),
            action,
            kind,
            input,
            output,
            playerActorId);
        capture.Steps.Add(step);
        RecordProcessCaptureEvent(new(Tick, capture.Id, ProcessCaptureEventKind.StepCaptured, step.Sequence, step.Action));
    }

    private void RecordProcessCaptureEvent(ProcessCaptureEvent entry)
    {
        processCaptureEvents.Add(entry);
        processCaptureEventSnapshot = processCaptureEvents.ToArray();
    }

    private CommandResult BeginProcessEdit(PlayerProcessArtifactId artifactId)
    {
        if (activeProcessCapture is not null) return CommandResult.Rejected("Complete process capture before editing.");
        if (activeProcessEdit is not null) return CommandResult.Rejected("A process edit draft is already active.");
        var artifact = processArtifacts.FirstOrDefault(candidate => candidate.Id == artifactId);
        if (artifact is null) return CommandResult.Rejected($"Process artifact {artifactId.Value} does not exist.");
        if (artifact.Owner != playerActorId) return CommandResult.Rejected("Only the owning player can edit this process.");
        activeProcessEdit = new(
            artifact.Id,
            artifact.Current.Version,
            artifact.Current.Provenance.CaptureId,
            artifact.Current.Steps.ToList(),
            artifact.Current.RoutingPolicy,
            ValidateProcessDraft(artifact.Current.Steps));
        RecordProcessCaptureEvent(new(Tick, artifact.Current.Provenance.CaptureId, ProcessCaptureEventKind.DraftStarted, null, null));
        Notify("Process draft opened", $"Editing '{artifact.Name}' current v{artifact.Current.Version}; baseline v{artifact.Baseline.Version} remains unchanged.");
        return CommandResult.Accepted("Process edit draft opened.");
    }

    private CommandResult MoveProcessStep(ProcessStepId stepId, int offset)
    {
        if (activeProcessEdit is not { } draft) return CommandResult.Rejected("No process edit draft is active.");
        if (offset is not (-1 or 1)) return CommandResult.Rejected("A step move must be exactly -1 or +1.");
        var index = draft.Steps.FindIndex(step => step.Id == stepId);
        if (index < 0) return CommandResult.Rejected($"Process step {stepId.Value} is not in the draft.");
        var target = index + offset;
        if (target < 0 || target >= draft.Steps.Count) return CommandResult.Rejected("The step is already at that edge.");
        (draft.Steps[index], draft.Steps[target]) = (draft.Steps[target], draft.Steps[index]);
        Renumber(draft);
        DraftChanged(draft);
        return CommandResult.Accepted("Process step reordered in draft.");
    }

    private CommandResult AssignProcessStep(ProcessStepId stepId, ActorId actor)
    {
        if (activeProcessEdit is not { } draft) return CommandResult.Rejected("No process edit draft is active.");
        if (actor != playerActorId && actor != newHireId) return CommandResult.Rejected("A process step can be assigned only to the player or new hire.");
        var index = draft.Steps.FindIndex(step => step.Id == stepId);
        if (index < 0) return CommandResult.Rejected($"Process step {stepId.Value} is not in the draft.");
        draft.Steps[index] = draft.Steps[index] with { AssignedActor = actor };
        DraftChanged(draft);
        return CommandResult.Accepted($"Step assigned to actor {actor.Value} in draft.");
    }

    private CommandResult SetProcessRoutingPolicy(ProcessRoutingPolicy policy)
    {
        if (activeProcessEdit is not { } draft) return CommandResult.Rejected("No process edit draft is active.");
        if (!Enum.IsDefined(policy)) return CommandResult.Rejected("Unknown process routing policy.");
        draft.RoutingPolicy = policy;
        DraftChanged(draft);
        return CommandResult.Accepted($"Draft routing set to {policy}.");
    }

    private CommandResult ApplyProcessEdit()
    {
        if (activeProcessEdit is not { } draft) return CommandResult.Rejected("No process edit draft is active.");
        draft.Diagnostics = ValidateProcessDraft(draft.Steps);
        if (draft.Diagnostics.Length > 0) return CommandResult.Rejected(draft.Diagnostics[0].Message);
        var artifactIndex = processArtifacts.FindIndex(artifact => artifact.Id == draft.ArtifactId);
        if (artifactIndex < 0) return CommandResult.Rejected("The edited process artifact no longer exists.");
        var artifact = processArtifacts[artifactIndex];
        if (artifact.Current.Version != draft.BasedOnVersion)
            return CommandResult.Rejected("The process changed after this draft opened; reopen it from the current version.");
        var current = new PlayerProcessVersion(
            artifact.Current.Version + 1,
            draft.Steps.ToImmutableArray(),
            artifact.Current.Provenance,
            draft.RoutingPolicy,
            new(artifact.Current.Version, Tick, playerActorId));
        processArtifacts[artifactIndex] = artifact with { Current = current };
        processArtifactSnapshot = processArtifacts.ToArray();
        appliedProcessArtifactId = artifact.Id;
        RecordProcessCaptureEvent(new(Tick, draft.CaptureId, ProcessCaptureEventKind.VersionApplied, null, null));
        activeProcessEdit = null;
        Notify("Process version applied", $"'{artifact.Name}' current v{current.Version} is active; baseline v{artifact.Baseline.Version} is preserved.");
        return CommandResult.Accepted("Process version validated and applied.");
    }

    private CommandResult DiscardProcessEdit()
    {
        if (activeProcessEdit is not { } draft) return CommandResult.Rejected("No process edit draft is active.");
        RecordProcessCaptureEvent(new(Tick, draft.CaptureId, ProcessCaptureEventKind.DraftDiscarded, null, null));
        activeProcessEdit = null;
        return CommandResult.Accepted("Process edit draft discarded.");
    }

    private void DraftChanged(MutableProcessEdit draft)
    {
        draft.Diagnostics = ValidateProcessDraft(draft.Steps);
        RecordProcessCaptureEvent(new(Tick, draft.CaptureId, ProcessCaptureEventKind.DraftChanged, null, null));
    }

    private static void Renumber(MutableProcessEdit draft)
    {
        for (var index = 0; index < draft.Steps.Count; index++)
            draft.Steps[index] = draft.Steps[index] with { Sequence = index + 1 };
    }

    private static ImmutableArray<ProcessEditDiagnostic> ValidateProcessDraft(IReadOnlyList<CapturedProcessStep> steps)
    {
        var diagnostics = ImmutableArray.CreateBuilder<ProcessEditDiagnostic>();
        if (steps.Count == 0) diagnostics.Add(new("empty", "A process version must contain at least one step."));
        if (steps.Select(step => step.Id).Distinct().Count() != steps.Count)
            diagnostics.Add(new("duplicate-step", "Process step IDs must remain unique."));
        for (var index = 0; index < steps.Count; index++)
        {
            var step = steps[index];
            if (step.Sequence != index + 1)
                diagnostics.Add(new("sequence", "Process step sequence must be contiguous.", step.Id));
            if (step.AssignedActor.Value is not (0 or 1))
                diagnostics.Add(new("assignment", "Process steps can be assigned only to player actor 0 or new-hire actor 1.", step.Id));
            if (index == 0) continue;
            var previous = steps[index - 1];
            var compatible = previous.OutputState == step.InputState ||
                             previous.Action == DishAction.StartWasher && step.Action == DishAction.Unload;
            if (!compatible)
                diagnostics.Add(new("transition",
                    $"Step {step.Id.Value} expects {step.InputState}, but preceding step {previous.Id.Value} produces {previous.OutputState}.", step.Id));
        }
        return diagnostics.ToImmutable();
    }

    private bool EffectiveRushEnabled => RushEnabled || incidentDemandSpike;
    private DishKind EffectiveDemandKind => incidentDemandSpike ? incidentDemandKind : Configuration.DemandKind;
    private int EffectiveDemandIntervalTicks => incidentDemandSpike ? incidentDemandIntervalTicks : Configuration.DemandIntervalTicks;
    private int EffectiveRackCapacity => Math.Max(1, Configuration.RackCapacity - incidentCapacityLoss);

    private CommandResult TriggerIncident(DishStationIncident incident)
    {
        if (incident is null) return CommandResult.Rejected("Incident is required.");
        try
        {
            incident.Validate();
        }
        catch (ArgumentException exception)
        {
            return CommandResult.Rejected($"Invalid incident: {exception.Message}");
        }

        if (activeIncidents.Any(active => active.Incident.Id == incident.Id))
            return CommandResult.Rejected($"Incident '{incident.Id}' is already active.");
        if (activeIncidents.Any(active => active.Incident.Effect.Kind == incident.Effect.Kind))
            return CommandResult.Rejected($"A {incident.Effect.Kind} incident is already active.");

        activeIncidents.Add(new(incident, Tick + incident.Effect.DurationTicks));
        if (incident.Effect is ProcessDelayIncidentEffect delay && WasherRunning)
            washerCompletesAt += delay.AddedCycleTicks;
        RecomputeIncidentEffects();
        RecordIncidentTrace(incident, DishStationIncidentPhase.Started, incident.Observable);
        Notify("Incident started", incident.Observable);
        return CommandResult.Accepted($"{incident.Effect.Kind} incident started.");
    }

    private void ExpireIncidents()
    {
        var changed = false;
        for (var index = activeIncidents.Count - 1; index >= 0; index--)
        {
            var active = activeIncidents[index];
            if (Tick.Value < active.EndsAt.Value) continue;
            activeIncidents.RemoveAt(index);
            RecordIncidentTrace(active.Incident, DishStationIncidentPhase.Recovered, active.Incident.Recovery);
            Notify("Incident recovered", active.Incident.Recovery);
            changed = true;
        }
        if (changed) RecomputeIncidentEffects();
    }

    private void RecomputeIncidentEffects()
    {
        incidentProcessDelayTicks = 0;
        incidentCapacityLoss = 0;
        incidentBadSensor = false;
        incidentBlockedResource = false;
        incidentWorkerAbsent = false;
        incidentDemandSpike = false;
        incidentDemandKind = default;
        incidentDemandIntervalTicks = 0;
        foreach (var active in activeIncidents)
        {
            switch (active.Incident.Effect)
            {
                case ProcessDelayIncidentEffect delay: incidentProcessDelayTicks = delay.AddedCycleTicks; break;
                case CapacityLossIncidentEffect capacity: incidentCapacityLoss = capacity.LostSlots; break;
                case BadSensorIncidentEffect: incidentBadSensor = true; break;
                case BlockedResourceIncidentEffect: incidentBlockedResource = true; break;
                case WorkerAbsenceIncidentEffect: incidentWorkerAbsent = true; break;
                case DemandSpikeIncidentEffect demand:
                    incidentDemandSpike = true;
                    incidentDemandKind = demand.DemandKind;
                    incidentDemandIntervalTicks = demand.IntervalTicks;
                    break;
            }
        }
        activeIncidentSnapshot = activeIncidents
            .OrderBy(active => active.Incident.Id.Value, StringComparer.Ordinal)
            .Select(active => new ActiveDishStationIncidentSnapshot(
                active.Incident.Id, active.Incident.Effect.Kind, active.EndsAt, active.Incident.Scope, active.Incident.Evidence))
            .ToArray();
    }

    private void RecordIncidentTrace(DishStationIncident incident, DishStationIncidentPhase phase, string observation)
    {
        if (incidentTrace.Count == 48) incidentTrace.RemoveAt(0);
        incidentTrace.Add(new(Tick, incident.Id, incident.Effect.Kind, phase, observation, incident.Evidence));
        incidentTraceSnapshot = incidentTrace.ToArray();
    }

    private void TrackEntries(DishState state, DishKind kind, int count)
    {
        var entries = Entries(state, kind);
        for (var i = 0; i < count; i++) entries.Enqueue(Tick.Value);
    }

    private void TrackDeparture(DishState state, DishKind kind)
    {
        var entries = Entries(state, kind);
        if (!entries.TryDequeue(out var enteredAt))
        {
            throw new InvalidOperationException($"Residence tracker lost {kind} in {state}.");
        }

        var residence = Tick.Value - enteredAt;
        var index = (int)state;
        if (kind == DishKind.Plate)
        {
            completedPlateResidenceTicks[index] += residence;
            completedPlateVisits[index]++;
            maxPlateResidenceTicks[index] = Math.Max(maxPlateResidenceTicks[index], residence);
        }
        else if (kind == DishKind.Glass)
        {
            completedGlassResidenceTicks[index] += residence;
            completedGlassVisits[index]++;
            maxGlassResidenceTicks[index] = Math.Max(maxGlassResidenceTicks[index], residence);
        }
        else
        {
            completedTrayResidenceTicks[index] += residence;
            completedTrayVisits[index]++;
            maxTrayResidenceTicks[index] = Math.Max(maxTrayResidenceTicks[index], residence);
        }
    }

    private void SetTrackedCount(DishState state, DishKind kind, int count)
    {
        var entries = Entries(state, kind);
        entries.Clear();
        TrackEntries(state, kind, count);
    }

    private Queue<long> Entries(DishState state, DishKind kind) => kind switch
    {
        DishKind.Plate => plateStageEntries[(int)state],
        DishKind.Glass => glassStageEntries[(int)state],
        DishKind.Tray => trayStageEntries[(int)state],
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    private long OldestAge(Queue<long> entries) => entries.TryPeek(out var enteredAt) ? Tick.Value - enteredAt : 0;

    private void ClearStageEntries()
    {
        foreach (var entries in plateStageEntries) entries.Clear();
        foreach (var entries in glassStageEntries) entries.Clear();
        foreach (var entries in trayStageEntries) entries.Clear();
    }

    private static Queue<long>[] CreateStageEntryQueues()
    {
        var queues = new Queue<long>[Enum.GetValues<DishState>().Length];
        for (var i = 0; i < queues.Length; i++) queues[i] = new Queue<long>(32);
        return queues;
    }

    private void Notify(string title, string message) => notifications.Add(new(Tick, title, message));
    private void Narrate(DishStationNarrativeEventKind kind, DishStationQuestId quest) => narrativeEvents.Add(new(Tick, kind, quest));

    private static string DishPlural(DishKind kind) => kind switch
    {
        DishKind.Plate => "Plates",
        DishKind.Glass => "Glasses",
        DishKind.Tray => "Trays",
        _ => kind.ToString(),
    };

    private sealed record ActiveDishStationIncident(DishStationIncident Incident, SimulationTick EndsAt);
    private sealed class MutableProcessCapture(ProcessCaptureId id, string name, SimulationTick startedAt)
    {
        public ProcessCaptureId Id { get; } = id;
        public string Name { get; } = name;
        public SimulationTick StartedAt { get; } = startedAt;
        public List<CapturedProcessStep> Steps { get; } = new(8);
    }
    private sealed class MutableProcessEdit(
        PlayerProcessArtifactId artifactId,
        int basedOnVersion,
        ProcessCaptureId captureId,
        List<CapturedProcessStep> steps,
        ProcessRoutingPolicy routingPolicy,
        ImmutableArray<ProcessEditDiagnostic> diagnostics)
    {
        public PlayerProcessArtifactId ArtifactId { get; } = artifactId;
        public int BasedOnVersion { get; } = basedOnVersion;
        public ProcessCaptureId CaptureId { get; } = captureId;
        public List<CapturedProcessStep> Steps { get; } = steps;
        public ProcessRoutingPolicy RoutingPolicy { get; set; } = routingPolicy;
        public ImmutableArray<ProcessEditDiagnostic> Diagnostics { get; set; } = diagnostics;

        public ProcessEditDraft Snapshot() => new(
            ArtifactId, BasedOnVersion, Steps.ToImmutableArray(), RoutingPolicy, Diagnostics);
    }
}

public readonly record struct CommandResult(bool Success, string Message)
{
    public static CommandResult Accepted(string message) => new(true, message);
    public static CommandResult Rejected(string message) => new(false, message);
}

public sealed record WorldNotification(SimulationTick Tick, string Title, string Message);

public sealed record DishStationSnapshot(
    SimulationTick Tick,
    DishCounts[] Dishes,
    StageTelemetry[] Telemetry,
    DishState? Bottleneck,
    bool WasherRunning,
    bool WasherOccupied,
    bool RushEnabled,
    int Completed,
    int ServiceShortages,
    DishTutorialStage TutorialStage,
    DishState? BottleneckHypothesis,
    NewHireSnapshot NewHire,
    DishStationLayoutSnapshot Layout,
    AutomationSnapshot Automation,
    DishStationIncidentSnapshot Incidents,
    ProcessCaptureSnapshot ProcessCapture,
    OnboardingSnapshot Onboarding,
    ShiftTrialSnapshot ShiftTrial,
    DishStationEconomySnapshot Economy,
    ShiftReportSnapshot ShiftReport,
    CareerProgressionSnapshot Progression,
    IReadOnlyList<DishTransitionEntry> RecentTransitions,
    IReadOnlyList<DishStationNarrativeEvent> NarrativeEvents,
    WorldNotification? LatestNotification)
{
    public DishCounts At(DishState state) => Dishes[(int)state];
    public StageTelemetry MetricAt(DishState state) => Telemetry[(int)state];
}

public enum DishStationIncidentPhase
{
    Started,
    Recovered,
}

public readonly record struct ActiveDishStationIncidentSnapshot(
    DishStationIncidentId Id,
    DishStationIncidentKind Kind,
    SimulationTick EndsAt,
    string Scope,
    string Evidence);

public sealed record DishStationIncidentSnapshot(
    IReadOnlyList<ActiveDishStationIncidentSnapshot> Active,
    IReadOnlyList<DishStationIncidentTraceEntry> Trace);

public readonly record struct DishStationIncidentTraceEntry(
    SimulationTick Tick,
    DishStationIncidentId Id,
    DishStationIncidentKind Kind,
    DishStationIncidentPhase Phase,
    string Observation,
    string Evidence);

public readonly record struct OnboardingSnapshot(
    bool Complete,
    GuidanceMode GuidanceMode,
    bool ReducedMotion = false,
    bool HighContrast = false);

public enum ShiftTrialStatus
{
    NotStarted,
    Running,
    Failed,
    Passed,
}

public readonly record struct ShiftTrialSnapshot(
    ShiftTrialStatus Status,
    int SuccessfulDemandChecks,
    int TargetDemandChecks,
    int Attempts,
    long StartedAtTick,
    long CompletedAtTick);

public readonly record struct ShiftReportSnapshot(
    bool Available,
    long CompletedAtTick,
    int CompletedDishes,
    int ServiceShortages,
    int BaselineRouteSteps,
    int ValidatedRouteSteps,
    int FinalRouteSteps,
    int WorkerActions,
    int TrayReworkIncidents,
    int AutomatedStarts,
    int AutomationIncidents,
    int PreventedUnsafeStarts,
    DishStationEconomySnapshot Economy);

public readonly record struct DishStationEconomySnapshot(
    int PlayerWorkActions,
    int WorkerActions,
    int LaborTicks,
    int StaffedTicks,
    int ReworkIncidents,
    int ServiceShortages,
    int AutomationIncidents,
    bool FlowCellInvested,
    int ThroughputValue,
    int LaborCost,
    int StaffingCost,
    int WasteCost,
    int ShortageDowntimeCost,
    int IncidentDowntimeCost,
    int DowntimeCost,
    int InvestmentCost,
    int TotalCost,
    int NetValue);

public readonly record struct DishStationQuestProgress(
    DishStationQuestId Id,
    bool Complete,
    int Percent,
    long StartedAtTick,
    long CompletedAtTick,
    long ElapsedTicks);

public sealed record CareerProgressionSnapshot(
    int Level,
    int Experience,
    int CurrentLevelExperience,
    int NextLevelExperience,
    DishStationQuestId? ActiveQuest,
    long ActivePlayTicks,
    IReadOnlyList<DishStationQuestProgress> Quests,
    IReadOnlyList<CareerCapability> UnlockedCapabilities)
{
    public bool IsUnlocked(CareerCapability capability) => UnlockedCapabilities.Contains(capability);
    public DishStationQuestProgress Quest(DishStationQuestId id) => Quests[(int)id];
}

public readonly record struct StageTelemetry(
    long PlateItemTicks,
    long GlassItemTicks,
    long TrayItemTicks,
    int MaxQueueDepth,
    long CompletedPlateResidenceTicks,
    long CompletedGlassResidenceTicks,
    long CompletedTrayResidenceTicks,
    int CompletedPlateVisits,
    int CompletedGlassVisits,
    int CompletedTrayVisits,
    long MaxPlateResidenceTicks,
    long MaxGlassResidenceTicks,
    long MaxTrayResidenceTicks,
    long OldestPlateAge,
    long OldestGlassAge,
    long OldestTrayAge)
{
    public long TotalItemTicks => PlateItemTicks + GlassItemTicks + TrayItemTicks;
    public long OldestAge(DishKind kind) => kind switch
    {
        DishKind.Plate => OldestPlateAge,
        DishKind.Glass => OldestGlassAge,
        DishKind.Tray => OldestTrayAge,
        _ => 0,
    };
    public long AverageResidenceTicks(DishKind kind) => kind switch
    {
        DishKind.Plate => CompletedPlateVisits == 0 ? 0 : CompletedPlateResidenceTicks / CompletedPlateVisits,
        DishKind.Glass => CompletedGlassVisits == 0 ? 0 : CompletedGlassResidenceTicks / CompletedGlassVisits,
        DishKind.Tray => CompletedTrayVisits == 0 ? 0 : CompletedTrayResidenceTicks / CompletedTrayVisits,
        _ => 0,
    };
    public long MaxResidenceTicks(DishKind kind) => kind switch
    {
        DishKind.Plate => MaxPlateResidenceTicks,
        DishKind.Glass => MaxGlassResidenceTicks,
        DishKind.Tray => MaxTrayResidenceTicks,
        _ => 0,
    };
}

public readonly record struct NewHireSnapshot(
    ActorId Id,
    bool Enabled,
    DishProcessSpecification Specification,
    int ActionsCompleted,
    int PlateActions,
    int GlassActions,
    int TrayActions,
    int TrayReworkIncidents,
    DishAction? LastAction,
    DishKind? LastKind,
    bool OmittedPriorityObserved);

public readonly record struct DishStationLayoutSnapshot(
    DishStationLayout Layout,
    int PlayerTravelSteps,
    int NewHireTravelSteps,
    int BaselineRouteSteps,
    int ValidatedRouteSteps,
    DishStationPlacements Placements,
    FloorCell PlayerCell,
    int SandboxMovementSteps,
    int EstimatedRouteSteps);

public readonly record struct AutomationSnapshot(
    WasherAutomationPolicy Policy,
    bool ReportedReady,
    bool PhysicalReady,
    bool StickyReadySignal,
    bool Halted,
    int AutomatedStarts,
    int Incidents,
    int PreventedUnsafeStarts,
    AutomationIncidentSnapshot Incident,
    IReadOnlyList<AutomationTraceEntry> Trace,
    IReadOnlyList<AutomationRuleTraceEntry> RuleTrace,
    AutomationRule ActiveRule,
    AutomationRuleEditDraft? ActiveEdit,
    AutomationComparisonSnapshot Comparison);
