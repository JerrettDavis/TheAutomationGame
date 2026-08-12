using Automation.Client.Stride;

namespace Automation.Integration.Tests;

public sealed class ClientSettingsTests
{
    [Fact]
    public void ChangedSettingsAndInputBindingSurviveAStoreReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"automation-settings-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var changed = ClientSettings.Default
                .Adjust(ClientSettingsOption.MasterVolume, -1)
                .Adjust(ClientSettingsOption.UiScale, -1)
                .Adjust(ClientSettingsOption.CameraSensitivity, 1)
                .Adjust(ClientSettingsOption.WindowMode, 1)
                .WithInputBindings(InputBindingProfile.Default.WithBinding(GameInputAction.Interact, KeyboardKey.Space));

            ClientSettingsStore.SaveFileAtomic(path, changed);
            var restarted = ClientSettingsStore.LoadFile(path);

            Assert.Equal(90, restarted.MasterVolumePercent);
            Assert.Equal(75, restarted.UiScalePercent);
            Assert.Equal(125, restarted.CameraSensitivityPercent);
            Assert.Equal(ClientWindowMode.Windowed, restarted.WindowMode);
            Assert.True(restarted.InputBindings.Matches(GameInputAction.Interact, KeyboardKey.Space));
            Assert.False(restarted.InputBindings.Matches(GameInputAction.Interact, KeyboardKey.E));
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [Fact]
    public void MissingOrCorruptSettingsFallBackWithoutBreakingStartup()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"automation-settings-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "settings.json");
        try
        {
            var missing = ClientSettingsStore.LoadFileOrDefault(path, out var missingFallback);
            Directory.CreateDirectory(directory);
            File.WriteAllText(path, "{ definitely-not-json }");
            var corrupt = ClientSettingsStore.LoadFileOrDefault(path, out var corruptFallback);
            ClientSettingsStore.SaveFileAtomic(path, ClientSettings.Default);
            var invalidJson = File.ReadAllText(path).Replace(
                "\"masterVolumePercent\": 100",
                "\"masterVolumePercent\": 101",
                StringComparison.Ordinal);
            File.WriteAllText(path, invalidJson);
            var invalid = ClientSettingsStore.LoadFileOrDefault(path, out var invalidFallback);

            Assert.True(missingFallback);
            Assert.True(corruptFallback);
            Assert.True(invalidFallback);
            Assert.Same(ClientSettings.Default, missing);
            Assert.Same(ClientSettings.Default, corrupt);
            Assert.Same(ClientSettings.Default, invalid);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
            if (Directory.Exists(directory)) Directory.Delete(directory);
        }
    }

    [Fact]
    public void SettingsRangesClampAtSupportedPresentationLimits()
    {
        var settings = ClientSettings.Default;
        for (var index = 0; index < 20; index++)
        {
            settings = settings
                .Adjust(ClientSettingsOption.MasterVolume, -1)
                .Adjust(ClientSettingsOption.UiScale, -1)
                .Adjust(ClientSettingsOption.CameraSensitivity, 1);
        }

        Assert.Equal(0, settings.MasterVolumePercent);
        Assert.Equal(ClientSettings.MinimumUiScalePercent, settings.UiScalePercent);
        Assert.Equal(ClientSettings.MaximumCameraSensitivityPercent, settings.CameraSensitivityPercent);
    }

    [Fact]
    public void InvalidPersistedValuesFailValidation()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClientSettings(
            ClientSettings.CurrentSchemaVersion,
            101,
            100,
            100,
            ClientWindowMode.Windowed,
            InputBindingProfile.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() => new ClientSettings(
            99,
            100,
            100,
            100,
            ClientWindowMode.Windowed,
            InputBindingProfile.Default));
    }

    [Fact]
    public void ExplicitWindowModeOverridesWinOverPersistedPreference()
    {
        var windowed = ClientSettings.Default.Adjust(ClientSettingsOption.WindowMode, 1);

        Assert.Equal(ClientWindowMode.Windowed,
            ClientStartupSettings.ResolveWindowMode(windowed, forceWindowed: false, forceFullscreen: false));
        Assert.Equal(ClientWindowMode.BorderlessFullscreen,
            ClientStartupSettings.ResolveWindowMode(windowed, forceWindowed: false, forceFullscreen: true));
        Assert.Equal(ClientWindowMode.Windowed,
            ClientStartupSettings.ResolveWindowMode(ClientSettings.Default, forceWindowed: true, forceFullscreen: false));
    }

    [Fact]
    public void UiScaleRemainsFittedAndUsesTheSameTransformForPointerMapping()
    {
        var full = ClientCanvasLayout.Fit(1920, 1080, 1024, 600, 100);
        var reduced = ClientCanvasLayout.Fit(1920, 1080, 1024, 600, 75);
        var physicalCenterX = reduced.OffsetX + 512 * reduced.Scale;
        var physicalCenterY = reduced.OffsetY + 300 * reduced.Scale;

        var virtualCenter = reduced.ToVirtual(physicalCenterX, physicalCenterY);

        Assert.True(reduced.Scale < full.Scale);
        Assert.True(reduced.OffsetX > full.OffsetX);
        Assert.Equal(512, virtualCenter.X, 3);
        Assert.Equal(300, virtualCenter.Y, 3);
    }
}
