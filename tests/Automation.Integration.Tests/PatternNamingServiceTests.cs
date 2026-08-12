using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class PatternNamingServiceTests
{
    [Fact]
    public void ReflectionNamesOnlyRecognizedEvidenceAndIsIdempotent()
    {
        var definition = DishStationPatternContent.Strategy;
        Assert.Throws<InvalidOperationException>(() =>
            PatternNamingService.RecordReflection(PatternKnowledgeProfile.Empty, definition));

        var routing = CompletedRouting();
        var recognized = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), definition);
        var named = PatternNamingService.RecordReflection(recognized, definition);
        var duplicate = PatternNamingService.RecordReflection(named, definition);
        var knowledge = named.For(definition.PatternId);

        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Named));
        Assert.Equal(named, duplicate);
        var conclusion = Assert.Single(knowledge.Conclusions,
            item => item.Milestone == PatternKnowledgeMilestone.Named);
        Assert.Contains(knowledge.Evidence, evidence => evidence.Id == conclusion.Basis);
        Assert.Equal(PatternKnowledgeMilestone.Applied,
            knowledge.Evidence.Single(evidence => evidence.Id == conclusion.Basis).Milestone);
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
}
