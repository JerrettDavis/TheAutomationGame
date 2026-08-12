using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class PatternCodexPresenterTests
{
    [Fact]
    public void RecognizedPreNamePageShowsOnlyThePlayersRestaurantEvidence()
    {
        var routing = CompletedRouting();
        var profile = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);

        var view = PatternCodexPresenter.Present(DishStationPatternContent.Strategy,
            profile.For(DishStationPatternContent.Strategy.PatternId));

        Assert.Equal("REUSABLE ROUTING CHOICE", view.Title);
        Assert.Equal("RECOGNIZED FROM YOUR WORK", view.Status);
        Assert.Equal("CONVENTIONAL NAME  NOT RECORDED", view.NameStatus);
        Assert.Equal(2, view.Evidence.Count);
        Assert.Contains(view.Evidence, evidence => evidence.Consequence.Contains("1 SHORTAGE", StringComparison.Ordinal));
        Assert.Contains(view.Evidence, evidence => evidence.Consequence.Contains("BOTH STATIONS SUPPLIED", StringComparison.Ordinal));
        Assert.DoesNotContain("strategy", AllText(view), StringComparison.OrdinalIgnoreCase);
    }

    private static TwoStationRoutingWorld CompletedRouting()
    {
        var routing = new TwoStationRoutingWorld(42, DishStationTwoStationsContent.Configuration);
        routing.ExecuteNow(new CopyRoutingStationPolicyCommand(routing.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
        routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
        routing.ExecuteNow(new SetRoutingStationPolicyCommand(routing.Tick,
            DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
        routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
        return routing;
    }

    private static string AllText(PatternCodexView view) => string.Join(' ', view.Title, view.Status,
        view.NameStatus, view.EvidenceSummary, string.Join(' ', view.Evidence.Select(evidence =>
            $"{evidence.Milestone} {evidence.Place} {evidence.Problem} {evidence.Solution} {evidence.Consequence}")));
}
