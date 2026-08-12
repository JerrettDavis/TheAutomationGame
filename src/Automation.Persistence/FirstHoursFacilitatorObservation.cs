using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automation.Persistence;

public enum FirstHoursParticipantKind
{
    Human,
    SyntheticFixture,
}

public enum FirstHoursIssueDisposition
{
    Fixed,
    Backlog,
}

public sealed record FirstHoursReadinessIssue(
    string Code,
    string Summary,
    string Owner,
    FirstHoursIssueDisposition Disposition);

public sealed record FirstHoursFacilitatorObservation(
    int SchemaVersion,
    string SessionId,
    DateTimeOffset RecordedAtUtc,
    FirstHoursParticipantKind ParticipantKind,
    bool VocabularyNovice,
    bool MovementDiscoveredWithoutCoaching,
    bool InteractionDiscoveredWithoutCoaching,
    bool MeaningfulBottleneckIdentifiedCausally,
    bool ReportedVsPhysicalReadinessUnderstood,
    bool ReplayProofValueArticulated,
    bool StrategyExpressedBeforeNaming,
    bool ActionDirectedFacilitatorHelp,
    string? PrimaryProgressionBlocker,
    FirstHoursReadinessIssue[] CriticalIssues)
{
    public const int CurrentSchemaVersion = 2;

    public FirstHoursFacilitatorObservation Validate()
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new NotSupportedException($"Facilitator observation schema {SchemaVersion} is not supported.");
        ArgumentException.ThrowIfNullOrWhiteSpace(SessionId);
        ArgumentNullException.ThrowIfNull(CriticalIssues);
        if (!Enum.IsDefined(ParticipantKind)) throw new InvalidDataException($"Unknown participant kind {ParticipantKind}.");

        var issueCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var issue in CriticalIssues)
        {
            if (string.IsNullOrWhiteSpace(issue.Code) || string.IsNullOrWhiteSpace(issue.Summary) || string.IsNullOrWhiteSpace(issue.Owner))
                throw new InvalidDataException("Every critical issue requires a code, summary, and owner.");
            if (!Enum.IsDefined(issue.Disposition))
                throw new InvalidDataException($"Critical issue '{issue.Code}' has an unknown disposition.");
            if (!issueCodes.Add(issue.Code)) throw new InvalidDataException($"Critical issue '{issue.Code}' is duplicated.");
        }
        return this;
    }
}

public static class FirstHoursFacilitatorObservationStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static void SaveFileAtomic(string path, FirstHoursFacilitatorObservation observation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(observation);
        observation.Validate();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Facilitator observation path must have a parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, observation, Options);
            stream.Flush(true);
        }
        File.Move(temporaryPath, fullPath, true);
    }

    public static FirstHoursFacilitatorObservation LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        return (JsonSerializer.Deserialize<FirstHoursFacilitatorObservation>(stream, Options)
                ?? throw new InvalidDataException("The facilitator observation file was empty."))
            .Validate();
    }
}
