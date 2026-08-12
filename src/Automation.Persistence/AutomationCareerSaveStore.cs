using System.Text.Json;
using System.Text.Json.Serialization;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Persistence;

public sealed record AutomationCareerSave(
    int SchemaVersion,
    DishStationReplaySave FirstShift,
    TwoStationRoutingReplaySave TwoStationRouting,
    PatternKnowledgeProfile PatternKnowledge)
{
    public const int CurrentSchemaVersion = 1;
}

public sealed record AutomationCareerState(
    DishStationWorld FirstShift,
    TwoStationRoutingWorld TwoStationRouting,
    PatternKnowledgeProfile PatternKnowledge);

public static class AutomationCareerSaveStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new PatternIdJsonConverter(), new PatternEvidenceIdJsonConverter() },
    };

    public static string Serialize(AutomationCareerState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(ToSave(state), Options);
    }

    public static AutomationCareerState Deserialize(
        string json,
        int legacyRoutingSeed,
        TwoStationRoutingConfiguration legacyRoutingConfiguration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentNullException.ThrowIfNull(legacyRoutingConfiguration);
        using var document = JsonDocument.Parse(json);
        if (!document.RootElement.TryGetProperty("firstShift", out _))
            return new(DishStationSaveStore.Deserialize(json),
                new TwoStationRoutingWorld(legacyRoutingSeed, legacyRoutingConfiguration), PatternKnowledgeProfile.Empty);

        var save = JsonSerializer.Deserialize<AutomationCareerSave>(json, Options)
            ?? throw new InvalidDataException("The career save did not contain a checkpoint.");
        if (save.SchemaVersion != AutomationCareerSave.CurrentSchemaVersion)
            throw new NotSupportedException($"Career save schema {save.SchemaVersion} is not supported.");
        if (save.FirstShift is null || save.TwoStationRouting is null || save.PatternKnowledge is null)
            throw new InvalidDataException("The career save is incomplete.");
        if (save.PatternKnowledge.Patterns.Select(item => item.Pattern).Distinct().Count() != save.PatternKnowledge.Patterns.Length)
            throw new InvalidDataException("The career save contains duplicate pattern journals.");
        return new(DishStationWorld.Restore(save.FirstShift), TwoStationRoutingWorld.Restore(save.TwoStationRouting),
            save.PatternKnowledge);
    }

    public static void SaveFileAtomic(string path, AutomationCareerState state)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(state);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Career save path must have a parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, ToSave(state), Options);
            stream.Flush(true);
        }
        File.Move(temporaryPath, fullPath, true);
    }

    public static AutomationCareerState LoadFile(
        string path,
        int legacyRoutingSeed,
        TwoStationRoutingConfiguration legacyRoutingConfiguration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        return Deserialize(File.ReadAllText(Path.GetFullPath(path)), legacyRoutingSeed, legacyRoutingConfiguration);
    }

    private static AutomationCareerSave ToSave(AutomationCareerState state) => new(
        AutomationCareerSave.CurrentSchemaVersion,
        state.FirstShift.CreateReplaySave(),
        state.TwoStationRouting.CreateReplaySave(),
        state.PatternKnowledge);

    private sealed class PatternIdJsonConverter : JsonConverter<PatternId>
    {
        public override PatternId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? throw new JsonException("Pattern ID cannot be null."));

        public override void Write(Utf8JsonWriter writer, PatternId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }

    private sealed class PatternEvidenceIdJsonConverter : JsonConverter<PatternEvidenceId>
    {
        public override PatternEvidenceId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            new(reader.GetString() ?? throw new JsonException("Pattern evidence ID cannot be null."));

        public override void Write(Utf8JsonWriter writer, PatternEvidenceId value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.Value);
    }
}
