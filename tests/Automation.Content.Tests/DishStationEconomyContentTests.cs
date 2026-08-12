using Automation.Content;
using Automation.Domain;

namespace Automation.Content.Tests;

public sealed class DishStationEconomyContentTests
{
    [Fact]
    public void FirstShiftCompilesAuthoredEngineNeutralEconomyRates()
    {
        var scenario = ContentCompilerV1.CompileFile(ContentTestPaths.FirstShift).Scenarios.Single(candidate =>
            candidate.Id.Value == DishStationFirstHoursContent.ScenarioId);

        Assert.Equal(new DishStationEconomyConfiguration(120, 1, 3, 1, 35, 80, 120, 180), scenario.DishStation!.Economy);
    }

    [Fact]
    public void EconomyBlockIsAllOrNothingAndRejectsInvalidRatesAtSemanticPaths()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift).ReplaceLineEndings("\n");
        var missing = yaml.Replace("        staffing_cost_per_enabled_tick: 1\n", "", StringComparison.Ordinal);
        var missingFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(missing, "missing-economy.yaml"));
        Assert.Contains(missingFailure.Diagnostics, diagnostic =>
            diagnostic.Path == "scenarios[0].dish_station.economy.staffing_cost_per_enabled_tick");

        var invalid = yaml.Replace("        completed_dish_value: 120", "        completed_dish_value: 0", StringComparison.Ordinal);
        var invalidFailure = Assert.Throws<ContentCompilationException>(() => ContentCompilerV1.Compile(invalid, "invalid-economy.yaml"));
        Assert.Contains(invalidFailure.Diagnostics, diagnostic =>
            diagnostic.Path == "scenarios[0].dish_station.economy.completed_dish_value");
    }

    [Fact]
    public void ScenarioWithoutEconomyBlockRemainsCompatibleWithExplicitDefaults()
    {
        var yaml = File.ReadAllText(ContentTestPaths.FirstShift).ReplaceLineEndings("\n");
        var economy = "      economy:\n" +
            "        completed_dish_value: 120\n" +
            "        labor_ticks_per_work_action: 1\n" +
            "        labor_cost_per_tick: 3\n" +
            "        staffing_cost_per_enabled_tick: 1\n" +
            "        tray_rework_cost: 35\n" +
            "        service_shortage_downtime_cost: 80\n" +
            "        automation_incident_downtime_cost: 120\n" +
            "        flow_cell_investment_cost: 180\n";
        var scenario = ContentCompilerV1.Compile(
            yaml.Replace(economy, "", StringComparison.Ordinal), "compatible.yaml").Scenarios.Single(candidate =>
            candidate.Id.Value == DishStationFirstHoursContent.ScenarioId);

        Assert.Equal(DishStationEconomyConfiguration.Default, scenario.DishStation!.Economy);
    }
}
