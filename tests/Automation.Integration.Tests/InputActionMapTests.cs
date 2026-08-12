using System.Text.Json;
using Automation.Client.Stride;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class InputActionMapTests
{
    [Fact]
    public void DefaultsDefineEveryLogicalActionAndProductionBindingsMatchCurrentControls()
    {
        var profile = InputBindingProfile.Default;

        Assert.All(Enum.GetValues<GameInputAction>(), action =>
        {
            Assert.NotEmpty(profile.KeysFor(action).ToArray());
            Assert.NotEqual(InputActionContext.None, InputActionCatalog.ContextOf(action));
        });
        Assert.All(Enum.GetValues<KeyboardKey>(), key => StrideKeyboardAdapter.ToStrideKey(key));
        Assert.True(profile.Matches(GameInputAction.MoveAway, KeyboardKey.W));
        Assert.True(profile.Matches(GameInputAction.Interact, KeyboardKey.E));
        Assert.True(profile.Matches(GameInputAction.Inspect, KeyboardKey.F));
        Assert.True(profile.Matches(GameInputAction.ProcessCaptureToggle, KeyboardKey.H));
        Assert.True(profile.Matches(GameInputAction.ProcessEditorToggle, KeyboardKey.Enter));
        Assert.Equal(InputActionContext.ProcessEditor, InputActionCatalog.ContextOf(GameInputAction.ProcessEditorMoveUp));
        Assert.True(profile.Matches(GameInputAction.AutomationEditorToggle, KeyboardKey.Digit6));
        Assert.Equal(InputActionContext.AutomationEditor, InputActionCatalog.ContextOf(GameInputAction.AutomationEditorToggleValue));
        Assert.Equal(InputActionContext.Gameplay, InputActionCatalog.ContextOf(GameInputAction.ProcessCaptureToggle));
        Assert.Equal("C / HOME", profile.DisplayName(GameInputAction.CameraReset));
    }

    [Fact]
    public void RemappingAKeyPreservesLogicalActionAndChangesItsVisibleHint()
    {
        var remapped = InputBindingProfile.Default.WithBinding(GameInputAction.MoveAway, KeyboardKey.Up);

        Assert.False(remapped.Matches(GameInputAction.MoveAway, KeyboardKey.W));
        Assert.True(remapped.Matches(GameInputAction.MoveAway, KeyboardKey.Up));
        Assert.Equal("UP", remapped.DisplayName(GameInputAction.MoveAway));

        var command = GameplayMovementInput.CreateCommand(
            DirectMovementInput.Away,
            new FloorCell(6, 4),
            new SimulationTick(3));
        Assert.Equal(new FloorCell(5, 3), command?.Destination);
    }

    [Fact]
    public void DeveloperActionsAreClassifiedAndCannotMatchWhenUnavailable()
    {
        var profile = InputBindingProfile.Default;

        Assert.Equal(InputActionContext.Developer, InputActionCatalog.ContextOf(GameInputAction.DeveloperToggle));
        Assert.False(profile.Matches(GameInputAction.DeveloperToggle, KeyboardKey.F1, developerActionsAvailable: false));
        Assert.True(profile.Matches(GameInputAction.DeveloperToggle, KeyboardKey.F1, developerActionsAvailable: true));
        Assert.True(profile.Matches(GameInputAction.Interact, KeyboardKey.E, developerActionsAvailable: false));
    }

    [Fact]
    public void BindingProfileRoundTripsAsVersionedJson()
    {
        var expected = InputBindingProfile.Default.WithBinding(GameInputAction.Interact, KeyboardKey.Space);

        var json = JsonSerializer.Serialize(expected);
        var restored = JsonSerializer.Deserialize<InputBindingProfile>(json);

        Assert.NotNull(restored);
        Assert.Contains("\"Interact\"", json, StringComparison.Ordinal);
        Assert.Contains("\"Space\"", json, StringComparison.Ordinal);
        Assert.Equal(InputBindingProfile.CurrentSchemaVersion, restored.SchemaVersion);
        Assert.Equal(expected.Bindings, restored.Bindings);
        Assert.True(restored.Matches(GameInputAction.Interact, KeyboardKey.Space));
        Assert.Equal("SPACE", restored.DisplayName(GameInputAction.Interact));
    }

    [Fact]
    public void SchemaTwoProfileMigratesWithoutLosingExistingRemaps()
    {
        var legacy = InputBindingProfile.Default
            .WithBinding(GameInputAction.Interact, KeyboardKey.Space)
            .Bindings
            .Where(binding => InputActionCatalog.ContextOf(binding.Action) != InputActionContext.AutomationEditor &&
                              binding.Action != GameInputAction.AutomationEditorToggle)
            .ToArray();

        var migrated = new InputBindingProfile(2, legacy);

        Assert.Equal(InputBindingProfile.CurrentSchemaVersion, migrated.SchemaVersion);
        Assert.True(migrated.Matches(GameInputAction.Interact, KeyboardKey.Space));
        Assert.False(migrated.Matches(GameInputAction.Interact, KeyboardKey.E));
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorToggle, KeyboardKey.Digit6));
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorApply, KeyboardKey.Enter));
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorRunComparison, KeyboardKey.R));
    }

    [Fact]
    public void SchemaThreeProfileAddsComparisonControlsAndKeepsRuleEditorRemaps()
    {
        var legacy = InputBindingProfile.Default
            .WithBinding(GameInputAction.AutomationEditorToggle, KeyboardKey.F8)
            .Bindings
            .Where(binding => binding.Action is not (GameInputAction.AutomationEditorSaveBaseline or
                GameInputAction.AutomationEditorSaveVariant or GameInputAction.AutomationEditorRunComparison))
            .ToArray();

        var migrated = new InputBindingProfile(3, legacy);

        Assert.Equal(InputBindingProfile.CurrentSchemaVersion, migrated.SchemaVersion);
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorToggle, KeyboardKey.F8));
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorSaveBaseline, KeyboardKey.B));
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorSaveVariant, KeyboardKey.V));
        Assert.True(migrated.Matches(GameInputAction.AutomationEditorRunComparison, KeyboardKey.R));
    }

    [Fact]
    public void InvalidProfilesFailFast()
    {
        var missingAction = InputBindingProfile.Default.Bindings
            .Where(binding => binding.Action != GameInputAction.MenuPrevious)
            .ToArray();
        var duplicate = InputBindingProfile.Default.Bindings
            .Append(InputBindingProfile.Default.Bindings[0])
            .ToArray();

        Assert.Throws<ArgumentException>(() => new InputBindingProfile(InputBindingProfile.CurrentSchemaVersion, missingAction));
        Assert.Throws<ArgumentException>(() => new InputBindingProfile(InputBindingProfile.CurrentSchemaVersion, duplicate));
        Assert.Throws<ArgumentOutOfRangeException>(() => new InputBindingProfile(99, InputBindingProfile.Default.Bindings));
    }
}
