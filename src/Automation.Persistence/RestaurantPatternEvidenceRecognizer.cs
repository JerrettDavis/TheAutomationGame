using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Persistence;

public static class RestaurantPatternEvidenceRecognizer
{
    public static PatternKnowledgeProfile Recognize(
        PatternKnowledgeProfile profile,
        TwoStationRoutingSnapshot routing,
        PatternContentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(routing);
        ArgumentNullException.ThrowIfNull(definition);
        if (!definition.ProblemSignatures.Contains(PatternProblemSignature.InterchangeablePolicy)) return profile;

        var knowledge = profile.For(definition.PatternId);
        var copied = routing.Trials.FirstOrDefault(trial =>
            trial.TotalShortages > 0 && trial.Stations.Select(station => station.Policy).Distinct().Count() == 1);
        if (copied is not null)
            knowledge = knowledge.Record(CopiedEvidence(definition.PatternId, copied));

        var fitted = routing.Trials.LastOrDefault(trial =>
            trial.TotalShortages == 0 && trial.Stations.Select(station => station.Policy).Distinct().Count() > 1);
        if (fitted is not null)
            knowledge = knowledge.Record(FittedEvidence(definition.PatternId, fitted));

        var qualifying = knowledge.Evidence.Count(evidence =>
            definition.ProblemSignatures.Contains(evidence.ProblemSignature));
        var applicationSatisfied = !definition.RequiresApplication || knowledge.Has(PatternKnowledgeMilestone.Applied);
        if (qualifying >= definition.MinimumEvidence && applicationSatisfied && fitted is not null)
            knowledge = knowledge.Conclude(PatternKnowledgeMilestone.Recognized,
                new PatternEvidenceId("restaurant.two-stations.fitted"));
        return profile.Put(knowledge);
    }

    private static PatternEvidence CopiedEvidence(PatternId pattern, TwoStationRoutingTrialResult trial) => new(
        new("restaurant.two-stations.copied"), pattern, PatternKnowledgeMilestone.Encountered,
        "industry.restaurant", DishStationTwoStationsContent.ScenarioId, DishStationTwoStationsContent.QuestId,
        "Rossi's Restaurant / Main Dish Room + Patio Service Station",
        PatternProblemSignature.InterchangeablePolicy,
        "The copied glass-first routing fit the main dish room but left patio plate demand short.",
        "One routing choice was reused in the same decision slot at both stations.",
        $"Trial {trial.Sequence}: {trial.TotalCompleted} completed, {trial.TotalShortages} shortage, net {trial.TotalNetValue}.",
        trial.Sequence, ReplayReference(trial));

    private static PatternEvidence FittedEvidence(PatternId pattern, TwoStationRoutingTrialResult trial) => new(
        new("restaurant.two-stations.fitted"), pattern, PatternKnowledgeMilestone.Applied,
        "industry.restaurant", DishStationTwoStationsContent.ScenarioId, DishStationTwoStationsContent.QuestId,
        "Rossi's Restaurant / Main Dish Room + Patio Service Station",
        PatternProblemSignature.InterchangeablePolicy,
        "Glass-heavy main service and plate-heavy patio service needed different routing priorities.",
        "Different policies occupied the same routing decision slot without changing the station workflow.",
        $"Trial {trial.Sequence}: both stations supplied, {trial.TotalShortages} shortages, net {trial.TotalNetValue}.",
        trial.Sequence, ReplayReference(trial));

    private static string ReplayReference(TwoStationRoutingTrialResult trial) =>
        $"two-station-trial:{trial.Sequence}:seed:{trial.Seed}:horizon:{trial.HorizonTicks}";
}
