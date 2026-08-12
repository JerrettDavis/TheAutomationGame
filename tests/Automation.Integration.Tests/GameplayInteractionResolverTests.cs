using Automation.Client.Stride;
using Automation.Domain;

namespace Automation.Integration.Tests;

public sealed class GameplayInteractionResolverTests
{
    [Theory]
    [InlineData(GameplayInteractionActions.Interact, GameplayInteractionIntent.Interact)]
    [InlineData(GameplayInteractionActions.Inspect, GameplayInteractionIntent.Inspect)]
    [InlineData(GameplayInteractionActions.Interact | GameplayInteractionActions.Inspect, GameplayInteractionIntent.Interact | GameplayInteractionIntent.Inspect)]
    public void PhysicalInteractionKeysMapToSemanticIntent(
        GameplayInteractionActions actions,
        GameplayInteractionIntent expected)
    {
        Assert.Equal(expected, GameplayInteractionInput.Resolve(actions));
    }

    [Fact]
    public void SelectedInRangeFixtureWins()
    {
        var placements = DishStationPlacements.Linear;
        var topology = new DishStationTopology(placements);

        var target = GameplayInteractionResolver.Resolve(
            topology.InteractionPort(DishStationFixture.Washer),
            placements,
            DishStationFixture.Washer);

        Assert.Equal(DishStationFixture.Washer, target);
    }

    [Fact]
    public void OutOfRangeSelectionFallsBackToNearestFixture()
    {
        var placements = DishStationPlacements.Linear;
        var topology = new DishStationTopology(placements);

        var target = GameplayInteractionResolver.Resolve(
            topology.InteractionPort(DishStationFixture.Rack),
            placements,
            DishStationFixture.Service);

        Assert.Equal(DishStationFixture.Rack, target);
    }

    [Fact]
    public void EqualDistanceTargetsUseStableFixtureOrder()
    {
        var placements = new DishStationPlacements(
            new FloorCell(2, 1),
            new FloorCell(4, 1),
            new FloorCell(7, 1),
            new FloorCell(10, 1),
            new FloorCell(12, 3),
            new FloorCell(11, 6));

        var topology = new DishStationTopology(placements);
        var scrapePort = topology.InteractionPort(DishStationFixture.Scrape);
        var rackPort = topology.InteractionPort(DishStationFixture.Rack);
        var midpoint = new FloorCell((scrapePort.X + rackPort.X) / 2, scrapePort.Y);

        var target = GameplayInteractionResolver.Resolve(
            midpoint,
            placements,
            null);

        Assert.Equal(DishStationFixture.Scrape, target);
    }
}
