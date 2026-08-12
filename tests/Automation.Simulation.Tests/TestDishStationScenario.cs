using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

internal static class TestDishStationScenario
{
    public static DishStationScenarioConfiguration Reference { get; } = new()
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

    public static DishStationWorld World(int seed = 42) => new(seed, Reference);

    public static DishStationWorld World(int seed, DishStationScenarioConfiguration configuration) => new(seed, configuration);
}
