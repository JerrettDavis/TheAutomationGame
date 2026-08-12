using System.Text.Json;
using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Content.Tests;

public sealed class IncidentTemplateFamilyV1Tests
{
    private static readonly string[] Families =
    [
        "process-delay", "capacity-loss", "bad-sensor",
        "blocked-resource", "worker-absence", "demand-spike",
    ];

    [Fact]
    public void AllFamiliesExpandDeterministicallyToTypedScheduledIncidents()
    {
        var expectedKinds = Enum.GetValues<DishStationIncidentKind>();
        for (var index = 0; index < Families.Length; index++)
        {
            var family = Families[index];
            var template = ContentTemplateCompilerV1.CompileFile(ContentTestPaths.IncidentTemplate(family));
            var parameters = Parameters(family);
            var first = template.Expand(parameters, "incident-proof-42");
            var second = template.Expand(new Dictionary<string, string>(parameters.Reverse(), StringComparer.Ordinal), "incident-proof-42");
            var definition = Assert.Single(first.Catalog.Incidents);
            var schedule = DishStationIncidentContentAdapter.ToSchedule(definition).Validate();

            Assert.Equal(expectedKinds[index], definition.Effect.Kind);
            Assert.Equal(definition.TriggerAt, schedule.TriggerAt);
            Assert.Equal(definition.Id.Value, schedule.Incident.Id.Value);
            Assert.Equal(first.ExpandedYaml, second.ExpandedYaml);
            Assert.Equal(first.Catalog.Manifest.Sha256, second.Catalog.Manifest.Sha256);
            Assert.Equal(first.ExpansionSha256, second.ExpansionSha256);
        }
    }

    [Fact]
    public void SeedChangesOnlyDeclaredTriggerForEveryFamily()
    {
        foreach (var family in Families)
        {
            var template = ContentTemplateCompilerV1.CompileFile(ContentTestPaths.IncidentTemplate(family));
            var parameters = Parameters(family);
            var first = template.Expand(parameters, "seed-0");
            ContentTemplateExpansionResultV1? changed = null;
            for (var seed = 1; seed < 100; seed++)
            {
                var candidate = template.Expand(parameters, $"seed-{seed}");
                if (candidate.Provenance.VariantSelections["trigger-tick"] != first.Provenance.VariantSelections["trigger-tick"])
                {
                    changed = candidate;
                    break;
                }
            }

            Assert.NotNull(changed);
            Assert.Equal(NormalizeTrigger(first.ExpandedYaml), NormalizeTrigger(changed!.ExpandedYaml));
            var firstIncident = Assert.Single(first.Catalog.Incidents);
            var changedIncident = Assert.Single(changed.Catalog.Incidents);
            Assert.Equal(firstIncident with { TriggerAt = default }, changedIncident with { TriggerAt = default });
            Assert.NotEqual(firstIncident.TriggerAt, changedIncident.TriggerAt);
        }
    }

    [Fact]
    public void EveryFamilyChangesAuthoritativeBehaviorAndRecoversWithTrace()
    {
        ProveProcessDelay();
        ProveCapacityLoss();
        ProveBadSensor();
        ProveBlockedResource();
        ProveWorkerAbsence();
        ProveDemandSpike();
    }

    [Fact]
    public void ScheduledIncidentTimelineAndReplayAreDeterministic()
    {
        var definition = Expand("demand-spike", "timeline-proof");
        var schedule = DishStationIncidentContentAdapter.ToSchedule(definition);
        var first = World(BaseConfiguration() with { InitialAvailable = new(8, 0, 0) });
        var second = World(BaseConfiguration() with { InitialAvailable = new(8, 0, 0) });
        first.Schedule(new TriggerDishStationIncidentCommand(schedule.TriggerAt, schedule.Incident));
        second.Schedule(new TriggerDishStationIncidentCommand(schedule.TriggerAt, schedule.Incident));

        Advance(first, checked((int)schedule.TriggerAt.Value + 1));
        Advance(second, checked((int)schedule.TriggerAt.Value + 1));
        Assert.Equal(Json(first.Snapshot()), Json(second.Snapshot()));

        var restored = DishStationWorld.Restore(first.CreateReplaySave());
        Assert.Equal(Json(first.Snapshot()), Json(restored.Snapshot()));
        Advance(first, 8);
        Advance(second, 8);
        Advance(restored, 8);
        Assert.Equal(Json(first.Snapshot()), Json(second.Snapshot()));
        Assert.Equal(Json(first.Snapshot()), Json(restored.Snapshot()));
        Assert.Collection(first.Snapshot().Incidents.Trace,
            started => Assert.Equal(DishStationIncidentPhase.Started, started.Phase),
            recovered => Assert.Equal(DishStationIncidentPhase.Recovered, recovered.Phase));
    }

    [Fact]
    public void InvalidEffectsFailAtTargetedSemanticPaths()
    {
        var source = File.ReadAllText(ContentTestPaths.IncidentTemplate("demand-spike")).ReplaceLineEndings("\n");
        var duplicate = source.Replace(
            "      demand_spike:\n",
            "      worker_absence:\n        duration_ticks: 2\n        worker: new-hire\n      demand_spike:\n",
            StringComparison.Ordinal);
        var duplicateTemplate = ContentTemplateCompilerV1.Compile(duplicate, "duplicate-incident.template.yaml");
        var duplicateError = Assert.Throws<ContentCompilationException>(() =>
            duplicateTemplate.Expand(Parameters("demand-spike"), "proof"));
        Assert.Contains(duplicateError.Diagnostics, item =>
            item.Path.EndsWith(".effect", StringComparison.Ordinal) && item.Message.Contains("exactly one", StringComparison.Ordinal));

        var invalid = source.Replace("interval_ticks: {{parameter:interval-ticks}}", "interval_ticks: 0", StringComparison.Ordinal)
            .Replace("  interval-ticks: positive_integer\n", "", StringComparison.Ordinal);
        var invalidTemplate = ContentTemplateCompilerV1.Compile(invalid, "invalid-incident.template.yaml");
        var invalidParameters = Parameters("demand-spike");
        invalidParameters.Remove("interval-ticks");
        var invalidError = Assert.Throws<ContentCompilationException>(() => invalidTemplate.Expand(invalidParameters, "proof"));
        Assert.Contains(invalidError.Diagnostics, item =>
            item.Path.EndsWith(".demand_spike.interval_ticks", StringComparison.Ordinal) && item.Message.Contains("positive", StringComparison.Ordinal));
    }

    private static void ProveProcessDelay()
    {
        var world = World(BaseConfiguration() with { WasherCycleTicks = 3, InitialDirty = new(1, 0, 0) });
        PrepareRacked(world);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        Trigger(world, "process-delay");
        Advance(world, 4);
        Assert.True(world.WasherRunning);
        world.Advance();
        Assert.False(world.WasherRunning);
        AssertRecovered(world, DishStationIncidentKind.ProcessDelay);
    }

    private static void ProveCapacityLoss()
    {
        var world = World(BaseConfiguration() with { RackCapacity = 2, InitialDirty = new(2, 0, 0) });
        PrepareRacked(world);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Trigger(world, "capacity-loss");
        Assert.False(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        Advance(world, 3);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        AssertRecovered(world, DishStationIncidentKind.CapacityLoss);
    }

    private static void ProveBadSensor()
    {
        var world = World(BaseConfiguration() with { WasherCycleTicks = 10, InitialDirty = new(1, 0, 0) });
        PrepareRacked(world);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        Assert.False(world.WasherReportedReady);
        Trigger(world, "bad-sensor");
        Assert.True(world.WasherReportedReady);
        Advance(world, 3);
        Assert.False(world.WasherReportedReady);
        AssertRecovered(world, DishStationIncidentKind.BadSensor);
    }

    private static void ProveBlockedResource()
    {
        var world = World(BaseConfiguration() with { InitialDirty = new(1, 0, 0) });
        PrepareRacked(world);
        Trigger(world, "blocked-resource");
        Assert.False(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        Advance(world, 3);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        AssertRecovered(world, DishStationIncidentKind.BlockedResource);
    }

    private static void ProveWorkerAbsence()
    {
        var world = World(BaseConfiguration() with
        {
            InitialDirty = new(2, 0, 0),
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.FullyDocumented,
            WorkerActionIntervalTicks = 1,
        });
        Trigger(world, "worker-absence");
        world.Advance();
        Assert.Equal(0, world.Snapshot().NewHire.ActionsCompleted);
        Advance(world, 2);
        Assert.True(world.Snapshot().NewHire.ActionsCompleted > 0);
        AssertRecovered(world, DishStationIncidentKind.WorkerAbsence);
    }

    private static void ProveDemandSpike()
    {
        var world = World(BaseConfiguration() with { InitialAvailable = new(3, 0, 0) });
        Trigger(world, "demand-spike");
        world.Advance();
        Assert.Equal(2, world.At(DishState.Available).Plates);
        Advance(world, 2);
        Assert.Equal(1, world.At(DishState.Available).Plates);
        AssertRecovered(world, DishStationIncidentKind.DemandSpike);
    }

    private static void Trigger(DishStationWorld world, string family)
    {
        var incident = DishStationIncidentContentAdapter.ToSchedule(Expand(family, $"{family}-behavior")).Incident;
        Assert.True(world.ExecuteNow(new TriggerDishStationIncidentCommand(world.Tick, incident)).Success);
    }

    private static void PrepareRacked(DishStationWorld world)
    {
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
    }

    private static void AssertRecovered(DishStationWorld world, DishStationIncidentKind kind)
    {
        Assert.Empty(world.Snapshot().Incidents.Active);
        Assert.Collection(world.Snapshot().Incidents.Trace,
            started => { Assert.Equal(kind, started.Kind); Assert.Equal(DishStationIncidentPhase.Started, started.Phase); },
            recovered => { Assert.Equal(kind, recovered.Kind); Assert.Equal(DishStationIncidentPhase.Recovered, recovered.Phase); });
    }

    private static IncidentContentDefinition Expand(string family, string seed) => Assert.Single(
        ContentTemplateCompilerV1.CompileFile(ContentTestPaths.IncidentTemplate(family))
            .Expand(Parameters(family), seed).Catalog.Incidents);

    private static Dictionary<string, string> Parameters(string family)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["incident-slug"] = $"{family}-proof",
            ["duration-ticks"] = "3",
        };
        if (family == "process-delay") result["added-cycle-ticks"] = "2";
        if (family == "capacity-loss") result["lost-slots"] = "1";
        if (family == "demand-spike")
        {
            result["demand-kind"] = "plate";
            result["interval-ticks"] = "1";
        }
        return result;
    }

    private static DishStationScenarioConfiguration BaseConfiguration() => DishStationFirstHoursContent.ScenarioConfiguration with
    {
        InitialDirty = new(0, 0, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        RackCapacity = 2,
        WasherCycleTicks = 5,
        WorkerActionIntervalTicks = 1,
        FlowCellWorkerActionIntervalTicks = 1,
        DemandKind = DishKind.Glass,
        DemandIntervalTicks = 1000,
        InitialRushEnabled = false,
        InitialNewHireEnabled = false,
        InitialNewHireSpecification = default,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
    };

    private static DishStationWorld World(DishStationScenarioConfiguration configuration) => new(42, configuration);
    private static void Advance(DishStationWorld world, int ticks) { for (var index = 0; index < ticks; index++) world.Advance(); }
    private static string Json(DishStationSnapshot snapshot) => JsonSerializer.Serialize(snapshot);
    private static string NormalizeTrigger(string yaml) => System.Text.RegularExpressions.Regex.Replace(
        yaml, "trigger_at_tick: [0-9]+", "trigger_at_tick: <declared-variant>");
}
