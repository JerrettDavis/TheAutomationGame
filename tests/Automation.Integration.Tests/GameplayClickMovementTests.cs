using Automation.Client.Stride;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class GameplayClickMovementTests
{
    [Fact]
    public void ClickRouteFeedsOnlyLegalStepsThroughWorldAuthority()
    {
        var world = IntegrationTestScenario.World();
        var route = new GameplayClickRoute();
        var destination = new FloorCell(8, 1);

        Assert.True(route.Begin(world.PlayerCell, destination, world.Placements));
        var issued = new List<FloorCell>();
        while (route.TakeNext(world.PlayerCell, world.Placements, world.Tick) is { } command)
        {
            issued.Add(command.Destination);
            Assert.True(world.ExecuteNow(command).Success);
            Assert.True(world.Topology.IsWalkable(world.PlayerCell));
        }

        Assert.NotEmpty(issued);
        Assert.Equal(destination, world.PlayerCell);
        Assert.Equal(0, route.PendingSteps);
    }

    [Fact]
    public void ClickingFixtureFootprintRoutesToItsInteractionPort()
    {
        var world = IntegrationTestScenario.World();
        var route = new GameplayClickRoute();
        var fixture = DishStationFixture.Washer;

        Assert.True(route.Begin(world.PlayerCell, world.Placements.At(fixture), world.Placements));
        while (route.TakeNext(world.PlayerCell, world.Placements, world.Tick) is { } command)
            Assert.True(world.ExecuteNow(command).Success);

        Assert.Equal(world.Topology.InteractionPort(fixture), world.PlayerCell);
        Assert.NotEqual(world.Placements.At(fixture), world.PlayerCell);
    }

    [Fact]
    public void SameClickProducesSameDeterministicAuthoritativeCommandSequence()
    {
        var placements = DishStationPlacements.Linear;

        Assert.Equal(
            CommandsFrom(new FloorCell(6, 1), new FloorCell(8, 1), placements),
            CommandsFrom(new FloorCell(6, 1), new FloorCell(8, 1), placements));
    }

    [Fact]
    public void DirectInputCanCancelPendingClickMovement()
    {
        var route = new GameplayClickRoute();

        Assert.True(route.Begin(new FloorCell(1, 2), new FloorCell(11, 7), DishStationPlacements.Linear));
        Assert.True(route.PendingSteps > 0);

        route.Cancel();

        Assert.Equal(0, route.PendingSteps);
        Assert.Null(route.TakeNext(new FloorCell(1, 2), DishStationPlacements.Linear, new SimulationTick(1)));
    }

    private static FloorCell[] CommandsFrom(FloorCell start, FloorCell destination, DishStationPlacements placements)
    {
        var route = new GameplayClickRoute();
        Assert.True(route.Begin(start, destination, placements));
        var current = start;
        var commands = new List<FloorCell>();
        while (route.TakeNext(current, placements, new SimulationTick(commands.Count)) is { } command)
        {
            commands.Add(command.Destination);
            current = command.Destination;
        }
        return [.. commands];
    }
}
