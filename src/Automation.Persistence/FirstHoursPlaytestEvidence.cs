using System.Text.Json;
using System.Text.Json.Serialization;
using Automation.Simulation;

namespace Automation.Persistence;

public sealed record FirstHoursPlaytestEvidence(
    int SchemaVersion,
    string SessionId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    double WallClockSeconds,
    OnboardingSnapshot Onboarding,
    int Level,
    int Experience,
    long ActiveSimulationTicks,
    DishStationQuestProgress[] Quests,
    ShiftTrialSnapshot ShiftTrial,
    ShiftReportSnapshot ShiftReport,
    HandbookVisitEvidence[] HandbookVisits)
{
    public const int CurrentSchemaVersion = 2;

    public static FirstHoursPlaytestEvidence Create(
        string sessionId,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        DishStationSnapshot snapshot,
        IReadOnlyList<HandbookVisitEvidence>? handbookVisits = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(snapshot);
        if (completedAtUtc < startedAtUtc) throw new ArgumentOutOfRangeException(nameof(completedAtUtc));
        if (snapshot.Progression.ActiveQuest is not null || !snapshot.ShiftReport.Available)
            throw new InvalidOperationException("First-hours evidence can be recorded only after the final outcome is complete.");

        return new(
            CurrentSchemaVersion,
            sessionId,
            startedAtUtc,
            completedAtUtc,
            (completedAtUtc - startedAtUtc).TotalSeconds,
            snapshot.Onboarding,
            snapshot.Progression.Level,
            snapshot.Progression.Experience,
            snapshot.Progression.ActivePlayTicks,
            snapshot.Progression.Quests.ToArray(),
            snapshot.ShiftTrial,
            snapshot.ShiftReport,
            handbookVisits?.ToArray() ?? []);
    }
}

public readonly record struct HandbookVisitEvidence(
    DishTutorialStage Stage,
    int OpenCount,
    long FirstOpenedAtTick,
    long LastOpenedAtTick);

public static class FirstHoursPlaytestEvidenceStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static void SaveFileAtomic(string path, FirstHoursPlaytestEvidence evidence)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(evidence);
        if (evidence.SchemaVersion != FirstHoursPlaytestEvidence.CurrentSchemaVersion)
            throw new NotSupportedException($"Playtest evidence schema {evidence.SchemaVersion} is not supported.");

        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Playtest evidence path must have a parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, evidence, Options);
            stream.Flush(true);
        }
        File.Move(temporaryPath, fullPath, true);
    }

    public static FirstHoursPlaytestEvidence LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        var evidence = JsonSerializer.Deserialize<FirstHoursPlaytestEvidence>(stream, Options)
            ?? throw new InvalidDataException("The playtest evidence file was empty.");
        if (evidence.SchemaVersion != FirstHoursPlaytestEvidence.CurrentSchemaVersion)
            throw new NotSupportedException($"Playtest evidence schema {evidence.SchemaVersion} is not supported.");
        return evidence;
    }
}
