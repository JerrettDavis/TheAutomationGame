using System.Collections.Immutable;
using Automation.Domain;

namespace Automation.Domain.Tests;

public sealed class PatternKnowledgeTests
{
    private static readonly PatternId Strategy = new("pattern.strategy");

    [Fact]
    public void EvidenceRecordsMilestonesOnceAndConclusionsRequireABasis()
    {
        var encountered = Evidence("restaurant.two-stations.copied", PatternKnowledgeMilestone.Encountered, 1);
        var applied = Evidence("restaurant.two-stations.fitted", PatternKnowledgeMilestone.Applied, 2);
        var knowledge = PatternKnowledge.Empty(Strategy).Record(encountered).Record(encountered).Record(applied);

        Assert.Equal(2, knowledge.Evidence.Length);
        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Encountered));
        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Applied));
        Assert.False(knowledge.Has(PatternKnowledgeMilestone.Recognized));
        Assert.Throws<ArgumentException>(() => knowledge.Conclude(PatternKnowledgeMilestone.Recognized,
            new("missing")));

        var recognized = knowledge.Conclude(PatternKnowledgeMilestone.Recognized, applied.Id);
        Assert.True(recognized.Has(PatternKnowledgeMilestone.Recognized));
        Assert.Equal(2, recognized.Evidence.Length);
    }

    [Fact]
    public void ProfileKeepsIndependentPatternJournalsInStableOrder()
    {
        var state = new PatternId("pattern.state");
        var profile = PatternKnowledgeProfile.Empty
            .Put(PatternKnowledge.Empty(Strategy).Record(Evidence("strategy", PatternKnowledgeMilestone.Applied, 2)))
            .Put(PatternKnowledge.Empty(state).Record(Evidence("state", PatternKnowledgeMilestone.Encountered, 1, state)));

        Assert.Equal([state, Strategy], profile.Patterns.Select(item => item.Pattern));
        Assert.Single(profile.For(Strategy).Evidence);
        Assert.Empty(profile.For(new("pattern.observer")).Evidence);
    }

    [Fact]
    public void InvalidIdsAndUnsupportedDirectConclusionsFailFast()
    {
        Assert.Throws<ArgumentException>(() => new PatternId("Strategy"));
        Assert.Throws<ArgumentException>(() => new PatternEvidenceId("has space"));
        var invalid = Evidence("bad", PatternKnowledgeMilestone.Named, 1);
        Assert.Throws<ArgumentException>(() => PatternKnowledge.Empty(Strategy).Record(invalid));
    }

    private static PatternEvidence Evidence(string id, PatternKnowledgeMilestone milestone, int sequence,
        PatternId? pattern = null) => new(
        new(id), pattern ?? Strategy, milestone, "industry.restaurant", "scenario.restaurant.two-stations",
        "quest.restaurant.two-stations.one-problem", "Rossi's Restaurant / Dish Station",
        PatternProblemSignature.InterchangeablePolicy, "One copied choice missed patio demand.",
        "Different policies occupied the same routing slot.", "Both stations supplied service.", sequence,
        $"two-station-trial:{sequence}");
}
