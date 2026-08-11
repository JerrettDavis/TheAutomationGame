using System.Text.Json;
using System.Text.Json.Serialization;
using Automation.Simulation;

namespace Automation.Persistence;

public static class DishStationSaveStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Serialize(DishStationWorld world)
    {
        ArgumentNullException.ThrowIfNull(world);
        return JsonSerializer.Serialize(world.CreateReplaySave(), Options);
    }

    public static DishStationWorld Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var save = JsonSerializer.Deserialize<DishStationReplaySave>(json, Options)
            ?? throw new InvalidDataException("The dish-station save did not contain a replay checkpoint.");
        return DishStationWorld.Restore(save);
    }

    public static void Save(Stream stream, DishStationWorld world)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(world);
        JsonSerializer.Serialize(stream, world.CreateReplaySave(), Options);
    }

    public static DishStationWorld Load(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var save = JsonSerializer.Deserialize<DishStationReplaySave>(stream, Options)
            ?? throw new InvalidDataException("The dish-station save did not contain a replay checkpoint.");
        return DishStationWorld.Restore(save);
    }

    public static void SaveFileAtomic(string path, DishStationWorld world)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(world);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Career save path must have a parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            Save(stream, world);
            stream.Flush(true);
        }
        File.Move(temporaryPath, fullPath, true);
    }

    public static DishStationWorld LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        return Load(stream);
    }
}
