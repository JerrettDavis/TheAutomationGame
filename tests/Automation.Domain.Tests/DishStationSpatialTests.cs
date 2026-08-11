using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class DishStationSpatialTests
{
    [Fact]
    public void PresetsAreValidAndUCellHasShorterHandoffRoute()
    {
        Assert.True(DishStationPlacements.Linear.IsValid());
        Assert.True(DishStationPlacements.UShapedCell.IsValid());
        Assert.Equal(17, DishStationPlacements.Linear.EstimatedRouteSteps);
        Assert.Equal(15, DishStationPlacements.UShapedCell.EstimatedRouteSteps);
        Assert.True(DishStationPlacements.UShapedCell.EstimatedRouteSteps < DishStationPlacements.Linear.EstimatedRouteSteps);
    }

    [Fact]
    public void PlacementUpdatePreservesOtherFixtures()
    {
        var original = DishStationPlacements.Linear;
        var changed = original.With(DishStationFixture.Rack, new FloorCell(3, 3));

        Assert.Equal(new FloorCell(3, 3), changed.Rack);
        Assert.Equal(original.Washer, changed.Washer);
        Assert.True(changed.IsValid());
    }
}
