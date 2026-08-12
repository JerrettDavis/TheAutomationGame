using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Automation.Domain;

namespace Automation.Content;

public readonly partial record struct ContentId
{
    public ContentId(string value)
    {
        if (!IsValid(value)) throw new ArgumentException($"'{value}' is not a valid semantic content ID.", nameof(value));
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
    public static bool IsValid(string? value) => value is not null && ContentIdPattern().IsMatch(value);

    [GeneratedRegex("^[a-z][a-z0-9]*(?:\\.[a-z0-9][a-z0-9-]*)+$", RegexOptions.CultureInvariant)]
    private static partial Regex ContentIdPattern();
}

public enum ContentDefinitionKind
{
    Industry,
    Facility,
    Item,
    Workstation,
    Process,
    Scenario,
    Quest,
    Character,
    Incident,
    Pattern,
}

public sealed record IndustryContentDefinition(ContentId Id, string DisplayName);

public sealed record FacilityContentDefinition(
    ContentId Id,
    ContentId Industry,
    string DisplayName,
    ImmutableArray<ContentId> Workstations);

public sealed record ItemContentDefinition(
    ContentId Id,
    ContentId Industry,
    string DisplayName,
    ImmutableArray<string> States);

public enum WorkstationTemplateFamily
{
    Manual,
    Batch,
    Buffer,
    Inspection,
    Service,
    Transport,
}

public abstract record WorkstationBehaviorContentDefinition(WorkstationTemplateFamily Family);
public sealed record ManualWorkstationBehaviorContentDefinition(string Action)
    : WorkstationBehaviorContentDefinition(WorkstationTemplateFamily.Manual);
public sealed record BatchWorkstationBehaviorContentDefinition(int Capacity, int CycleTicks)
    : WorkstationBehaviorContentDefinition(WorkstationTemplateFamily.Batch);
public sealed record BufferWorkstationBehaviorContentDefinition(int Capacity, string Ordering)
    : WorkstationBehaviorContentDefinition(WorkstationTemplateFamily.Buffer);
public sealed record InspectionWorkstationBehaviorContentDefinition(string Observation)
    : WorkstationBehaviorContentDefinition(WorkstationTemplateFamily.Inspection);
public sealed record ServiceWorkstationBehaviorContentDefinition(DishKind DemandKind, int RequestIntervalTicks)
    : WorkstationBehaviorContentDefinition(WorkstationTemplateFamily.Service);

public sealed record WorkstationContentDefinition(
    ContentId Id,
    ContentId Industry,
    string DisplayName,
    ImmutableArray<ContentId> AcceptedItems,
    string InputState,
    string OutputState,
    ContentId Presentation,
    ContentId PresentationFallback,
    WorkstationBehaviorContentDefinition? Behavior);

public sealed record ProcessStepContentDefinition(string Id, ContentId Workstation);
public sealed record ProcessRouteContentDefinition(string From, string To);

public sealed record ProcessContentDefinition(
    ContentId Id,
    ContentId Industry,
    ImmutableArray<ProcessStepContentDefinition> Steps,
    ImmutableArray<ProcessRouteContentDefinition> Routes,
    bool AllowCycles);

public sealed record CharacterRelationshipContentDefinition(
    ContentId Character,
    ContentId Kind);

public enum CharacterDialoguePriority
{
    Ambient,
    Important,
    Critical,
}

public sealed record CharacterBarkContentDefinition(
    ContentId Id,
    ContentId Quest,
    DishStationNarrativeEventKind Trigger,
    CharacterDialoguePriority Priority,
    int CooldownTicks,
    string Line);

public sealed record CharacterContentDefinition(
    ContentId Id,
    ContentId Industry,
    string DisplayName,
    ContentId Role,
    string Motivation,
    ImmutableArray<ContentId> KnownFacts,
    ImmutableArray<ContentId> BlindSpots,
    ImmutableArray<ContentId> Authority,
    ImmutableArray<CharacterRelationshipContentDefinition> Relationships,
    ImmutableArray<CharacterBarkContentDefinition> Barks,
    ContentId Presentation,
    ContentId PresentationFallback);

public sealed record ScenarioBriefingPageContentDefinition(string Title, string Body);

public sealed record ScenarioNarrativeContentDefinition(
    string ChapterTitle,
    ImmutableArray<ScenarioBriefingPageContentDefinition> Briefing,
    string DebriefSummary,
    ImmutableArray<string> DebriefQuestions);

public sealed record ScenarioContentDefinition(
    ContentId Id,
    ContentId Industry,
    ContentId Facility,
    ImmutableArray<ContentId> Processes,
    ImmutableArray<ContentId> Items,
    ImmutableArray<ContentId> Characters,
    string Seed,
    ScenarioNarrativeContentDefinition? Narrative,
    DishStationScenarioConfiguration? DishStation,
    TwoStationRoutingConfiguration? TwoStationRouting);

public sealed record QuestCompletionContentDefinition(string Metric, string Operator, double Value);

public sealed record QuestNarrativeStepContentDefinition(
    string Id,
    string Text,
    string? InputAction);

public sealed record QuestNarrativeContentDefinition(
    string RuntimeId,
    int Sequence,
    string Title,
    string Situation,
    string Discovery,
    string UnlockRationale,
    int ExperienceReward,
    ContentId CapabilityReward,
    ImmutableArray<QuestNarrativeStepContentDefinition> Steps);

public sealed record QuestContentDefinition(
    ContentId Id,
    ContentId Scenario,
    ImmutableArray<ContentId> Participants,
    string Objective,
    QuestCompletionContentDefinition Completion,
    QuestNarrativeContentDefinition? Narrative);

public sealed record IncidentContentDefinition(
    ContentId Id,
    ContentId Industry,
    string DisplayName,
    SimulationTick TriggerAt,
    string Scope,
    string Observable,
    string Evidence,
    string Recovery,
    DishStationIncidentEffect Effect);

public sealed record PatternContentDefinition(
    ContentId Id,
    string Catalog,
    string Category,
    string ExternalCatalogId,
    string PreNameTitle,
    ImmutableArray<PatternProblemSignature> ProblemSignatures,
    int MinimumEvidence,
    bool RequiresApplication,
    ImmutableArray<ContentId> PrimaryEncounters)
{
    public PatternId PatternId => new(Id.Value);
}

public static class DishStationIncidentContentAdapter
{
    public static ScheduledDishStationIncident ToSchedule(IncidentContentDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return new(definition.TriggerAt, new(
            new(definition.Id.Value),
            definition.Scope,
            definition.Observable,
            definition.Evidence,
            definition.Recovery,
            definition.Effect));
    }
}

public sealed record ContentManifestV1(
    int SchemaVersion,
    int DefinitionCount,
    ImmutableDictionary<ContentDefinitionKind, int> Counts,
    string Sha256);

public sealed record CompiledContentCatalogV1(
    ImmutableArray<IndustryContentDefinition> Industries,
    ImmutableArray<FacilityContentDefinition> Facilities,
    ImmutableArray<ItemContentDefinition> Items,
    ImmutableArray<WorkstationContentDefinition> Workstations,
    ImmutableArray<ProcessContentDefinition> Processes,
    ImmutableArray<ScenarioContentDefinition> Scenarios,
    ImmutableArray<QuestContentDefinition> Quests,
    ImmutableArray<CharacterContentDefinition> Characters,
    ImmutableArray<IncidentContentDefinition> Incidents,
    ImmutableArray<PatternContentDefinition> Patterns,
    ContentManifestV1 Manifest);

public sealed record ContentDiagnostic(string Source, string Path, string Message, int? Line = null, int? Column = null)
{
    public override string ToString() => Line is { } line
        ? $"{Source}({line},{Column ?? 1}): {Path}: {Message}"
        : $"{Source}: {Path}: {Message}";
}

public sealed class ContentCompilationException : Exception
{
    public ContentCompilationException(IEnumerable<ContentDiagnostic> diagnostics)
        : base(string.Join(Environment.NewLine, diagnostics.Select(diagnostic => diagnostic.ToString())))
    {
        Diagnostics = diagnostics.ToImmutableArray();
    }

    public ImmutableArray<ContentDiagnostic> Diagnostics { get; }
}
