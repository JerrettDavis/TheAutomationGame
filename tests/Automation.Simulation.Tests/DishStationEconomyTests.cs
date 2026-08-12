using Automation.Domain;
using Automation.Simulation;

namespace Automation.Simulation.Tests;

public sealed class DishStationEconomyTests
{
    [Fact]
    public void AcceptedWorkTicksAndConsequencesProduceExplainableEconomy()
    {
        var rates = new DishStationEconomyConfiguration(100, 2, 5, 3, 40, 70, 90, 30);
        var world = TestDishStationScenario.World(42, TestDishStationScenario.Reference with
        {
            InitialDirty = new(2, 0, 1),
            InitialAvailable = new(0, 0, 0),
            ArrivalIntervalTicks = 1000,
            WasherCycleTicks = 5,
            DemandKind = DishKind.Glass,
            DemandIntervalTicks = 1,
            InitialRushEnabled = true,
            InitialAutomationPolicy = WasherAutomationPolicy.ReportedReadyOnly,
            StickyReadyFaultAfterAutomatedStarts = 1,
            Economy = rates,
        });

        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        world.Advance();
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Scrape, DishKind.Plate)).Success);
        Assert.True(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Rack, DishKind.Plate)).Success);
        world.Advance();
        Assert.True(world.ExecuteNow(new ConfigureDishStationLayoutCommand(world.Tick, DishStationLayout.UShapedCell)).Success);

        var economy = world.Snapshot().Economy;

        Assert.Equal(4, economy.PlayerWorkActions);
        Assert.Equal(8, economy.LaborTicks);
        Assert.Equal(40, economy.LaborCost);
        Assert.Equal(2, economy.ServiceShortages);
        Assert.Equal(140, economy.ShortageDowntimeCost);
        Assert.Equal(1, economy.AutomationIncidents);
        Assert.Equal(90, economy.IncidentDowntimeCost);
        Assert.Equal(230, economy.DowntimeCost);
        Assert.True(economy.FlowCellInvested);
        Assert.Equal(30, economy.InvestmentCost);
        Assert.Equal(300, economy.TotalCost);
        Assert.Equal(-300, economy.NetValue);
    }

    [Fact]
    public void StaffingIsChargedOnlyFromAuthoritativeTicksAndRejectedActionsDoNotMutateEconomy()
    {
        var world = TestDishStationScenario.World(42, TestDishStationScenario.Reference with
        {
            InitialDirty = new(1, 0, 0),
            ArrivalIntervalTicks = 1000,
            WorkerActionIntervalTicks = 1,
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.HappyPath,
        });

        world.Advance();
        world.Advance();
        var beforeRejectedAction = world.Snapshot().Economy;
        Assert.False(world.ExecuteNow(new PerformDishActionCommand(world.Tick, DishAction.Unload, DishKind.Tray)).Success);
        var economy = world.Snapshot().Economy;

        Assert.Equal(2, economy.StaffedTicks);
        Assert.Equal(2, economy.WorkerActions);
        Assert.Equal(0, economy.ReworkIncidents);
        Assert.Equal(0, economy.WasteCost);
        Assert.Equal(beforeRejectedAction, economy);
    }

    [Fact]
    public void SameSeedLinearAndFlowCellChoicesAreViableDeterministicAndEconomicallyDistinct()
    {
        var first = DishStationEconomyComparison.Run(42, TestDishStationScenario.Reference);
        var second = DishStationEconomyComparison.Run(42, TestDishStationScenario.Reference);

        Assert.Equal(first, second);
        Assert.True(first.SameSeed);
        Assert.True(first.LinearStation.Viable);
        Assert.True(first.FlowCell.Viable);
        Assert.True(first.DifferentProfile);
        Assert.True(first.FlowCell.CompletedDishes > first.LinearStation.CompletedDishes);
        Assert.True(first.FlowCell.WorkerTravelSteps < first.LinearStation.WorkerTravelSteps);
        Assert.True(first.FlowCell.Economy.InvestmentCost > first.LinearStation.Economy.InvestmentCost);
        Assert.NotEqual(first.LinearStation.Economy.NetValue, first.FlowCell.Economy.NetValue);
        Assert.Equal((3, 109, 360, 72, 120, 0, 192, 168),
            (first.LinearStation.CompletedDishes, first.LinearStation.WorkerTravelSteps,
                first.LinearStation.Economy.ThroughputValue, first.LinearStation.Economy.LaborCost,
                first.LinearStation.Economy.StaffingCost, first.LinearStation.Economy.InvestmentCost,
                first.LinearStation.Economy.TotalCost, first.LinearStation.Economy.NetValue));
        Assert.Equal((4, 65, 480, 90, 120, 180, 390, 90),
            (first.FlowCell.CompletedDishes, first.FlowCell.WorkerTravelSteps,
                first.FlowCell.Economy.ThroughputValue, first.FlowCell.Economy.LaborCost,
                first.FlowCell.Economy.StaffingCost, first.FlowCell.Economy.InvestmentCost,
                first.FlowCell.Economy.TotalCost, first.FlowCell.Economy.NetValue));
    }

    [Fact]
    public void ReplaySaveRestoresLiveEconomy()
    {
        var world = TestDishStationScenario.World();
        world.ExecuteNow(new ConfigureDishStationLayoutCommand(world.Tick, DishStationLayout.UShapedCell));
        world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true));
        world.ExecuteNow(new TrainNewHireCommand(world.Tick, DishProcessSpecification.FullyDocumented));
        for (var tick = 0; tick < 140; tick++) world.Advance();

        var restored = DishStationWorld.Restore(world.CreateReplaySave());

        Assert.Equal(world.Snapshot().Economy, restored.Snapshot().Economy);
        Assert.Equal(world.Snapshot().ShiftReport, restored.Snapshot().ShiftReport);
    }
}
