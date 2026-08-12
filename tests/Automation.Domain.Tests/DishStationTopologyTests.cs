using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class DishStationTopologyTests
{
    [Fact]
    public void FixtureFootprintsAreBlockedAndExposeDeterministicWalkablePorts()
    {
        var topology = new DishStationTopology(DishStationPlacements.Linear);

        foreach (var fixture in Enum.GetValues<DishStationFixture>())
        {
            Assert.False(topology.IsWalkable(DishStationPlacements.Linear.At(fixture)));
            Assert.True(topology.IsWalkable(topology.InteractionPort(fixture)));
        }
        Assert.Equal(new FloorCell(7, 2), topology.InteractionPort(DishStationFixture.Washer));
        Assert.Equal(new FloorCell(11, 7), topology.InteractionPort(DishStationFixture.Service));
        Assert.True(topology.AllInteractionPortsConnected());
    }

    [Fact]
    public void DeterministicRouteDetoursAroundWasherFootprint()
    {
        var topology = new DishStationTopology(DishStationPlacements.Linear);
        var expected = new[]
        {
            new FloorCell(6, 1),
            new FloorCell(6, 0),
            new FloorCell(7, 0),
            new FloorCell(8, 0),
            new FloorCell(8, 1),
        };

        var first = topology.FindPath(new FloorCell(6, 1), new FloorCell(8, 1));
        var second = topology.FindPath(new FloorCell(6, 1), new FloorCell(8, 1));

        Assert.Equal(expected, first);
        Assert.Equal(first, second);
        Assert.DoesNotContain(DishStationPlacements.Linear.Washer, first);
    }

    [Fact]
    public void DiagonalStepCannotCutAcrossFixtureCorner()
    {
        var placements = DishStationPlacements.Linear.With(DishStationFixture.Washer, new FloorCell(7, 2));
        var topology = new DishStationTopology(placements);

        Assert.False(topology.CanStep(new FloorCell(6, 2), new FloorCell(7, 1)));
        Assert.Empty(topology.FindPath(new FloorCell(6, 2), placements.Washer));
    }

    [Fact]
    public void LayoutWithASealedFixtureHasNoConnectedInteractionTopology()
    {
        var placements = new DishStationPlacements(
            new FloorCell(1, 1),
            new FloorCell(1, 2),
            new FloorCell(0, 1),
            new FloorCell(2, 1),
            new FloorCell(1, 0),
            new FloorCell(11, 6));
        var topology = new DishStationTopology(placements);

        Assert.False(topology.TryInteractionPort(DishStationFixture.Scrape, out _));
        Assert.False(topology.AllInteractionPortsConnected());
    }
}
