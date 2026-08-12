using System.Text.Json;
using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class DishStationScenarioContentTests
{
    [Fact]
    public void ProductionScenarioCompilesToLegacyReferenceConfiguration()
    {
        var compiled = DishStationFirstHoursContent.ScenarioConfiguration;

        Assert.Equal(LegacyReference(), compiled);
        Assert.Same(compiled, DishStationFirstHoursContent.Scenario.DishStation);
    }

    [Fact]
    public void CompiledScenarioMatchesLegacyFixedSeedRunReplayAndFuture()
    {
        var legacy = ScriptedWorld(LegacyReference());
        var compiled = ScriptedWorld(DishStationFirstHoursContent.ScenarioConfiguration);

        Assert.Equal(Json(legacy.Snapshot()), Json(compiled.Snapshot()));
        Assert.Equal(legacy.Notifications, compiled.Notifications);
        Assert.Equal(Json(legacy.CreateReplaySave()), Json(compiled.CreateReplaySave()));

        var restored = DishStationSaveStore.Deserialize(DishStationSaveStore.Serialize(compiled));
        Assert.Equal(DishStationFirstHoursContent.ScenarioConfiguration, restored.Configuration);
        for (var tick = 0; tick < 25; tick++)
        {
            legacy.Advance();
            restored.Advance();
        }
        Assert.Equal(Json(legacy.Snapshot()), Json(restored.Snapshot()));
        Assert.Equal(legacy.Notifications, restored.Notifications);
    }

    [Fact]
    public void ContentOnlyScenarioChangeProducesDifferentConfigurationAndManifest()
    {
        var yaml = File.ReadAllText(ProductionScenarioPath());
        var changed = yaml.Replace("rack_capacity: 12", "rack_capacity: 7", StringComparison.Ordinal);
        Assert.NotEqual(yaml, changed);

        var baseline = ContentCompilerV1.Compile(yaml, "first-shift.yaml");
        var modified = ContentCompilerV1.Compile(changed, "changed-first-shift.yaml");
        var scenario = modified.Scenarios.Single(item => item.Id.Value == DishStationFirstHoursContent.ScenarioId);

        Assert.Equal(7, scenario.DishStation!.RackCapacity);
        Assert.NotEqual(baseline.Manifest.Sha256, modified.Manifest.Sha256);
    }

    [Fact]
    public void InvalidAuthoredScenarioValueFailsAtItsSemanticPath()
    {
        var yaml = File.ReadAllText(ProductionScenarioPath());
        var changed = yaml.Replace("washer_cycle_ticks: 20", "washer_cycle_ticks: 0", StringComparison.Ordinal);

        var exception = Assert.Throws<ContentCompilationException>(() =>
            ContentCompilerV1.Compile(changed, "bad-scenario.yaml"));

        Assert.Contains(exception.Diagnostics, diagnostic =>
            diagnostic.Path.EndsWith("dish_station.washer_cycle_ticks", StringComparison.Ordinal) &&
            diagnostic.Message.Contains("positive integer", StringComparison.Ordinal));
    }

    private static DishStationWorld ScriptedWorld(DishStationScenarioConfiguration scenario)
    {
        var world = new DishStationWorld(42, scenario);
        world.ExecuteNow(new CompleteIntroCommand(world.Tick, GuidanceMode.Contextual));
        world.Schedule(new PerformDishActionCommand(new(1), DishAction.Scrape, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(2), DishAction.Rack, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(3), DishAction.StartWasher, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(24), DishAction.Unload, DishKind.Plate));
        world.Schedule(new PerformDishActionCommand(new(25), DishAction.DryAndRestock, DishKind.Plate));
        world.Schedule(new SetRushCommand(new(26), true));
        world.Schedule(new InspectProcessCommand(new(31)));
        world.Schedule(new ConfirmBottleneckCommand(new(32), DishState.Dirty));
        world.Schedule(new ConfigureDishStationLayoutCommand(new(33), DishStationLayout.UShapedCell));
        world.Schedule(new PerformDishActionCommand(new(33), DishAction.Scrape, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(34), DishAction.Rack, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(35), DishAction.StartWasher, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(56), DishAction.Unload, DishKind.Glass));
        world.Schedule(new PerformDishActionCommand(new(57), DishAction.DryAndRestock, DishKind.Glass));
        world.Schedule(new SetNewHireEnabledCommand(new(61), true));
        world.Schedule(new TrainNewHireCommand(new(62), DishProcessSpecification.HappyPath));
        world.Schedule(new TrainNewHireCommand(new(76), DishProcessSpecification.RushAware));
        world.Schedule(new TrainNewHireCommand(new(158), DishProcessSpecification.FullyDocumented));
        world.Schedule(new BeginAutomationRuleEditCommand(new(200)));
        world.Schedule(new SetAutomationRuleEnabledCommand(new(200), true));
        world.Schedule(new ApplyAutomationRuleEditCommand(new(200)));
        world.Schedule(new SaveAutomationRulePresetCommand(new(200), AutomationPresetSlot.Baseline));
        world.Schedule(new InspectAutomationIncidentCommand(new(241)));
        world.Schedule(new ReplayAutomationIncidentCommand(new(242)));
        world.Schedule(new BeginAutomationRuleEditCommand(new(243)));
        world.Schedule(new ToggleAutomationRuleConditionCommand(new(243), AutomationObservable.PhysicalReady));
        world.Schedule(new ApplyAutomationRuleEditCommand(new(243)));
        world.Schedule(new SaveAutomationRulePresetCommand(new(243), AutomationPresetSlot.Variant));
        world.Schedule(new ReplayAutomationIncidentCommand(new(244)));
        world.Schedule(new RunAutomationRuleComparisonCommand(new(244)));
        world.Schedule(new ConfigureDishSupplyCommand(new(245), DishState.Available, DishKind.Glass, 3));
        world.Schedule(new StartShiftTrialCommand(new(245)));
        for (var tick = 0; tick < 250; tick++) world.Advance();
        return world;
    }

    private static DishStationScenarioConfiguration LegacyReference() => new()
    {
        InitialDirty = new(6, 2, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 30,
        GlassEveryArrivals = 3,
        RackCapacity = 12,
        WasherCycleTicks = 20,
        WorkerActionIntervalTicks = 5,
        FlowCellWorkerActionIntervalTicks = 4,
        StickyReadyFaultAfterAutomatedStarts = 2,
        StickyReadyFaultPermillePerStart = 0,
        DemandKind = DishKind.Glass,
        DemandIntervalTicks = 15,
        InitialRushEnabled = false,
        InitialNewHireEnabled = false,
        InitialNewHireSpecification = default,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
        InitialLayout = DishStationLayout.Linear,
    };

    private static string ProductionScenarioPath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "TheAutomationGame.sln")))
            directory = directory.Parent;
        Assert.NotNull(directory);
        return Path.Combine(directory!.FullName, "content", "restaurant", "first-shift.yaml");
    }

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
