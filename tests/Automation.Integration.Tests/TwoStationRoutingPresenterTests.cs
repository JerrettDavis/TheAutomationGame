using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class TwoStationRoutingPresenterTests
{
    [Fact]
    public void CopiedChoiceExposesPatioShortageInTheSameDecisionSlot()
    {
        var world = NewWorld();
        Assert.True(world.ExecuteNow(new CopyRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation)).Success);
        Assert.True(world.ExecuteNow(new RunTwoStationRoutingTrialCommand(world.Tick)).Success);

        var view = Present(world, 1);

        Assert.Equal("TWO STATIONS, ONE PROBLEM", view.Title);
        Assert.Equal(1, view.CopyCount);
        Assert.Equal(1, view.TotalShortages);
        Assert.False(view.OutcomeMet);
        Assert.All(view.Stations, station => Assert.Equal("GLASSES FIRST", station.Policy));
        Assert.Equal(1, view.Stations.Single(station => station.Id == DishRoutingStationId.PatioServiceStation).Shortages);
        Assert.DoesNotContain("strategy", AllText(view), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FittedPatioChoiceRevealsTheAuthoredDiscovery()
    {
        var world = NewWorld();
        world.ExecuteNow(new CopyRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
        world.ExecuteNow(new RunTwoStationRoutingTrialCommand(world.Tick));
        world.ExecuteNow(new SetRoutingStationPolicyCommand(world.Tick,
            DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
        world.ExecuteNow(new RunTwoStationRoutingTrialCommand(world.Tick));

        var view = Present(world, 1);

        Assert.True(view.OutcomeMet);
        Assert.Equal(0, view.TotalShortages);
        Assert.Equal(2, view.TrialCount);
        Assert.Equal(DishStationTwoStationsContent.Quest.Narrative!.Discovery, view.Discovery);
        Assert.Equal("PLATES FIRST", view.Stations.Single(station => station.Selected).Policy);
    }

    private static TwoStationRoutingWorld NewWorld() => new(42, DishStationTwoStationsContent.Configuration);

    private static TwoStationRoutingView Present(TwoStationRoutingWorld world, int selected) =>
        TwoStationRoutingPresenter.Present(DishStationTwoStationsContent.Configuration, world.Snapshot(),
            DishStationTwoStationsContent.Quest, selected);

    private static string AllText(TwoStationRoutingView view) => string.Join(' ',
        view.Title, view.Situation, view.Evidence, view.Discovery,
        string.Join(' ', view.Stations.Select(station => $"{station.Name} {station.Demand} {station.Policy}")));
}
