using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class DishStationEconomyConfigurationTests
{
    [Fact]
    public void FirstShiftRatesRequirePositiveValueAndLaborTimeWithNonnegativeCosts()
    {
        Assert.Same(DishStationEconomyConfiguration.Default, DishStationEconomyConfiguration.Default.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (DishStationEconomyConfiguration.Default with { CompletedDishValue = 0 }).Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => (DishStationEconomyConfiguration.Default with { LaborTicksPerWorkAction = 0 }).Validate());

        foreach (var invalid in new[]
                 {
                     DishStationEconomyConfiguration.Default with { LaborCostPerTick = -1 },
                     DishStationEconomyConfiguration.Default with { StaffingCostPerEnabledTick = -1 },
                     DishStationEconomyConfiguration.Default with { TrayReworkCost = -1 },
                     DishStationEconomyConfiguration.Default with { ServiceShortageDowntimeCost = -1 },
                     DishStationEconomyConfiguration.Default with { AutomationIncidentDowntimeCost = -1 },
                     DishStationEconomyConfiguration.Default with { FlowCellInvestmentCost = -1 },
                 })
            Assert.Throws<ArgumentOutOfRangeException>(() => invalid.Validate());
    }
}
