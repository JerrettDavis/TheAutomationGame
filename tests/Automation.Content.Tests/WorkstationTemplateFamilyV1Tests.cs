using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Content.Tests;

public sealed class WorkstationTemplateFamilyV1Tests
{
    [Fact]
    public void SupportedFamiliesExpandDeterministicallyToTypedBehaviors()
    {
        var cases = new[]
        {
            Case("manual", new() { ["workstation-slug"] = "manual-proof" }, WorkstationTemplateFamily.Manual),
            Case("batch", new() { ["workstation-slug"] = "batch-proof", ["cycle-ticks"] = "3" }, WorkstationTemplateFamily.Batch),
            Case("buffer", new() { ["workstation-slug"] = "buffer-proof", ["capacity"] = "2" }, WorkstationTemplateFamily.Buffer),
            Case("inspection", new() { ["workstation-slug"] = "inspection-proof" }, WorkstationTemplateFamily.Inspection),
            Case("service", new() { ["workstation-slug"] = "service-proof", ["demand-kind"] = "plate", ["request-interval-ticks"] = "2" }, WorkstationTemplateFamily.Service),
        };

        foreach (var item in cases)
        {
            var template = ContentTemplateCompilerV1.CompileFile(ContentTestPaths.WorkstationTemplate(item.Name));
            var first = template.Expand(item.Parameters);
            var second = template.Expand(item.Parameters);
            var workstation = Assert.Single(first.Catalog.Workstations);

            Assert.Equal(item.Family, workstation.Behavior!.Family);
            Assert.Equal(first.ExpandedYaml, second.ExpandedYaml);
            Assert.Equal(first.Catalog.Manifest.Sha256, second.Catalog.Manifest.Sha256);
            Assert.Equal(first.ExpansionSha256, second.ExpansionSha256);
            if (item.Name == "batch")
            {
                var changedParameters = new Dictionary<string, string>(item.Parameters, StringComparer.Ordinal)
                {
                    ["cycle-ticks"] = "4",
                };
                Assert.NotEqual(first.Catalog.Manifest.Sha256, template.Expand(changedParameters).Catalog.Manifest.Sha256);
            }
        }
    }

    [Fact]
    public void BatchTimingAndBufferCapacityFlowThroughAuthoritativeWorldRules()
    {
        var workstations = new[]
        {
            Expand("batch", new() { ["workstation-slug"] = "washer-proof", ["cycle-ticks"] = "3" }),
            Expand("buffer", new() { ["workstation-slug"] = "rack-proof", ["capacity"] = "1" }),
        };
        var configuration = DishStationWorkstationTemplateAdapter.Apply(
            DishStationFirstHoursContent.ScenarioConfiguration with
            {
                InitialDirty = new(2, 0, 0),
                InitialAvailable = new(0, 0, 0),
                InitialRushEnabled = false,
            },
            workstations);
        var world = new DishStationWorld(42, configuration);

        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.False(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.StartWasher, DishKind.Plate)).Success);
        world.Advance();
        world.Advance();
        Assert.True(world.WasherRunning);
        world.Advance();
        Assert.False(world.WasherRunning);
        Assert.Equal(1, world.At(DishState.WashedInMachine).Plates);
    }

    [Fact]
    public void ManualInspectionAndServiceFamiliesMatchExistingAuthoritativeSemantics()
    {
        var manual = Assert.IsType<ManualWorkstationBehaviorContentDefinition>(Expand("manual",
            new() { ["workstation-slug"] = "manual-proof" }).Behavior);
        Assert.Equal("scrape", manual.Action);

        var service = Expand("service", new()
        {
            ["workstation-slug"] = "service-proof",
            ["demand-kind"] = "plate",
            ["request-interval-ticks"] = "2",
        });
        var configuration = DishStationWorkstationTemplateAdapter.Apply(
            DishStationFirstHoursContent.ScenarioConfiguration with
            {
                InitialDirty = new(1, 0, 0),
                InitialAvailable = new(1, 0, 0),
                InitialRushEnabled = true,
                ArrivalIntervalTicks = 100,
            }, [service]);
        var world = new DishStationWorld(42, configuration);

        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.Equal(1, world.At(DishState.Scraped).Plates);

        var inspection = Assert.IsType<InspectionWorkstationBehaviorContentDefinition>(Expand("inspection",
            new() { ["workstation-slug"] = "inspection-proof" }).Behavior);
        Assert.Equal("state-counts", inspection.Observation);
        var beforeInspection = world.Snapshot().Dishes.ToArray();
        MoveTo(world, DishStationFixture.Service);
        Assert.True(world.ExecuteNow(new InspectDishStationFixtureCommand(world.Tick, DishStationFixture.Service, DishKind.Plate)).Success);
        Assert.Equal(beforeInspection, world.Snapshot().Dishes);

        world.Advance();
        world.Advance();
        Assert.Equal(0, world.At(DishState.Available).Plates);
        Assert.Equal(0, world.ServiceShortages);
        Assert.Equal(1, world.At(DishState.Dirty).Plates);
    }

    [Fact]
    public void InvalidFamilyStateAndMultipleFamiliesFailAtTargetedPaths()
    {
        var batch = File.ReadAllText(ContentTestPaths.WorkstationTemplate("batch"))
            .Replace("input_state: racked", "input_state: washed_in_machine", StringComparison.Ordinal)
            .Replace("states: [racked, washed_in_machine]", "states: [racked, washed_in_machine]", StringComparison.Ordinal);
        var template = ContentTemplateCompilerV1.Compile(batch, "bad-batch.template.yaml");
        var stateError = Assert.Throws<ContentCompilationException>(() => template.Expand(new Dictionary<string, string>
        {
            ["workstation-slug"] = "bad-batch",
            ["cycle-ticks"] = "1",
        }));
        Assert.Contains(stateError.Diagnostics, item => item.Path.EndsWith(".behavior", StringComparison.Ordinal));

        var unsupportedCapacity = ContentTemplateCompilerV1.Compile(
            File.ReadAllText(ContentTestPaths.WorkstationTemplate("batch")).Replace("capacity: 1", "capacity: 2", StringComparison.Ordinal),
            "unsupported-capacity.template.yaml");
        var capacityError = Assert.Throws<ContentCompilationException>(() => unsupportedCapacity.Expand(new Dictionary<string, string>
        {
            ["workstation-slug"] = "unsupported-capacity",
            ["cycle-ticks"] = "1",
        }));
        Assert.Contains(capacityError.Diagnostics, item => item.Path.EndsWith(".batch.capacity", StringComparison.Ordinal) && item.Message.Contains("capacity 1", StringComparison.Ordinal));

        var duplicate = File.ReadAllText(ContentTestPaths.WorkstationTemplate("buffer"))
            .ReplaceLineEndings("\n")
            .Replace("      buffer:\n", "      manual:\n        action: rack\n      buffer:\n", StringComparison.Ordinal);
        var duplicateTemplate = ContentTemplateCompilerV1.Compile(duplicate, "duplicate-family.template.yaml");
        var duplicateError = Assert.Throws<ContentCompilationException>(() => duplicateTemplate.Expand(new Dictionary<string, string>
        {
            ["workstation-slug"] = "duplicate",
            ["capacity"] = "1",
        }));
        Assert.Contains(duplicateError.Diagnostics, item => item.Path.EndsWith(".behavior", StringComparison.Ordinal) && item.Message.Contains("at most one", StringComparison.Ordinal));
    }

    [Fact]
    public void TransportIsExplicitlyUnsupportedUntilQueuedMovementExists()
    {
        Assert.False(WorkstationTemplateFamiliesV1.IsSupported(WorkstationTemplateFamily.Transport));
        Assert.Contains("queued work-item movement primitive", WorkstationTemplateFamiliesV1.UnsupportedReason(WorkstationTemplateFamily.Transport), StringComparison.Ordinal);
    }

    private static WorkstationContentDefinition Expand(string family, Dictionary<string, string> parameters) =>
        Assert.Single(ContentTemplateCompilerV1.CompileFile(ContentTestPaths.WorkstationTemplate(family)).Expand(parameters).Catalog.Workstations);

    private static void MoveTo(DishStationWorld world, DishStationFixture fixture)
    {
        var path = world.Topology.FindPath(world.PlayerCell, world.Topology.InteractionPort(fixture));
        foreach (var cell in path.Skip(1))
            Assert.True(world.ExecuteNow(new MovePlayerCommand(world.Tick, cell)).Success);
    }

    private static (string Name, Dictionary<string, string> Parameters, WorkstationTemplateFamily Family) Case(
        string name, Dictionary<string, string> parameters, WorkstationTemplateFamily family) => (name, parameters, family);

}
