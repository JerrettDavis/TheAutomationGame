using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class DishStationInteractionTests
{
    [Fact]
    public void ContextWorkRejectsAnOutOfRangeTargetWithoutMovingOrMutating()
    {
        var world = TestDishStationScenario.World();
        var originalCell = world.PlayerCell;
        var dirtyPlates = world.At(DishState.Dirty).Plates;

        var result = world.ExecuteNow(new InteractWithDishStationFixtureCommand(
            world.Tick,
            DishStationFixture.Rack,
            DishKind.Plate));

        Assert.False(result.Success);
        Assert.Contains("closer", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(originalCell, world.PlayerCell);
        Assert.Equal(dirtyPlates, world.At(DishState.Dirty).Plates);
    }

    [Fact]
    public void ContextWorkAtTheInteractionCellPerformsTheAuthoritativeAction()
    {
        var world = TestDishStationScenario.World();

        var result = world.ExecuteNow(new InteractWithDishStationFixtureCommand(
            world.Tick,
            DishStationFixture.Scrape,
            DishKind.Plate));

        Assert.True(result.Success);
        Assert.Equal(5, world.At(DishState.Dirty).Plates);
        Assert.Equal(1, world.At(DishState.Scraped).Plates);
        Assert.Equal(world.Topology.InteractionPort(DishStationFixture.Scrape), world.PlayerCell);
    }

    [Fact]
    public void InspectionReportsStateWithoutMutatingTheWorld()
    {
        var world = TestDishStationScenario.World();
        var before = world.Snapshot();

        var result = world.ExecuteNow(new InspectDishStationFixtureCommand(
            world.Tick,
            DishStationFixture.Scrape,
            DishKind.Plate));
        var after = world.Snapshot();

        Assert.True(result.Success);
        Assert.Contains("Dirty P6 G2", result.Message, StringComparison.Ordinal);
        Assert.Equal(before.Dishes, after.Dishes);
        Assert.Equal(before.Layout, after.Layout);
        Assert.Equal(before.RecentTransitions.ToArray(), after.RecentTransitions.ToArray());
    }

    [Fact]
    public void ServiceCanBeInspectedInRangeButHasNoWorkAction()
    {
        var world = TestDishStationScenario.World();
        MoveToInteractionPort(world, DishStationFixture.Service);

        var interaction = world.InteractionAt(DishStationFixture.Service, DishKind.Glass);
        var work = world.ExecuteNow(new InteractWithDishStationFixtureCommand(world.Tick, DishStationFixture.Service, DishKind.Glass));
        var inspect = world.ExecuteNow(new InspectDishStationFixtureCommand(world.Tick, DishStationFixture.Service, DishKind.Glass));

        Assert.False(interaction.CanWork);
        Assert.True(interaction.CanInspect);
        Assert.Equal(DishStationInteractionBlockReason.InspectionOnly, interaction.WorkBlockReason);
        Assert.False(work.Success);
        Assert.True(inspect.Success);
        Assert.Contains("Service supply", inspect.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void InteractionProjectionProvidesAConcreteDisabledReason()
    {
        var world = TestDishStationScenario.World();
        MoveToInteractionPort(world, DishStationFixture.Rack);

        var interaction = world.InteractionAt(DishStationFixture.Rack, DishKind.Plate);

        Assert.False(interaction.CanWork);
        Assert.Equal(DishState.Scraped, interaction.RequiredState);
        Assert.Equal(DishStationInteractionBlockReason.NoDishReady, interaction.WorkBlockReason);
    }

    [Fact]
    public void InteractionCommandsRoundTripThroughReplaySerialization()
    {
        ISimulationCommand interact = new InteractWithDishStationFixtureCommand(new SimulationTick(4), DishStationFixture.Washer, DishKind.Glass);
        ISimulationCommand inspect = new InspectDishStationFixtureCommand(new SimulationTick(5), DishStationFixture.Service, DishKind.Tray);

        Assert.Equal(interact, RecordedSimulationCommand.FromCommand(interact).ToCommand());
        Assert.Equal(inspect, RecordedSimulationCommand.FromCommand(inspect).ToCommand());
    }

    private static void MoveToInteractionPort(DishStationWorld world, DishStationFixture fixture)
    {
        var destination = world.Topology.InteractionPort(fixture);
        var path = world.Topology.FindPath(world.PlayerCell, destination);
        Assert.NotEmpty(path);
        foreach (var cell in path.Skip(1))
            Assert.True(world.ExecuteNow(new MovePlayerCommand(world.Tick, cell)).Success);
    }
}
