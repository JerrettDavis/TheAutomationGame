using System.Text.Json;
using System.Text.Json.Nodes;
using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class AutomationCareerSaveStoreTests
{
    [Fact]
    public void CareerEnvelopeRestoresFirstShiftRoutingAndRecognizedEvidence()
    {
        var firstShift = new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration);
        firstShift.ExecuteNow(new CompleteIntroCommand(firstShift.Tick, GuidanceMode.Guided));
        var routing = CompletedRouting();
        var profile = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);

        var restored = AutomationCareerSaveStore.Deserialize(
            AutomationCareerSaveStore.Serialize(new(firstShift, routing, profile)),
            42, DishStationTwoStationsContent.Configuration);

        Assert.Equal(Json(firstShift.Snapshot()), Json(restored.FirstShift.Snapshot()));
        Assert.Equal(Json(routing.Snapshot()), Json(restored.TwoStationRouting.Snapshot()));
        Assert.Equal(Json(profile), Json(restored.PatternKnowledge));
        Assert.True(restored.PatternKnowledge.For(DishStationPatternContent.Strategy.PatternId)
            .Has(PatternKnowledgeMilestone.Recognized));
        Assert.Single(restored.PatternKnowledge.For(DishStationPatternContent.Strategy.PatternId).Conclusions);
    }

    [Fact]
    public void SchemaOneCareerMigratesRecognitionToACitedConclusion()
    {
        var routing = CompletedRouting();
        var profile = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);
        var root = JsonNode.Parse(AutomationCareerSaveStore.Serialize(new(
            new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration), routing, profile)))!.AsObject();
        root["schemaVersion"] = 1;
        foreach (var pattern in root["patternKnowledge"]!["patterns"]!.AsArray())
            pattern!.AsObject().Remove("conclusions");

        var restored = AutomationCareerSaveStore.Deserialize(root.ToJsonString(), 42,
            DishStationTwoStationsContent.Configuration);
        var knowledge = restored.PatternKnowledge.For(DishStationPatternContent.Strategy.PatternId);

        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Recognized));
        var conclusion = Assert.Single(knowledge.Conclusions);
        Assert.Equal(PatternKnowledgeMilestone.Recognized, conclusion.Milestone);
        Assert.Contains(knowledge.Evidence, evidence => evidence.Id == conclusion.Basis);
        Assert.Contains("\"schemaVersion\": 2", AutomationCareerSaveStore.Serialize(restored), StringComparison.Ordinal);
    }

    [Fact]
    public void NamedPatternAndItsEvidenceBasisSurviveCareerResume()
    {
        var routing = CompletedRouting();
        var recognized = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);
        var named = PatternNamingService.RecordReflection(recognized, DishStationPatternContent.Strategy);

        var restored = AutomationCareerSaveStore.Deserialize(AutomationCareerSaveStore.Serialize(new(
            new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration), routing, named)),
            42, DishStationTwoStationsContent.Configuration);
        var knowledge = restored.PatternKnowledge.For(DishStationPatternContent.Strategy.PatternId);

        Assert.True(knowledge.Has(PatternKnowledgeMilestone.Named));
        Assert.Equal(2, knowledge.Conclusions.Length);
        Assert.All(knowledge.Conclusions, conclusion =>
            Assert.Contains(knowledge.Evidence, evidence => evidence.Id == conclusion.Basis));
    }

    [Fact]
    public void CareerEnvelopeRejectsAConclusionWithMissingEvidence()
    {
        var routing = CompletedRouting();
        var recognized = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);
        var named = PatternNamingService.RecordReflection(recognized, DishStationPatternContent.Strategy);
        var json = AutomationCareerSaveStore.Serialize(new(
            new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration), routing, named));
        json = json.Replace("\"basis\": \"restaurant.two-stations.fitted\"",
            "\"basis\": \"missing-evidence\"", StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => AutomationCareerSaveStore.Deserialize(json, 42,
            DishStationTwoStationsContent.Configuration));
    }

    [Fact]
    public void LegacyRawFirstShiftReplayLoadsWithEmptyPostShiftHistory()
    {
        var firstShift = new DishStationWorld(73, DishStationFirstHoursContent.ScenarioConfiguration);
        firstShift.ExecuteNow(new CompleteIntroCommand(firstShift.Tick, GuidanceMode.Minimal));

        var restored = AutomationCareerSaveStore.Deserialize(DishStationSaveStore.Serialize(firstShift),
            42, DishStationTwoStationsContent.Configuration);

        Assert.Equal(73, restored.FirstShift.Seed);
        Assert.Empty(restored.TwoStationRouting.Snapshot().Trials);
        Assert.Empty(restored.PatternKnowledge.Patterns);
    }

    [Fact]
    public void AtomicCareerEnvelopeLeavesNoTemporaryFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"automation-profile-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "career.json");
        try
        {
            var routing = CompletedRouting();
            var profile = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
                routing.Snapshot(), DishStationPatternContent.Strategy);
            AutomationCareerSaveStore.SaveFileAtomic(path,
                new(new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration), routing, profile));

            var restored = AutomationCareerSaveStore.LoadFile(path, 42, DishStationTwoStationsContent.Configuration);
            Assert.Equal(2, restored.TwoStationRouting.Snapshot().Trials.Count);
            Assert.False(File.Exists(path + ".tmp"));
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }

    [Fact]
    public void CareerEnvelopeRejectsMalformedPatternIds()
    {
        var routing = CompletedRouting();
        var profile = RestaurantPatternEvidenceRecognizer.Recognize(PatternKnowledgeProfile.Empty,
            routing.Snapshot(), DishStationPatternContent.Strategy);
        var json = AutomationCareerSaveStore.Serialize(new(
            new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration), routing, profile));
        json = json.Replace("\"pattern.strategy\"", "\"Strategy\"", StringComparison.Ordinal);

        Assert.Throws<ArgumentException>(() => AutomationCareerSaveStore.Deserialize(json, 42,
            DishStationTwoStationsContent.Configuration));
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

    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
}
