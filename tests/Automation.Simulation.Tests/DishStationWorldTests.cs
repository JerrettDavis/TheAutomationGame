using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class DishStationWorldTests
{
    [Fact]
    public void SandboxPlacementAndMovementAreAuthoritativeAndValidated()
    {
        var world = new DishStationWorld();

        var move = world.ExecuteNow(new MovePlayerCommand(world.Tick, new FloorCell(3, 3)));
        var place = world.ExecuteNow(new PlaceDishStationFixtureCommand(world.Tick, DishStationFixture.Rack, new FloorCell(4, 3)));
        var overlap = world.ExecuteNow(new PlaceDishStationFixtureCommand(world.Tick, DishStationFixture.Scrape, new FloorCell(4, 3)));
        var outside = world.ExecuteNow(new MovePlayerCommand(world.Tick, new FloorCell(50, 50)));
        var snapshot = world.Snapshot();

        Assert.True(move.Success);
        Assert.True(place.Success);
        Assert.False(overlap.Success);
        Assert.False(outside.Success);
        Assert.Equal(DishStationLayout.Custom, snapshot.Layout.Layout);
        Assert.Equal(new FloorCell(4, 3), snapshot.Layout.Placements.Rack);
        Assert.Equal(new FloorCell(3, 3), snapshot.Layout.PlayerCell);
        Assert.Equal(4, snapshot.Layout.SandboxMovementSteps);
    }

    [Fact]
    public void OccupiedWasherCannotBeRelocated()
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate));

        var result = world.ExecuteNow(new PlaceDishStationFixtureCommand(world.Tick, DishStationFixture.Washer, new FloorCell(6, 3)));

        Assert.False(result.Success);
        Assert.Equal(DishStationPlacements.Linear.Washer, world.Placements.Washer);
    }

    [Fact]
    public void CompactCustomRouteImprovesDelegatedWorkFrequency()
    {
        var linear = RunWorkerForLayout(DishStationPlacements.Linear, false);
        var compact = new DishStationPlacements(new(0, 4), new(1, 4), new(2, 4), new(3, 4), new(4, 4), new(5, 4));
        var custom = RunWorkerForLayout(compact, true);

        Assert.Equal(5, custom.Layout.EstimatedRouteSteps);
        Assert.True(custom.NewHire.ActionsCompleted > linear.NewHire.ActionsCompleted);
    }

    [Fact]
    public void ManualEpisodeProducesAvailableDish()
    {
        var world = new DishStationWorld();

        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        Advance(world, 20);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, DishKind.Plate)).Success);

        Assert.Equal(1, world.At(DishState.Available).Plates);
        Assert.Equal(1, world.Completed);
    }

    [Fact]
    public void SameSeedAndCommandsProduceSameSnapshot()
    {
        var first = RunDeterministicEpisode(17);
        var second = RunDeterministicEpisode(17);

        Assert.Equal(first.Tick, second.Tick);
        Assert.Equal(first.RushEnabled, second.RushEnabled);
        Assert.Equal(first.ServiceShortages, second.ServiceShortages);
        Assert.Equal(first.Dishes, second.Dishes);
    }

    [Fact]
    public void InvalidTransitionIsRejectedWithoutMutation()
    {
        var world = new DishStationWorld();

        var result = world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));

        Assert.False(result.Success);
        Assert.Equal(6, world.At(DishState.Dirty).Plates);
        Assert.Equal(0, world.At(DishState.Racked).Plates);
    }

    [Fact]
    public void RushMakesGlassBottleneckObservable()
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new SetRushCommand(world.Tick, true));

        Advance(world, 31);

        Assert.True(world.ServiceShortages >= 2);
        Assert.Contains(world.Notifications, item => item.Title == "Service is waiting");
    }

    [Fact]
    public void GodSetupConfiguresSupplyThroughCommands()
    {
        var world = new DishStationWorld();

        var result = world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Available, DishKind.Glass, 10));

        Assert.True(result.Success);
        Assert.Equal(10, world.At(DishState.Available).Glasses);
        Assert.Equal(2, world.At(DishState.Dirty).Glasses);
    }

    [Fact]
    public void ScenarioConfigurationControlsCapacityTimingArrivalsAndDemand()
    {
        var scenario = new DishStationScenarioConfiguration
        {
            InitialDirty = new(2, 0),
            ArrivalIntervalTicks = 4,
            GlassEveryArrivals = 1,
            RackCapacity = 1,
            WasherCycleTicks = 3,
            DemandKind = DishKind.Plate,
            DemandIntervalTicks = 2,
            InitialRushEnabled = true,
            StickyReadyFaultAfterAutomatedStarts = 0,
        };
        var world = new DishStationWorld(42, scenario);

        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        var fullRack = world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        Assert.False(fullRack.Success);

        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate));
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        Advance(world, 4);

        Assert.Equal(2, world.ServiceShortages);
        Assert.Equal(1, world.At(DishState.WashedInMachine).Plates);
        Assert.Equal(1, world.At(DishState.Dirty).Glasses);
        Assert.Contains(world.Notifications, notification => notification.Message.Contains("3 ticks", StringComparison.Ordinal));
    }

    [Fact]
    public void ScenarioCanStartWithKnowledgeAutomationLayoutAndGuaranteedFaultRisk()
    {
        var scenario = new DishStationScenarioConfiguration
        {
            InitialDirty = new(2, 0),
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.FullyDocumented,
            InitialAutomationPolicy = WasherAutomationPolicy.ReportedReadyOnly,
            InitialLayout = DishStationLayout.UShapedCell,
            FlowCellWorkerActionIntervalTicks = 1,
            StickyReadyFaultAfterAutomatedStarts = 0,
            StickyReadyFaultPermillePerStart = 1000,
        };
        var world = new DishStationWorld(17, scenario);

        Advance(world, 4);
        var snapshot = world.Snapshot();

        Assert.True(snapshot.NewHire.Enabled);
        Assert.Equal(DishProcessSpecification.FullyDocumented, snapshot.NewHire.Specification);
        Assert.Equal(DishStationLayout.UShapedCell, snapshot.Layout.Layout);
        Assert.Equal(WasherAutomationPolicy.ReportedReadyOnly, snapshot.Automation.Policy);
        Assert.True(snapshot.Automation.StickyReadySignal);
        Assert.Equal(1, snapshot.Automation.Incidents);
    }

    [Fact]
    public void InvalidScenarioConfigurationIsRejectedAtWorldBoundary()
    {
        var scenario = new DishStationScenarioConfiguration { RackCapacity = 0 };

        Assert.Throws<ArgumentOutOfRangeException>(() => new DishStationWorld(configuration: scenario));
    }

    [Fact]
    public void ResetCommandRestoresStartingEpisode()
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new SetRushCommand(world.Tick, true));
        world.ExecuteNow(new AddDirtyDishesCommand(world.Tick, DishKind.Glass, 12));
        Advance(world, 20);

        var result = world.ExecuteNow(new ResetDishStationCommand(world.Tick));

        Assert.True(result.Success);
        Assert.Equal(new SimulationTick(0), world.Tick);
        Assert.False(world.RushEnabled);
        Assert.Equal(new DishCounts(6, 2), world.At(DishState.Dirty));
        Assert.Equal(0, world.At(DishState.Available).Total);
        Assert.Equal("Scenario reset", world.Notifications[^1].Title);
        Assert.Equal(0, world.Snapshot().MetricAt(DishState.Dirty).TotalItemTicks);
    }

    [Fact]
    public void IntroChoiceAndEarlyCareerProgressReplayDeterministically()
    {
        var world = new DishStationWorld(23);
        var intro = world.ExecuteNow(new CompleteIntroCommand(world.Tick, GuidanceMode.Contextual, true, true));
        RunManualPlate(world);

        Assert.True(intro.Success);
        Assert.True(world.Snapshot().Onboarding.Complete);
        Assert.Equal(GuidanceMode.Contextual, world.Snapshot().Onboarding.GuidanceMode);
        Assert.True(world.Snapshot().Onboarding.ReducedMotion);
        Assert.True(world.Snapshot().Onboarding.HighContrast);
        Assert.True(world.Snapshot().Progression.Quest(DishStationQuestId.ClockIn).Complete);
        Assert.Equal(100, world.Snapshot().Progression.Experience);
        Assert.Equal(2, world.Snapshot().Progression.Level);
        Assert.True(world.Snapshot().Progression.IsUnlocked(CareerCapability.StateLens));

        var restored = DishStationWorld.Restore(world.CreateReplaySave());
        Assert.Equal(world.Snapshot().Onboarding, restored.Snapshot().Onboarding);
        Assert.Equal(world.Snapshot().Progression.Experience, restored.Snapshot().Progression.Experience);
        Assert.Equal(world.Snapshot().Progression.Quests.ToArray(), restored.Snapshot().Progression.Quests.ToArray());
    }

    [Fact]
    public void ProcessTelemetryMakesQueuePressureObservable()
    {
        var world = new DishStationWorld();

        Advance(world, 10);
        var snapshot = world.Snapshot();

        Assert.Equal(60, snapshot.MetricAt(DishState.Dirty).PlateItemTicks);
        Assert.Equal(20, snapshot.MetricAt(DishState.Dirty).GlassItemTicks);
        Assert.Equal(8, snapshot.MetricAt(DishState.Dirty).MaxQueueDepth);
        Assert.Equal(DishState.Dirty, snapshot.Bottleneck);
    }

    [Fact]
    public void StateTraceRecordsAuthoritativeTransitionCauses()
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.ReportedReadyOnly));
        world.Advance();
        Advance(world, 20);

        var transitions = world.Snapshot().RecentTransitions;
        Assert.Collection(transitions,
            item => Assert.Equal((DishState.Dirty, DishState.Scraped, DishTransitionCause.PlayerWork), (item.From, item.To, item.Cause)),
            item => Assert.Equal((DishState.Scraped, DishState.Racked, DishTransitionCause.PlayerWork), (item.From, item.To, item.Cause)),
            item => Assert.Equal((DishState.Racked, DishState.Washing, DishTransitionCause.Automation), (item.From, item.To, item.Cause)),
            item => Assert.Equal((DishState.Washing, DishState.WashedInMachine, DishTransitionCause.WasherCycle), (item.From, item.To, item.Cause)));
    }

    [Fact]
    public void TutorialEpisodeAdvancesFromManualWorkToEvidence()
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate));
        Advance(world, 20);
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, DishKind.Plate));
        Assert.Equal(DishTutorialStage.EnableDinnerRush, world.TutorialStage);

        world.ExecuteNow(new SetRushCommand(world.Tick, true));
        Advance(world, 10);
        Assert.Equal(DishTutorialStage.InspectShortage, world.TutorialStage);

        var result = world.ExecuteNow(new InspectProcessCommand(world.Tick));

        Assert.True(result.Success);
        Assert.Equal(DishTutorialStage.ChooseBottleneck, world.TutorialStage);

        var unsupported = world.ExecuteNow(new ConfirmBottleneckCommand(world.Tick, DishState.Racked));
        Assert.False(unsupported.Success);
        Assert.Equal(DishTutorialStage.ChooseBottleneck, world.TutorialStage);

        var supported = world.ExecuteNow(new ConfirmBottleneckCommand(world.Tick, DishState.Dirty));
        Assert.True(supported.Success);
        Assert.Equal(DishTutorialStage.ImproveLayout, world.TutorialStage);
        Assert.Equal(22, world.Snapshot().Layout.BaselineRouteSteps);

        var layout = world.ExecuteNow(new ConfigureDishStationLayoutCommand(world.Tick, DishStationLayout.UShapedCell));
        Assert.True(layout.Success);
        Assert.Equal(DishTutorialStage.ValidateBottleneck, world.TutorialStage);

        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Glass));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Glass));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Glass));
        Advance(world, 20);
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Glass));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, DishKind.Glass));
        Assert.Equal(DishTutorialStage.AwaitValidationDemand, world.TutorialStage);
        Assert.Equal(10, world.Snapshot().Layout.ValidatedRouteSteps);
        Assert.Contains(world.Notifications, notification => notification.Title == "Layout evidence");

        Advance(world, 10);
        Assert.Equal(DishTutorialStage.InviteNewHire, world.TutorialStage);
        Assert.Contains(world.Notifications, notification => notification.Title == "Hypothesis supported");

        world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true));
        world.ExecuteNow(new TrainNewHireCommand(world.Tick, DishProcessSpecification.HappyPath));
        Advance(world, 15);
        Assert.Equal(DishTutorialStage.DocumentGlassPriority, world.TutorialStage);
        Assert.True(world.Snapshot().NewHire.OmittedPriorityObserved);

        world.ExecuteNow(new TrainNewHireCommand(world.Tick, DishProcessSpecification.RushAware));
        for (var i = 0; i < 250 && world.TutorialStage != DishTutorialStage.DocumentRareTray; i++) world.Advance();

        Assert.Equal(DishTutorialStage.DocumentRareTray, world.TutorialStage);
        Assert.True(world.Snapshot().NewHire.TrayReworkIncidents > 0);

        world.ExecuteNow(new TrainNewHireCommand(world.Tick, DishProcessSpecification.FullyDocumented));
        for (var i = 0; i < 200 && world.TutorialStage != DishTutorialStage.OfferAutomation; i++) world.Advance();

        Assert.Equal(DishTutorialStage.OfferAutomation, world.TutorialStage);
        Assert.Contains(world.Notifications, notification => notification.Title == "Rare tray validated");

        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.ReportedReadyOnly));
        for (var i = 0; i < 250 && world.TutorialStage != DishTutorialStage.InvestigateAutomation; i++) world.Advance();

        Assert.Equal(DishTutorialStage.InvestigateAutomation, world.TutorialStage);
        Assert.True(world.Snapshot().Automation.StickyReadySignal);
        Assert.True(world.Snapshot().Automation.Halted);
        Assert.True(world.Snapshot().Automation.Incidents > 0);

        world.ExecuteNow(new InspectAutomationIncidentCommand(world.Tick));
        Assert.Equal(DishTutorialStage.ReplayAutomation, world.TutorialStage);

        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));
        Assert.Equal(DishTutorialStage.RefineAutomation, world.TutorialStage);
        Assert.True(world.Snapshot().Automation.Incident.LastReplayWouldStart);

        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.CorroboratedReady));
        for (var i = 0; i < 50 && world.TutorialStage != DishTutorialStage.ValidateRegression; i++) world.Advance();

        Assert.Equal(DishTutorialStage.ValidateRegression, world.TutorialStage);
        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));

        Assert.Equal(DishTutorialStage.ShiftReview, world.TutorialStage);
        Assert.True(world.Snapshot().Automation.PreventedUnsafeStarts > 0);
        Assert.True(world.Snapshot().Automation.Incident.RegressionPassed);
        Assert.Contains(world.Notifications, notification => notification.Title == "Regression passed");
        Assert.Equal(2500, world.Snapshot().Progression.Experience);
        Assert.Equal(6, world.Snapshot().Progression.Level);

        world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Available, DishKind.Glass, 1));
        var firstTrial = world.ExecuteNow(new StartShiftTrialCommand(world.Tick));
        Assert.True(firstTrial.Success);
        Assert.Equal(ShiftTrialStatus.Running, world.Snapshot().ShiftTrial.Status);
        world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, false));
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.Off));
        for (var i = 0; i < 40 && world.Snapshot().ShiftTrial.Status == ShiftTrialStatus.Running; i++) world.Advance();
        Assert.Equal(ShiftTrialStatus.Failed, world.Snapshot().ShiftTrial.Status);
        Assert.Equal(DishTutorialStage.ShiftReview, world.TutorialStage);
        Assert.Contains(world.Notifications, notification => notification.Title == "Reliability window failed");

        world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true));
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.CorroboratedReady));
        world.ExecuteNow(new ConfigureDishSupplyCommand(world.Tick, DishState.Available, DishKind.Glass, 3));
        var retry = world.ExecuteNow(new StartShiftTrialCommand(world.Tick));
        Assert.True(retry.Success);
        for (var i = 0; i < 60 && world.TutorialStage != DishTutorialStage.EpisodeComplete; i++) world.Advance();

        Assert.Equal(DishTutorialStage.EpisodeComplete, world.TutorialStage);
        Assert.Equal(ShiftTrialStatus.Passed, world.Snapshot().ShiftTrial.Status);
        Assert.Equal(3, world.Snapshot().ShiftTrial.SuccessfulDemandChecks);
        Assert.Equal(2, world.Snapshot().ShiftTrial.Attempts);
        Assert.Contains(world.Notifications, notification => notification.Title == "Shift owned");
        Assert.True(world.Snapshot().ShiftReport.Available);
        Assert.Equal(world.Tick.Value, world.Snapshot().ShiftReport.CompletedAtTick);
        Assert.Equal(3400, world.Snapshot().Progression.Experience);
        Assert.Equal(7, world.Snapshot().Progression.Level);
        Assert.Null(world.Snapshot().Progression.ActiveQuest);
        Assert.All(world.Snapshot().Progression.Quests, quest => Assert.True(quest.Complete));
        Assert.Equal(Enum.GetValues<CareerCapability>().Length, world.Snapshot().Progression.UnlockedCapabilities.Count);
        Assert.All(world.Snapshot().Progression.Quests, quest =>
        {
            Assert.True(quest.StartedAtTick >= 0);
            Assert.True(quest.CompletedAtTick >= quest.StartedAtTick);
            Assert.Equal(quest.CompletedAtTick - quest.StartedAtTick, quest.ElapsedTicks);
        });

        var completedCareerTicks = world.Snapshot().Progression.ActivePlayTicks;
        var completedShiftReport = world.Snapshot().ShiftReport;
        Advance(world, 10);
        Assert.Equal(completedCareerTicks, world.Snapshot().Progression.ActivePlayTicks);
        Assert.Equal(completedShiftReport, world.Snapshot().ShiftReport);

        var restored = DishStationWorld.Restore(world.CreateReplaySave()).Snapshot();
        Assert.Equal(world.Snapshot().ShiftTrial, restored.ShiftTrial);
        Assert.Equal(world.Snapshot().ShiftReport, restored.ShiftReport);
        Assert.Equal(world.Snapshot().Progression.Quests.ToArray(), restored.Progression.Quests.ToArray());
        Assert.Equal(3400, restored.Progression.Experience);
    }

    [Fact]
    public void CapturedIncidentReplaysDeterministicallyAgainstBothPolicies()
    {
        var first = RunAutomationIncidentReplay();
        var second = RunAutomationIncidentReplay();

        Assert.Equal(first.Trace.ToArray(), second.Trace.ToArray());
        Assert.True(first.Incident.Recorded);
        Assert.Equal(2, first.Incident.ReplayCount);
        Assert.False(first.Incident.LastReplayWouldStart);
        Assert.True(first.Incident.RegressionPassed);
        Assert.Contains(first.Trace, entry => entry.Outcome == AutomationTraceOutcome.ReplayWouldStart);
        Assert.Contains(first.Trace, entry => entry.Outcome == AutomationTraceOutcome.ReplayPrevented);
    }

    [Fact]
    public void ResidenceTelemetryTracksObservedStageTime()
    {
        var world = new DishStationWorld();
        Advance(world, 5);
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        Advance(world, 3);
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));

        var snapshot = world.Snapshot();

        Assert.Equal(5, snapshot.MetricAt(DishState.Dirty).AverageResidenceTicks(DishKind.Plate));
        Assert.Equal(3, snapshot.MetricAt(DishState.Scraped).AverageResidenceTicks(DishKind.Plate));
        Assert.Equal(8, snapshot.MetricAt(DishState.Dirty).OldestAge(DishKind.Plate));
    }

    [Fact]
    public void DelegatedBehaviorChangesWhenRushPriorityIsTransferred()
    {
        var happyPathOnly = RunDelegatedSample(DishProcessSpecification.HappyPath);
        var rushAware = RunDelegatedSample(DishProcessSpecification.RushAware);

        Assert.True(happyPathOnly.PlateActions > 0);
        Assert.Equal(0, happyPathOnly.GlassActions);
        Assert.True(rushAware.GlassActions > 0);
        Assert.Equal(0, rushAware.PlateActions);
    }

    [Fact]
    public void UShapedCellReducesTravelAndIncreasesDelegatedActionOpportunity()
    {
        var linear = RunLayoutWorker(DishStationLayout.Linear);
        var flowCell = RunLayoutWorker(DishStationLayout.UShapedCell);

        Assert.True(flowCell.NewHire.ActionsCompleted > linear.NewHire.ActionsCompleted);
        Assert.True(flowCell.Layout.NewHireTravelSteps < linear.Layout.NewHireTravelSteps);
    }

    private static DishStationSnapshot RunDeterministicEpisode(int seed)
    {
        var world = new DishStationWorld(seed);
        world.Schedule(new SetRushCommand(new(2), true));
        Advance(world, 100);
        return world.Snapshot() with { LatestNotification = null };
    }

    private static void RunManualPlate(DishStationWorld world)
    {
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        Advance(world, world.Configuration.WasherCycleTicks);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.DryAndRestock, DishKind.Plate)).Success);
    }

    private static NewHireSnapshot RunDelegatedSample(DishProcessSpecification specification)
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true));
        world.ExecuteNow(new TrainNewHireCommand(world.Tick, specification));
        world.ExecuteNow(new SetRushCommand(world.Tick, true));
        Advance(world, 15);
        return world.Snapshot().NewHire;
    }

    private static DishStationSnapshot RunWorkerForLayout(DishStationPlacements placements, bool placeCustom)
    {
        var configuration = new DishStationScenarioConfiguration
        {
            InitialDirty = new DishCounts(30, 0, 0),
            ArrivalIntervalTicks = 1000,
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.HappyPath,
            WorkerActionIntervalTicks = 4,
            FlowCellWorkerActionIntervalTicks = 2,
        };
        var world = new DishStationWorld(42, configuration);
        if (placeCustom)
            foreach (var fixture in Enum.GetValues<DishStationFixture>())
                Assert.True(world.ExecuteNow(new PlaceDishStationFixtureCommand(world.Tick, fixture, placements.At(fixture))).Success);
        Advance(world, 20);
        return world.Snapshot();
    }

    private static AutomationSnapshot RunAutomationIncidentReplay()
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.ReportedReadyOnly));
        world.Advance();
        world.ExecuteNow(new InjectStickyReadyFaultCommand(world.Tick));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate));
        world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate));
        world.Advance();

        Assert.True(world.Snapshot().Automation.Incident.Recorded);
        world.ExecuteNow(new InspectAutomationIncidentCommand(world.Tick));
        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));
        world.ExecuteNow(new ConfigureWasherAutomationCommand(world.Tick, WasherAutomationPolicy.CorroboratedReady));
        world.Advance();
        world.ExecuteNow(new ReplayAutomationIncidentCommand(world.Tick));
        return world.Snapshot().Automation;
    }

    private static DishStationSnapshot RunLayoutWorker(DishStationLayout layout)
    {
        var world = new DishStationWorld();
        world.ExecuteNow(new ConfigureDishStationLayoutCommand(world.Tick, layout));
        world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true));
        world.ExecuteNow(new TrainNewHireCommand(world.Tick, DishProcessSpecification.HappyPath));
        Advance(world, 20);
        return world.Snapshot();
    }

    private static void Advance(DishStationWorld world, int ticks)
    {
        for (var i = 0; i < ticks; i++) world.Advance();
    }
}
