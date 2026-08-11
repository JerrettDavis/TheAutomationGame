using Automation.Domain;

namespace Automation.Simulation;

public sealed class DishStationWorld
{
    private readonly Queue<ISimulationCommand> pendingCommands = new();
    private readonly List<RecordedCommandInvocation> commandJournal = new(64);
    private bool replaying;
    private readonly List<WorldNotification> notifications = new(32);
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
    private bool automationHalted;
    private bool stickyReadySignal;
    private int automatedStarts;
    private int automationIncidents;
    private int preventedUnsafeStarts;
    private bool safetyBlockActive;
    private readonly List<AutomationTraceEntry> automationTrace = new(24);
    private AutomationTraceEntry[] automationTraceSnapshot = [];
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

    private const int ShiftTrialTargetDemandChecks = 3;

    public DishStationWorld(int seed = 42, DishStationScenarioConfiguration? configuration = null)
    {
        Seed = seed;
        Configuration = (configuration ?? new DishStationScenarioConfiguration()).Validate();
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
    public bool WasherPhysicalReady => !WasherOccupied;
    public bool WasherReportedReady => stickyReadySignal || WasherPhysicalReady;
    public DishStationLayout Layout => layout;
    public DishStationPlacements Placements => placements;
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
        if (!tutorialStarted)
        {
            tutorialStarted = true;
            Notify("Clock In", "Move one plate through the station. Start by scraping a dirty plate.");
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

        if (RushEnabled && Tick.Value % Configuration.DemandIntervalTicks == 0)
        {
            ConsumeForService(Configuration.DemandKind);
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
            RushEnabled,
            Completed,
            ServiceShortages,
            TutorialStage,
            BottleneckHypothesis,
            CaptureNewHireSnapshot(),
            CaptureLayoutSnapshot(),
            CaptureAutomationSnapshot(),
            CaptureOnboardingSnapshot(),
            CaptureShiftTrialSnapshot(),
            shiftReport,
            CaptureProgressionSnapshot(),
            dishTransitionSnapshot,
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
            SetNewHireEnabledCommand worker => SetNewHireEnabled(worker.Enabled),
            TrainNewHireCommand training => TrainNewHire(training.Specification),
            ConfigureWasherAutomationCommand automation => ConfigureAutomation(automation.Policy),
            InspectAutomationIncidentCommand => InspectAutomationIncident(),
            ReplayAutomationIncidentCommand => ReplayAutomationIncident(),
            StartShiftTrialCommand => StartShiftTrial(),
            InjectStickyReadyFaultCommand => InjectStickyReadyFault(),
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
            var stationCell = placements.At(FixtureFor(action));
            sandboxMovementSteps += playerCell.DistanceTo(stationCell);
            playerCell = stationCell;
        }
        var source = DishStationRules.RequiredState(action);
        if (At(source).For(kind) <= 0)
        {
            return CommandResult.Rejected($"No {kind.ToString().ToLowerInvariant()} is {source.ToString().ToLowerInvariant()}.");
        }

        if (action == DishAction.StartWasher)
        {
            if (WasherOccupied)
            {
                return CommandResult.Rejected(WasherRunning ? "The washer is already running." : "Unload the clean dish before starting another cycle.");
            }

            RecordTravel(action, cause);
            Move(source, DishState.Washing, kind, cause);
            WasherRunning = true;
            washingKind = kind;
            washerCompletesAt = Tick + Configuration.WasherCycleTicks;
            Notify("Washer started", $"The cycle takes {Configuration.WasherCycleTicks} ticks. Watch what queues while it runs.");
            return CommandResult.Accepted("Washer started.");
        }

        if (action == DishAction.Rack && At(DishState.Racked).Total >= Configuration.RackCapacity)
        {
            return CommandResult.Rejected($"The rack is at its {Configuration.RackCapacity}-dish capacity.");
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
                Notify("Keep Up", "You restored service supply. Enable the dinner rush and watch what the station cannot keep up with.");
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
                Notify("Rare tray validated", "The new hire returned the uncommon tray without rework. The manager now offers an automatic washer-start controller.");
            }
        }
        else if (!performedByNewHire && action == DishAction.Scrape)
        {
            Notify("Work has state", "The dish changed because of your action. Move it to the rack station next.");
        }
        else if (!performedByNewHire && action == DishAction.Rack)
        {
            Notify("Ready for the machine", "Start the washer when the rack is ready. The simulation, not the renderer, owns the cycle.");
        }
        else if (!performedByNewHire && action == DishAction.Unload)
        {
            Notify("Drying area", "The wet dish is out of the machine. Dry and restock it next.");
        }

        return CommandResult.Accepted($"{action} completed.");
    }

    private CommandResult SetRush(bool enabled)
    {
        RushEnabled = enabled;
        if (enabled && TutorialStage == DishTutorialStage.EnableDinnerRush)
        {
            TutorialStage = DishTutorialStage.AwaitServiceShortage;
        }
        Notify(enabled ? "Dinner rush" : "Rush paused", enabled
            ? $"Service now consumes a {Configuration.DemandKind.ToString().ToLowerInvariant()} every {Configuration.DemandIntervalTicks} ticks. Total throughput may hide a targeted shortage."
            : "Demand is paused; the station can recover.");
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
        newHireEnabled = false;
        omittedPriorityObserved = false;
        workerDeliveredGlass = false;
        layout = DishStationLayout.Linear;
        placements = DishStationPlacements.Linear;
        playerCell = placements.Scrape;
        sandboxMovementSteps = 0;
        playerTravelSteps = 0;
        newHireTravelSteps = 0;
        baselineRouteSteps = 0;
        layoutComparisonStartSteps = 0;
        validatedRouteSteps = 0;
        automationPolicy = default;
        automationHalted = false;
        stickyReadySignal = false;
        automatedStarts = 0;
        automationIncidents = 0;
        preventedUnsafeStarts = 0;
        safetyBlockActive = false;
        automationTrace.Clear();
        automationTraceSnapshot = [];
        automationIncident = null;
        automationReplayCount = 0;
        automationHasReplay = false;
        lastReplayPolicy = default;
        lastReplayWouldStart = false;
        automationRegressionPassed = false;
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
        playerCell = placements.Scrape;
        newHireEnabled = Configuration.InitialNewHireEnabled;
        newHireSpecification = Configuration.InitialNewHireSpecification;
        newHireActsAt = newHireEnabled ? Tick + 1 : new(0);
        automationPolicy = Configuration.InitialAutomationPolicy;
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
                Notify("Hypothesis supported", $"Service consumed the {kind.ToString().ToLowerInvariant()} produced by your intervention. The manager is sending a new hire; decide what process knowledge to transfer.");
            }
            else if (kind == DishKind.Glass && workerDeliveredGlass && TutorialStage == DishTutorialStage.ValidateDelegation)
            {
                TutorialStage = DishTutorialStage.ObserveRareTray;
                Add(DishState.Dirty, DishKind.Tray);
                Notify("The Rare Tray", "Delegation restored glass service. An uncommon tray has arrived; observe whether the written process covers it.");
            }
            return;
        }

        ServiceShortages++;
        LastShortageKind = kind;
        if (TutorialStage == DishTutorialStage.AwaitServiceShortage)
        {
            TutorialStage = DishTutorialStage.InspectShortage;
            Notify($"Where Did the {DishPlural(kind)} Go?", $"Service is short of {DishPlural(kind).ToLowerInvariant()}. Open the process lens and inspect where work has accumulated.");
        }
        else if (TutorialStage == DishTutorialStage.ObserveNewHire)
        {
            TutorialStage = DishTutorialStage.DocumentGlassPriority;
            Notify("Specification gap", "The new hire followed the documented flow, but glass service still starved. The rush-priority rule was never transferred.");
        }
        else
        {
            Notify("Service is waiting", $"No clean {kind.ToString().ToLowerInvariant()} is available. Trace where it is queued.");
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
            Notify("Form a hypothesis", $"The process lens shows {leader} as the pressure leader. Select the workstation you think constrains {LastShortageKind.ToString().ToLowerInvariant()} flow and confirm it.");
            return CommandResult.Accepted("Shortage evidence inspected; choose a bottleneck hypothesis.");
        }

        Notify("Observation recorded", "The process lens exposes queue pressure and peak depth from simulation history.");
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
        Notify("Improve the route", $"Evidence supports {hypothesis} as the current constraint. The first complete route cost {baselineRouteSteps} walking steps. Re-form the station as a U-shaped cell, then run one {LastShortageKind.ToString().ToLowerInvariant()} through it.");
        return CommandResult.Accepted("Hypothesis accepted for validation.");
    }

    private CommandResult ConfigureLayout(DishStationLayout requestedLayout)
    {
        if (requestedLayout == DishStationLayout.Custom)
            return CommandResult.Rejected("Custom layouts are created by placing individual fixtures.");
        layout = requestedLayout;
        placements = PlacementsFor(requestedLayout);
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
        if ((fixture is DishStationFixture.Washer or DishStationFixture.Unload) && WasherOccupied)
            return CommandResult.Rejected("Unload the washer before relocating its work area.");

        placements = placements.With(fixture, cell);
        layout = DishStationLayout.Custom;
        Notify("Layout changed", $"{FixtureLabel(fixture)} moved to {cell.X},{cell.Y}. The estimated handoff route is now {placements.EstimatedRouteSteps} steps.");
        return CommandResult.Accepted($"{FixtureLabel(fixture)} placed at {cell.X},{cell.Y}.");
    }

    private CommandResult MovePlayer(FloorCell destination)
    {
        if (!destination.IsInsideDishStation)
            return CommandResult.Rejected($"{destination.X},{destination.Y} is outside the dish-station floor.");
        var steps = playerCell.DistanceTo(destination);
        playerCell = destination;
        sandboxMovementSteps += steps;
        return CommandResult.Accepted(steps == 0 ? "Already at that floor cell." : $"Walked {steps} floor steps.");
    }

    private CommandResult SetNewHireEnabled(bool enabled)
    {
        newHireEnabled = enabled;
        if (enabled)
        {
            newHireActsAt = Tick + 1;
            if (TutorialStage == DishTutorialStage.InviteNewHire)
            {
                TutorialStage = DishTutorialStage.TrainNewHire;
                Notify("The New Hire", "The new worker is ready but does not know the dish process. Transfer an explicit procedure before delegating work.");
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
                ? "The new hire received the dish flow and the rush glass-priority rule. Observe the delegated result."
                : "The new hire received the visible happy-path flow. Observe how that definition behaves during the rush.");
        }
        else if (TutorialStage == DishTutorialStage.DocumentGlassPriority && specification.RushGlassPriorityDocumented)
        {
            TutorialStage = DishTutorialStage.ValidateDelegation;
            workerDeliveredGlass = false;
            Notify("Knowledge made explicit", "The process now says that glasses take priority during the rush. Validate the changed delegated behavior.");
        }
        else if (TutorialStage == DishTutorialStage.DocumentRareTray && specification.RareTrayHandlingDocumented)
        {
            TutorialStage = DishTutorialStage.ValidateRareTray;
            Notify("Rare knowledge captured", "The process now includes the uncommon tray orientation. Let the new hire retry it.");
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
        if (!newHireEnabled || !newHireSpecification.FlowDocumented || Tick.Value < newHireActsAt.Value) return;
        newHireActsAt = Tick + WorkerIntervalForCurrentLayout();

        var glassHasWork = HasProcessWork(DishKind.Glass);
        DishKind? workedKind = null;
        if ((TutorialStage is DishTutorialStage.ObserveRareTray or DishTutorialStage.ValidateRareTray) && HasProcessWork(DishKind.Tray) && TryPerformNewHire(DishKind.Tray))
        {
            workedKind = DishKind.Tray;
        }
        else
        {
            var preferGlass = RushEnabled && newHireSpecification.RushGlassPriorityDocumented && glassHasWork;
            var primary = preferGlass ? DishKind.Glass : DishKind.Plate;
            var secondary = preferGlass ? DishKind.Plate : DishKind.Glass;
            workedKind = TryPerformNewHire(primary) ? primary : TryPerformNewHire(secondary) ? secondary : (DishKind?)null;
        }
        if (workedKind is null) return;

        if (RushEnabled && !newHireSpecification.RushGlassPriorityDocumented && workedKind == DishKind.Plate && glassHasWork && !omittedPriorityObserved)
        {
            omittedPriorityObserved = true;
            Notify("Observed behavior", "The new hire chose a plate while glasses were waiting. That matches the written flow; no rush priority was specified.");
        }
    }

    private bool TryPerformNewHire(DishKind kind)
    {
        DishAction? action = null;
        if (At(DishState.CleanWet).For(kind) > 0) action = DishAction.DryAndRestock;
        else if (At(DishState.WashedInMachine).For(kind) > 0) action = DishAction.Unload;
        else if (At(DishState.Racked).For(kind) > 0 && !WasherOccupied && !automationPolicy.Enabled) action = DishAction.StartWasher;
        else if (At(DishState.Scraped).For(kind) > 0) action = DishAction.Rack;
        else if (At(DishState.Dirty).For(kind) > 0) action = DishAction.Scrape;
        if (action is null) return false;

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
                Notify("Rare tray rework", "The new hire used the ordinary rack orientation. The uncommon tray returned dirty because that exception was not in the process.");
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

    private void AdvanceAutomation()
    {
        if (!automationPolicy.Enabled || automationHalted) return;
        var kind = ChooseAutomatedRack();
        if (kind is null || !WasherReportedReady)
        {
            safetyBlockActive = false;
            return;
        }

        if (!WasherPhysicalReady)
        {
            if (automationPolicy.RequirePhysicalReady)
            {
                if (!safetyBlockActive)
                {
                    safetyBlockActive = true;
                    preventedUnsafeStarts++;
                    RecordAutomationTrace(AutomationTraceOutcome.UnsafeStartPrevented, kind);
                    if (TutorialStage == DishTutorialStage.ValidateAutomation)
                    {
                        TutorialStage = DishTutorialStage.ValidateRegression;
                        Notify("Guard observed", "The ready signal stayed lit, but the physical-state check prevented an invalid start. Replay the recorded incident to make this case a regression check.");
                    }
                }
                return;
            }

            automationIncidents++;
            automationHalted = true;
            automationIncident ??= new(Tick, kind.Value, automationPolicy, WasherReportedReady, WasherPhysicalReady);
            RecordAutomationTrace(AutomationTraceOutcome.UnsafeStartRequested, kind);
            if (TutorialStage == DishTutorialStage.ObserveAutomation) TutorialStage = DishTutorialStage.InvestigateAutomation;
            Notify("Automation incident", "The controller requested a start because Ready was lit, but the previous clean rack was still physically in the machine. Automatic control is halted.");
            return;
        }

        safetyBlockActive = false;
        var reportedReadyAtDecision = WasherReportedReady;
        var physicalReadyAtDecision = WasherPhysicalReady;
        var result = Perform(DishAction.StartWasher, kind.Value, DishTransitionCause.Automation);
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
        if (RushEnabled && At(DishState.Racked).Glasses > 0) return DishKind.Glass;
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
            Notify("First divergence", $"Reported Ready was {automationIncident?.ReportedReady}, while physical readiness was {automationIncident?.PhysicalReady}. The rule trusted one fallible signal as complete state. Replay the captured decision next.");
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

        var wouldStart = WouldRequestStart(automationPolicy, incident.ReportedReady, incident.PhysicalReady);
        automationReplayCount++;
        automationHasReplay = true;
        lastReplayPolicy = automationPolicy;
        lastReplayWouldStart = wouldStart;
        RecordAutomationTrace(wouldStart ? AutomationTraceOutcome.ReplayWouldStart : AutomationTraceOutcome.ReplayPrevented, incident.Kind);
        if (!wouldStart && preventedUnsafeStarts > 0) automationRegressionPassed = true;

        if (TutorialStage == DishTutorialStage.ReplayAutomation && wouldStart)
        {
            TutorialStage = DishTutorialStage.RefineAutomation;
            Notify("Failure reproduced", $"At captured tick {incident.OccurredAt.Value}, the original rule requests another start from ReportedReady={incident.ReportedReady} even though PhysicalReady={incident.PhysicalReady}. Refine the rule now.");
        }
        else if (TutorialStage == DishTutorialStage.ValidateRegression && !wouldStart)
        {
            TutorialStage = DishTutorialStage.ShiftReview;
            Notify("Regression passed", "The corrected policy rejected the captured inputs. Prepare the station, then start a live reliability window to prove the whole shift can hold together.");
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
        Notify("Reliability window open", $"Service will make {ShiftTrialTargetDemandChecks} demand checks. Keep supply available without a new shortage or unsafe automation request.");
        return CommandResult.Accepted("Live reliability window started.");
    }

    private void AdvanceShiftTrial()
    {
        if (shiftTrialStatus != ShiftTrialStatus.Running) return;
        if (ServiceShortages > shiftTrialBaselineShortages || automationIncidents > shiftTrialBaselineAutomationIncidents)
        {
            shiftTrialStatus = ShiftTrialStatus.Failed;
            TutorialStage = DishTutorialStage.ShiftReview;
            Notify("Reliability window failed", ServiceShortages > shiftTrialBaselineShortages
                ? "Service waited for clean supply. Recover the queue, stage inventory, and retry when the system is ready."
                : "The controller made a new unsafe request. Inspect the policy and retry after correcting it.");
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
            preventedUnsafeStarts);
        TutorialStage = DishTutorialStage.EpisodeComplete;
        Notify("Shift owned", "Three live demand checks completed without a shortage or unsafe request. The combined system held under operation.");
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
        automationTraceSnapshot);

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

    private static bool WouldRequestStart(WasherAutomationPolicy policy, bool reportedReady, bool physicalReady) =>
        policy.Enabled && reportedReady && (!policy.RequirePhysicalReady || physicalReady);

    private void RecordAutomationTrace(AutomationTraceOutcome outcome, DishKind? kind, bool? reportedReady = null, bool? physicalReady = null)
    {
        if (automationTrace.Count == 24) automationTrace.RemoveAt(0);
        automationTrace.Add(new(Tick, outcome, kind, reportedReady ?? WasherReportedReady, physicalReady ?? WasherPhysicalReady, automationPolicy));
        automationTraceSnapshot = automationTrace.ToArray();
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

    private static string DishPlural(DishKind kind) => kind switch
    {
        DishKind.Plate => "Plates",
        DishKind.Glass => "Glasses",
        DishKind.Tray => "Trays",
        _ => kind.ToString(),
    };
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
    OnboardingSnapshot Onboarding,
    ShiftTrialSnapshot ShiftTrial,
    ShiftReportSnapshot ShiftReport,
    CareerProgressionSnapshot Progression,
    IReadOnlyList<DishTransitionEntry> RecentTransitions,
    WorldNotification? LatestNotification)
{
    public DishCounts At(DishState state) => Dishes[(int)state];
    public StageTelemetry MetricAt(DishState state) => Telemetry[(int)state];
}

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
    int PreventedUnsafeStarts);

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
    IReadOnlyList<AutomationTraceEntry> Trace);

public enum DishTutorialStage
{
    RestockFirstDish,
    EnableDinnerRush,
    AwaitServiceShortage,
    InspectShortage,
    ChooseBottleneck,
    ImproveLayout,
    ValidateBottleneck,
    AwaitValidationDemand,
    InviteNewHire,
    TrainNewHire,
    ObserveNewHire,
    DocumentGlassPriority,
    ValidateDelegation,
    ObserveRareTray,
    DocumentRareTray,
    ValidateRareTray,
    OfferAutomation,
    ObserveAutomation,
    InvestigateAutomation,
    ReplayAutomation,
    RefineAutomation,
    ValidateAutomation,
    ValidateRegression,
    ShiftReview,
    ValidateShift,
    EpisodeComplete,
}
