using System.Text.Json;
using System.Text.Json.Serialization;

namespace Automation.Client.Stride;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ClientWindowMode
{
    Windowed,
    BorderlessFullscreen,
}

public enum ClientSettingsOption
{
    MasterVolume,
    UiScale,
    CameraSensitivity,
    WindowMode,
    ResetDefaults,
}

public sealed record ClientSettings
{
    public const int CurrentSchemaVersion = 1;
    public const int MinimumUiScalePercent = 75;
    public const int MaximumUiScalePercent = 100;
    public const int MinimumCameraSensitivityPercent = 50;
    public const int MaximumCameraSensitivityPercent = 200;

    public static ClientSettings Default { get; } = new(
        CurrentSchemaVersion,
        masterVolumePercent: 100,
        uiScalePercent: 100,
        cameraSensitivityPercent: 100,
        ClientWindowMode.BorderlessFullscreen,
        InputBindingProfile.Default);

    [JsonConstructor]
    public ClientSettings(
        int schemaVersion,
        int masterVolumePercent,
        int uiScalePercent,
        int cameraSensitivityPercent,
        ClientWindowMode windowMode,
        InputBindingProfile inputBindings)
    {
        if (schemaVersion != CurrentSchemaVersion)
            throw new ArgumentOutOfRangeException(nameof(schemaVersion), $"Unsupported client settings schema {schemaVersion}.");
        if (masterVolumePercent is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(masterVolumePercent));
        if (uiScalePercent is < MinimumUiScalePercent or > MaximumUiScalePercent)
            throw new ArgumentOutOfRangeException(nameof(uiScalePercent));
        if (cameraSensitivityPercent is < MinimumCameraSensitivityPercent or > MaximumCameraSensitivityPercent)
            throw new ArgumentOutOfRangeException(nameof(cameraSensitivityPercent));
        if (!Enum.IsDefined(windowMode)) throw new ArgumentOutOfRangeException(nameof(windowMode));
        ArgumentNullException.ThrowIfNull(inputBindings);

        SchemaVersion = schemaVersion;
        MasterVolumePercent = masterVolumePercent;
        UiScalePercent = uiScalePercent;
        CameraSensitivityPercent = cameraSensitivityPercent;
        WindowMode = windowMode;
        InputBindings = inputBindings;
    }

    public int SchemaVersion { get; }
    public int MasterVolumePercent { get; }
    public int UiScalePercent { get; }
    public int CameraSensitivityPercent { get; }
    public ClientWindowMode WindowMode { get; }
    public InputBindingProfile InputBindings { get; }

    public ClientSettings Adjust(ClientSettingsOption option, int direction)
    {
        if (!Enum.IsDefined(option)) throw new ArgumentOutOfRangeException(nameof(option));
        if (direction == 0) return this;
        var sign = Math.Sign(direction);
        return option switch
        {
            ClientSettingsOption.MasterVolume => Copy(masterVolumePercent: Math.Clamp(MasterVolumePercent + sign * 10, 0, 100)),
            ClientSettingsOption.UiScale => Copy(uiScalePercent: Math.Clamp(UiScalePercent + sign * 25, MinimumUiScalePercent, MaximumUiScalePercent)),
            ClientSettingsOption.CameraSensitivity => Copy(cameraSensitivityPercent: Math.Clamp(CameraSensitivityPercent + sign * 25, MinimumCameraSensitivityPercent, MaximumCameraSensitivityPercent)),
            ClientSettingsOption.WindowMode => Copy(windowMode: WindowMode == ClientWindowMode.Windowed
                ? ClientWindowMode.BorderlessFullscreen
                : ClientWindowMode.Windowed),
            ClientSettingsOption.ResetDefaults => Default,
            _ => this,
        };
    }

    public ClientSettings WithInputBindings(InputBindingProfile inputBindings) => Copy(inputBindings: inputBindings);

    private ClientSettings Copy(
        int? masterVolumePercent = null,
        int? uiScalePercent = null,
        int? cameraSensitivityPercent = null,
        ClientWindowMode? windowMode = null,
        InputBindingProfile? inputBindings = null) => new(
            CurrentSchemaVersion,
            masterVolumePercent ?? MasterVolumePercent,
            uiScalePercent ?? UiScalePercent,
            cameraSensitivityPercent ?? CameraSensitivityPercent,
            windowMode ?? WindowMode,
            inputBindings ?? InputBindings);
}

public static class ClientSettingsStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "TheAutomationGame",
        "settings.json");

    public static void SaveFileAtomic(string path, ClientSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(settings);
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new ArgumentException("Settings path must have a parent directory.", nameof(path));
        Directory.CreateDirectory(directory);
        var temporaryPath = fullPath + ".tmp";
        using (var stream = new FileStream(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            JsonSerializer.Serialize(stream, settings, Options);
            stream.Flush(true);
        }
        File.Move(temporaryPath, fullPath, true);
    }

    public static ClientSettings LoadFile(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        return JsonSerializer.Deserialize<ClientSettings>(stream, Options)
            ?? throw new InvalidDataException("The client settings file was empty.");
    }

    public static ClientSettings LoadFileOrDefault(string path, out bool usedFallback)
    {
        try
        {
            if (!File.Exists(path))
            {
                usedFallback = true;
                return ClientSettings.Default;
            }

            var settings = LoadFile(path);
            usedFallback = false;
            return settings;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or
                                          InvalidDataException or ArgumentException or NotSupportedException)
        {
            usedFallback = true;
            return ClientSettings.Default;
        }
    }
}

public static class ClientStartupSettings
{
    public static ClientWindowMode ResolveWindowMode(
        ClientSettings settings,
        bool forceWindowed,
        bool forceFullscreen)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (forceWindowed) return ClientWindowMode.Windowed;
        if (forceFullscreen) return ClientWindowMode.BorderlessFullscreen;
        return settings.WindowMode;
    }
}

public readonly record struct ClientCanvasTransform(float Scale, float OffsetX, float OffsetY)
{
    public (float X, float Y) ToVirtual(float physicalX, float physicalY) =>
        ((physicalX - OffsetX) / Scale, (physicalY - OffsetY) / Scale);
}

public static class ClientCanvasLayout
{
    public static ClientCanvasTransform Fit(
        float physicalWidth,
        float physicalHeight,
        float virtualWidth,
        float virtualHeight,
        int scalePercent = 100)
    {
        if (physicalWidth <= 0) throw new ArgumentOutOfRangeException(nameof(physicalWidth));
        if (physicalHeight <= 0) throw new ArgumentOutOfRangeException(nameof(physicalHeight));
        if (virtualWidth <= 0) throw new ArgumentOutOfRangeException(nameof(virtualWidth));
        if (virtualHeight <= 0) throw new ArgumentOutOfRangeException(nameof(virtualHeight));
        if (scalePercent is < ClientSettings.MinimumUiScalePercent or > ClientSettings.MaximumUiScalePercent)
            throw new ArgumentOutOfRangeException(nameof(scalePercent));

        var fittedScale = Math.Max(0.5f, Math.Min(physicalWidth / virtualWidth, physicalHeight / virtualHeight));
        var scale = fittedScale * scalePercent / 100f;
        return new(scale,
            (physicalWidth - virtualWidth * scale) * 0.5f,
            (physicalHeight - virtualHeight * scale) * 0.5f);
    }
}
