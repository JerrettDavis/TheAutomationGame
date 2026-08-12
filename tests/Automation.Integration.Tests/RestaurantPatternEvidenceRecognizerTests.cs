using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class RestaurantPatternEvidenceRecognizerTests
{
    [Fact]
    public void RestaurantTrialsBecomePlayerEvidenceOnlyAfterTheirConsequencesExist()
    {
        var routing = new TwoStationRoutingWorld(42, DishStationTwoStationsContent.Configuration);
        var empty = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);
        Assert.Empty(empty.For(DishStationPatternContent.Strategy.PatternId).Evidence);

        routing.ExecuteNow(new CopyRoutingStationPolicyCommand(routing.Tick,
            DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
        routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
        var encountered = RestaurantPatternEvidenceRecognizer.Recognize(empty, routing.Snapshot(), DishStationPatternContent.Strategy);
        var firstKnowledge = encountered.For(DishStationPatternContent.Strategy.PatternId);
        Assert.Single(firstKnowledge.Evidence);
        Assert.True(firstKnowledge.Has(PatternKnowledgeMilestone.Encountered));
        Assert.False(firstKnowledge.Has(PatternKnowledgeMilestone.Recognized));

        routing.ExecuteNow(new SetRoutingStationPolicyCommand(routing.Tick,
            DishRoutingStationId.PatioServiceStation, ProcessRoutingPolicy.PlatesFirst));
        routing.ExecuteNow(new RunTwoStationRoutingTrialCommand(routing.Tick));
        var recognized = RestaurantPatternEvidenceRecognizer.Recognize(encountered, routing.Snapshot(), DishStationPatternContent.Strategy);
        var knowledge = recognized.For(DishStationPatternContent.Strategy.PatternId);
        Assert.Equal(2, knowledge.Evidence.Length);
        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Applied));
        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Recognized));
        Assert.False(knowledge.Has(PatternKnowledgeMilestone.Named));
        Assert.Contains(knowledge.Evidence, evidence => evidence.Consequence.Contains("1 shortage", StringComparison.Ordinal));
        Assert.Contains(knowledge.Evidence, evidence => evidence.Consequence.Contains("both stations supplied", StringComparison.OrdinalIgnoreCase));

        var duplicate = RestaurantPatternEvidenceRecognizer.Recognize(recognized, routing.Snapshot(), DishStationPatternContent.Strategy);
        Assert.Equal(knowledge, duplicate.For(DishStationPatternContent.Strategy.PatternId));
    }
}
