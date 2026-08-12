using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class TwoStationRoutingConfigurationTests
{
    [Fact]
    public void ConfigurationRequiresTheTwoConcreteRestaurantStations()
    {
        var invalid = new TwoStationRoutingConfiguration(
            Scenario(),
            ImmutableArray.Create(new DishRoutingStationProfile(
                DishRoutingStationId.MainDishRoom, "Main dish room", new(1, 1, 0),
                DishKind.Glass, ProcessRoutingPolicy.GlassesFirst)),
            5);

        var error = Assert.Throws<ArgumentException>(invalid.Validate);

        Assert.Contains("exactly two", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConfigurationRejectsUnsupportedHiddenVariants()
    {
        var invalid = Valid() with
        {
            Stations = ImmutableArray.Create(
                new DishRoutingStationProfile(DishRoutingStationId.MainDishRoom, "Main", new(1, 1, 0), DishKind.Glass, ProcessRoutingPolicy.GlassesFirst),
                new DishRoutingStationProfile(DishRoutingStationId.PatioServiceStation, "Patio", new(1, 1, 0), DishKind.Tray, ProcessRoutingPolicy.PlatesFirst)),
        };

        Assert.Throws<ArgumentOutOfRangeException>(invalid.Validate);
    }

    private static TwoStationRoutingConfiguration Valid() => new(
        Scenario(),
        ImmutableArray.Create(
            new DishRoutingStationProfile(DishRoutingStationId.MainDishRoom, "Main", new(1, 1, 0), DishKind.Glass, ProcessRoutingPolicy.GlassesFirst),
            new DishRoutingStationProfile(DishRoutingStationId.PatioServiceStation, "Patio", new(1, 1, 0), DishKind.Plate, ProcessRoutingPolicy.GlassesFirst)),
        5);

    private static DishStationScenarioConfiguration Scenario() => new()
    {
        InitialDirty = new(1, 1, 0),
        InitialAvailable = new(0, 0, 0),
        ArrivalIntervalTicks = 1000,
        GlassEveryArrivals = 2,
        RackCapacity = 4,
        WasherCycleTicks = 1,
        WorkerActionIntervalTicks = 1,
        FlowCellWorkerActionIntervalTicks = 1,
        StickyReadyFaultAfterAutomatedStarts = 0,
        StickyReadyFaultPermillePerStart = 0,
        DemandKind = DishKind.Glass,
        DemandIntervalTicks = 5,
        InitialRushEnabled = false,
        InitialNewHireEnabled = false,
        InitialNewHireSpecification = default,
        InitialAutomationPolicy = WasherAutomationPolicy.Off,
        InitialLayout = DishStationLayout.Linear,
    };
}
