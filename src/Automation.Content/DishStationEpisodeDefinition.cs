using System.Collections.Immutable;
using System.Text;
using Automation.Domain;

namespace Automation.Content;

public sealed record ScenarioOutcomeCondition(
    string Id,
    string PlayerFacingOutcome);

public sealed record ScenarioDiscoveryCondition(
    string Id,
    string PlayerFacingClue,
    string CausalEvidence);

public sealed record DishStationEpisodeDefinition(
    string Id,
    string DisplayName,
    string StartingSituation,
    IReadOnlyList<ScenarioOutcomeCondition> Outcomes,
    IReadOnlyList<ScenarioDiscoveryCondition> Discoveries)
{
    public static DishStationEpisodeDefinition FirstPlayable { get; } = new(
        "dish-station-first-playable",
        "THE AUTOMATION GAME / DISH STATION",
        "A dinner rush is approaching. Dirty dishes, a constrained washer, and incomplete operating knowledge share one small station.",
        [
            new("service-restored", "Service receives a clean dish produced through the observed process."),
            new("route-improved", "The same dish outcome requires less handling travel after the layout change."),
            new("knowledge-transferred", "Delegated work handles rush priority and the rare tray without repeating discovered rework."),
            new("incident-explained", "The first divergence between reported and physical readiness is causally explained."),
            new("regression-proved", "The corrected policy rejects the exact inputs that caused the unsafe request."),
        ],
        [
            new("glass-shortage", "Service waits even while total dish work continues.", "Queue age and pressure reveal where glasses are waiting."),
            new("priority-gap", "The new hire chooses valid work that does not relieve the urgent shortage.", "The transferred process contains flow but no rush priority."),
            new("rare-tray-gap", "An uncommon tray returns to dirty work after ordinary handling.", "Its orientation fact is absent from the explicit process."),
            new("sticky-ready", "Ready remains visible while a clean rack still occupies the washer.", "The runtime trace preserves reported and physical readiness at the unsafe request."),
        ]);
}

public sealed record DishStationQuestDefinition(
    ContentId ContentId,
    DishStationQuestId Id,
    ImmutableArray<ContentId> Participants,
    int Sequence,
    string Title,
    string Situation,
    string ObservableOutcome,
    string Discovery,
    string UnlockRationale,
    int ExperienceReward,
    CareerCapability CapabilityReward,
    ImmutableArray<DishStationQuestStepDefinition> Steps);

public sealed record DishStationQuestStepDefinition(
    string Id,
    string Text,
    string? InputAction);

public sealed record DishStationFirstShiftNarrative(
    ScenarioNarrativeContentDefinition Chapter,
    ImmutableArray<DishStationQuestDefinition> Quests)
{
    public DishStationQuestDefinition Quest(DishStationQuestId id) =>
        Quests.Single(quest => quest.Id == id);

    public int IndexOf(DishStationQuestId id) =>
        Quests.IndexOf(Quest(id));

    public DishStationQuestStepDefinition Step(string id) =>
        Quests.SelectMany(quest => quest.Steps).Single(step => string.Equals(step.Id, id, StringComparison.Ordinal));
}

public static class DishStationFirstHoursContent
{
    public const string ScenarioId = "scenario.restaurant.first-shift";
    private const string ResourceName = "Automation.Content.first-shift.yaml";

    public static CompiledContentCatalogV1 Catalog { get; } = LoadDefaultCatalog();
    public static DishStationFirstShiftNarrative Narrative { get; } = FromCatalog(Catalog, ResourceName);
    public static ScenarioContentDefinition Scenario { get; } = Catalog.Scenarios.Single(scenario => scenario.Id.Value == ScenarioId);
    public static DishStationScenarioConfiguration ScenarioConfiguration { get; } = Scenario.DishStation
        ?? throw new InvalidOperationException($"First-shift scenario '{ScenarioId}' has no dish_station runtime configuration.");
    public static IReadOnlyList<DishStationQuestDefinition> Quests => Narrative.Quests;

    public static DishStationQuestDefinition Quest(DishStationQuestId id) => Narrative.Quest(id);

    public static CharacterContentDefinition Character(ContentId id) =>
        Catalog.Characters.Single(character => character.Id == id);

    public static DishStationFirstShiftNarrative Compile(string yaml, string source = "<first-shift>") =>
        FromCatalog(ContentCompilerV1.Compile(yaml, source), source);

    public static DishStationFirstShiftNarrative FromCatalog(CompiledContentCatalogV1 catalog, string source = "<catalog>")
    {
        ArgumentNullException.ThrowIfNull(catalog);
        var diagnostics = new List<ContentDiagnostic>();
        var scenarioId = new ContentId(ScenarioId);
        var scenario = catalog.Scenarios.SingleOrDefault(candidate => candidate.Id == scenarioId);
        var chapter = scenario?.Narrative;
        if (chapter is null) Error("scenario.narrative", "First shift requires authored briefing and debrief narrative.");
        else
        {
            if (chapter.Briefing.Length != 3) Error("scenario.narrative.briefing", "First shift requires exactly three workplace briefing pages.");
            if (chapter.DebriefQuestions.Length != 3) Error("scenario.narrative.debrief_questions", "First shift requires exactly three debrief questions.");
        }
        var authored = catalog.Quests.Where(quest => quest.Scenario == scenarioId && quest.Narrative is not null).ToArray();
        var definitions = new List<DishStationQuestDefinition>(authored.Length);
        var seenRuntimeIds = new HashSet<DishStationQuestId>();
        var seenSequences = new HashSet<int>();
        var seenSteps = new HashSet<string>(StringComparer.Ordinal);

        foreach (var quest in authored)
        {
            var narrative = quest.Narrative!;
            if (!TryRuntimeQuest(narrative.RuntimeId, out var runtimeId))
            {
                Error($"quest[{quest.Id}].narrative.runtime_id", $"Unknown first-shift runtime quest '{narrative.RuntimeId}'.");
                continue;
            }
            if (!seenRuntimeIds.Add(runtimeId)) Error($"quest[{quest.Id}].narrative.runtime_id", $"Runtime quest '{narrative.RuntimeId}' is duplicated.");
            if (!seenSequences.Add(narrative.Sequence)) Error($"quest[{quest.Id}].narrative.sequence", $"Sequence {narrative.Sequence} is duplicated.");
            if (!TryCapability(narrative.CapabilityReward, out var capability))
            {
                Error($"quest[{quest.Id}].narrative.reward.capability", $"Unknown first-shift capability '{narrative.CapabilityReward}'.");
                continue;
            }
            if (narrative.ExperienceReward != DishStationProgressionRules.ExperienceReward(runtimeId))
                Error($"quest[{quest.Id}].narrative.reward.experience", $"Reward must match authoritative {runtimeId} progression behavior.");
            if (capability != DishStationProgressionRules.CapabilityReward(runtimeId))
                Error($"quest[{quest.Id}].narrative.reward.capability", $"Capability must match authoritative {runtimeId} progression behavior.");
            foreach (var step in narrative.Steps)
                if (!seenSteps.Add(step.Id)) Error($"quest[{quest.Id}].narrative.steps", $"Tutorial step '{step.Id}' is duplicated across the first shift.");

            definitions.Add(new(quest.Id, runtimeId, quest.Participants, narrative.Sequence, narrative.Title, narrative.Situation,
                quest.Objective, narrative.Discovery, narrative.UnlockRationale, narrative.ExperienceReward, capability,
                narrative.Steps.Select(step => new DishStationQuestStepDefinition(step.Id, step.Text, step.InputAction)).ToImmutableArray()));
        }

        var expectedCount = Enum.GetValues<DishStationQuestId>().Length;
        if (definitions.Count != expectedCount)
            Error("quests", $"First shift must define exactly {expectedCount} narrative quests; found {definitions.Count}.");
        foreach (var id in Enum.GetValues<DishStationQuestId>())
            if (!seenRuntimeIds.Contains(id)) Error("quests", $"First shift is missing runtime quest '{RuntimeToken(id)}'.");
        for (var sequence = 1; sequence <= expectedCount; sequence++)
            if (!seenSequences.Contains(sequence)) Error("quests", $"First shift is missing narrative sequence {sequence}.");
        var expectedSteps = Enum.GetValues<DishTutorialStage>().Select(stage => Kebab(stage.ToString())).ToHashSet(StringComparer.Ordinal);
        foreach (var step in expectedSteps)
            if (!seenSteps.Contains(step)) Error("quests.narrative.steps", $"First shift is missing reachable tutorial beat '{step}'.");
        foreach (var step in seenSteps)
            if (!expectedSteps.Contains(step)) Error("quests.narrative.steps", $"Tutorial beat '{step}' is unreachable by the authoritative first-shift stage model.");
        if (diagnostics.Count > 0) throw new ContentCompilationException(diagnostics);
        return new(chapter!, definitions.OrderBy(quest => quest.Sequence).ToImmutableArray());

        void Error(string path, string message) => diagnostics.Add(new(source, path, message));
    }

    public static string RuntimeToken(DishStationQuestId id) => Kebab(id.ToString());
    public static string CapabilityToken(CareerCapability capability) => $"capability.{Kebab(capability.ToString())}";

    private static CompiledContentCatalogV1 LoadDefaultCatalog()
    {
        using var stream = typeof(DishStationFirstHoursContent).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded first-shift narrative '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return ContentCompilerV1.Compile(reader.ReadToEnd(), ResourceName);
    }

    private static bool TryRuntimeQuest(string token, out DishStationQuestId id)
    {
        foreach (var candidate in Enum.GetValues<DishStationQuestId>())
            if (string.Equals(RuntimeToken(candidate), token, StringComparison.Ordinal)) { id = candidate; return true; }
        id = default;
        return false;
    }

    private static bool TryCapability(ContentId contentId, out CareerCapability capability)
    {
        foreach (var candidate in Enum.GetValues<CareerCapability>())
            if (string.Equals(CapabilityToken(candidate), contentId.Value, StringComparison.Ordinal)) { capability = candidate; return true; }
        capability = default;
        return false;
    }

    private static string Kebab(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character)) result.Append('-');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}

public static class DishStationTwoStationsContent
{
    public const string ScenarioId = "scenario.restaurant.two-stations";
    public const string QuestId = "quest.restaurant.two-stations.one-problem";

    public static ScenarioContentDefinition Scenario { get; } = DishStationFirstHoursContent.Catalog.Scenarios
        .Single(scenario => scenario.Id.Value == ScenarioId);

    public static TwoStationRoutingConfiguration Configuration { get; } = Scenario.TwoStationRouting
        ?? throw new InvalidOperationException($"Two-station scenario '{ScenarioId}' has no two_station_routing configuration.");

    public static QuestContentDefinition Quest { get; } = DishStationFirstHoursContent.Catalog.Quests
        .Single(quest => quest.Id.Value == QuestId);
}
