using Automation.Domain;

namespace Automation.Simulation;

public sealed record DishStationEconomyChoiceResult(
    string Choice,
    int Seed,
    int HorizonTicks,
    DishStationLayout Layout,
    int CompletedDishes,
    int ServiceShortages,
    int WorkerTravelSteps,
    DishStationEconomySnapshot Economy)
{
    public bool Viable => CompletedDishes > 0 && ServiceShortages == 0;
}

public sealed record DishStationEconomyComparisonResult(
    DishStationScenarioConfiguration Scenario,
    DishStationEconomyChoiceResult LinearStation,
    DishStationEconomyChoiceResult FlowCell)
{
    public bool SameSeed => LinearStation.Seed == FlowCell.Seed;
    public bool DifferentProfile =>
        LinearStation.Economy.TotalCost != FlowCell.Economy.TotalCost &&
        (LinearStation.CompletedDishes != FlowCell.CompletedDishes ||
         LinearStation.WorkerTravelSteps != FlowCell.WorkerTravelSteps);
}

public static class DishStationEconomyComparison
{
    public const int DefaultHorizonTicks = 120;

    public static DishStationEconomyComparisonResult Run(
        int seed,
        DishStationScenarioConfiguration scenario,
        int horizonTicks = DefaultHorizonTicks)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        if (horizonTicks <= 0) throw new ArgumentOutOfRangeException(nameof(horizonTicks));
        scenario.Validate();

        return new(
            scenario,
            RunChoice("staffed-linear", seed, scenario, horizonTicks, DishStationLayout.Linear),
            RunChoice("staffed-flow-cell", seed, scenario, horizonTicks, DishStationLayout.UShapedCell));
    }

    private static DishStationEconomyChoiceResult RunChoice(
        string choice,
        int seed,
        DishStationScenarioConfiguration scenario,
        int horizonTicks,
        DishStationLayout layout)
    {
        var world = new DishStationWorld(seed, scenario);
        RequireAccepted(world.ExecuteNow(new ConfigureDishStationLayoutCommand(world.Tick, layout)));
        RequireAccepted(world.ExecuteNow(new SetNewHireEnabledCommand(world.Tick, true)));
        RequireAccepted(world.ExecuteNow(new TrainNewHireCommand(world.Tick, DishProcessSpecification.FullyDocumented)));
        for (var tick = 0; tick < horizonTicks; tick++) world.Advance();

        var snapshot = world.Snapshot();
        return new(
            choice,
            seed,
            horizonTicks,
            snapshot.Layout.Layout,
            snapshot.Completed,
            snapshot.ServiceShortages,
            snapshot.Layout.NewHireTravelSteps,
            snapshot.Economy);
    }

    private static void RequireAccepted(CommandResult result)
    {
        if (!result.Success) throw new InvalidOperationException($"Economy comparison setup was rejected: {result.Message}");
    }
}
