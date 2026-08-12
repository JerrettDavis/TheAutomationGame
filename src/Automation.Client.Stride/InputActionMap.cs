using System.Text.Json.Serialization;

namespace Automation.Client.Stride;

[Flags]
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum InputActionContext
{
    None = 0,
    Menu = 1 << 0,
    Intro = 1 << 1,
    Gameplay = 1 << 2,
    Journal = 1 << 3,
    Help = 1 << 4,
    ShiftReport = 1 << 5,
    Placement = 1 << 6,
    Developer = 1 << 7,
    Settings = 1 << 8,
    ProcessEditor = 1 << 9,
    AutomationEditor = 1 << 10,
    TwoStationRouting = 1 << 11,
    PatternCodex = 1 << 12,
    VendorComparison = 1 << 13,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GameInputAction
{
    MenuPrevious,
    MenuNext,
    MenuConfirm,
    MenuBack,
    IntroPrevious,
    IntroNext,
    IntroConfirm,
    IntroExit,
    HelpClose,
    JournalToggle,
    JournalBack,
    JournalPrevious,
    JournalNext,
    JournalDetail,
    ShiftReportToggle,
    ShiftReportClose,
    SettingsToggle,
    SettingsPrevious,
    SettingsNext,
    SettingsDecrease,
    SettingsIncrease,
    SettingsConfirm,
    SettingsReset,
    SettingsClose,
    PreviousTarget,
    Interact,
    Inspect,
    ContextInteract,
    DirectScrape,
    DirectRack,
    DirectStartWasher,
    DirectUnload,
    DirectDryAndRestock,
    NextDish,
    ToggleRush,
    ConfirmBottleneck,
    ConfigureFlowCell,
    ToggleNewHire,
    TrainHappyPath,
    TrainRushPriority,
    TrainRareTray,
    InspectIncident,
    ReplayIncident,
    NextLens,
    ToggleProcessLens,
    ProcessCaptureToggle,
    ProcessEditorToggle,
    ProcessEditorPrevious,
    ProcessEditorNext,
    ProcessEditorMoveUp,
    ProcessEditorMoveDown,
    ProcessEditorToggleAssignment,
    ProcessEditorNextRouting,
    ProcessEditorApply,
    ProcessEditorClose,
    AutomationEditorToggle,
    AutomationEditorPrevious,
    AutomationEditorNext,
    AutomationEditorToggleValue,
    AutomationEditorApply,
    AutomationEditorClose,
    AutomationEditorSaveBaseline,
    AutomationEditorSaveVariant,
    AutomationEditorRunComparison,
    TwoStationRoutingToggle,
    TwoStationRoutingPreviousStation,
    TwoStationRoutingNextStation,
    TwoStationRoutingPreviousPolicy,
    TwoStationRoutingNextPolicy,
    TwoStationRoutingCopy,
    TwoStationRoutingRunTrial,
    TwoStationRoutingClose,
    PatternCodexToggle,
    PatternCodexReflect,
    PatternCodexClose,
    VendorComparisonToggle,
    VendorComparisonPrevious,
    VendorComparisonNext,
    VendorComparisonRunTrial,
    VendorComparisonClose,
    TogglePlacement,
    PlacementPrevious,
    PlacementNext,
    PlacementLeft,
    PlacementRight,
    PlacementUp,
    PlacementDown,
    PlacementConfirm,
    PlacementUndo,
    PlacementReset,
    CameraPanLeft,
    CameraPanRight,
    CameraPanUp,
    CameraPanDown,
    CameraZoomIn,
    CameraZoomOut,
    CameraReset,
    Exit,
    MoveAway,
    MoveLeft,
    MoveToward,
    MoveRight,
    DeveloperToggle,
    DeveloperAddDirty,
    DeveloperSetCleanSupply,
    DeveloperReset,
    DeveloperTogglePause,
    DeveloperStep,
    DeveloperStickyReady,
    DeveloperToggleLayout,
    DeveloperToggleBenchmark,
    DeveloperQuickSave,
    DeveloperQuickLoad,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KeyboardKey
{
    A, B, C, D, E, F, G, H, I, J, K, L, M, N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    Digit1, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9,
    Left, Right, Up, Down,
    Enter, Space, Escape, Tab, Backspace, Home,
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
}

public readonly record struct KeyboardBinding(GameInputAction Action, KeyboardKey Key);

public sealed class InputBindingProfile
{
    public const int CurrentSchemaVersion = 8;
    private readonly KeyboardKey[][] keysByAction;
    private readonly string[] displayNames;
    private readonly string[] primaryDisplayNames;
    private readonly KeyboardBinding[] bindings;

    public static InputBindingProfile Default { get; } = new(CurrentSchemaVersion,
    [
        new(GameInputAction.MenuPrevious, KeyboardKey.Q), new(GameInputAction.MenuPrevious, KeyboardKey.Left),
        new(GameInputAction.MenuNext, KeyboardKey.E), new(GameInputAction.MenuNext, KeyboardKey.Right),
        new(GameInputAction.MenuConfirm, KeyboardKey.Enter), new(GameInputAction.MenuConfirm, KeyboardKey.Space),
        new(GameInputAction.MenuBack, KeyboardKey.Escape),
        new(GameInputAction.IntroPrevious, KeyboardKey.Q), new(GameInputAction.IntroNext, KeyboardKey.E),
        new(GameInputAction.IntroConfirm, KeyboardKey.Enter), new(GameInputAction.IntroConfirm, KeyboardKey.Space),
        new(GameInputAction.IntroExit, KeyboardKey.Escape),
        new(GameInputAction.HelpClose, KeyboardKey.F12), new(GameInputAction.HelpClose, KeyboardKey.Escape),
        new(GameInputAction.JournalToggle, KeyboardKey.J), new(GameInputAction.JournalBack, KeyboardKey.Escape),
        new(GameInputAction.JournalPrevious, KeyboardKey.Up), new(GameInputAction.JournalPrevious, KeyboardKey.Q),
        new(GameInputAction.JournalNext, KeyboardKey.Down), new(GameInputAction.JournalNext, KeyboardKey.E),
        new(GameInputAction.JournalDetail, KeyboardKey.Enter), new(GameInputAction.JournalDetail, KeyboardKey.Space),
        new(GameInputAction.ShiftReportToggle, KeyboardKey.K),
        new(GameInputAction.ShiftReportClose, KeyboardKey.K), new(GameInputAction.ShiftReportClose, KeyboardKey.Escape),
        new(GameInputAction.SettingsToggle, KeyboardKey.O),
        new(GameInputAction.SettingsPrevious, KeyboardKey.Up), new(GameInputAction.SettingsNext, KeyboardKey.Down),
        new(GameInputAction.SettingsDecrease, KeyboardKey.Left), new(GameInputAction.SettingsIncrease, KeyboardKey.Right),
        new(GameInputAction.SettingsConfirm, KeyboardKey.Enter), new(GameInputAction.SettingsConfirm, KeyboardKey.Space),
        new(GameInputAction.SettingsReset, KeyboardKey.Backspace),
        new(GameInputAction.SettingsClose, KeyboardKey.Escape), new(GameInputAction.SettingsClose, KeyboardKey.O),
        new(GameInputAction.PreviousTarget, KeyboardKey.Q),
        new(GameInputAction.Interact, KeyboardKey.E), new(GameInputAction.Inspect, KeyboardKey.F),
        new(GameInputAction.ContextInteract, KeyboardKey.Space),
        new(GameInputAction.DirectScrape, KeyboardKey.Digit1), new(GameInputAction.DirectRack, KeyboardKey.Digit2),
        new(GameInputAction.DirectStartWasher, KeyboardKey.Digit3), new(GameInputAction.DirectUnload, KeyboardKey.Digit4),
        new(GameInputAction.DirectDryAndRestock, KeyboardKey.Digit5), new(GameInputAction.NextDish, KeyboardKey.Tab),
        new(GameInputAction.ToggleRush, KeyboardKey.R), new(GameInputAction.ConfirmBottleneck, KeyboardKey.B),
        new(GameInputAction.ConfigureFlowCell, KeyboardKey.G), new(GameInputAction.ToggleNewHire, KeyboardKey.N),
        new(GameInputAction.TrainHappyPath, KeyboardKey.T), new(GameInputAction.TrainRushPriority, KeyboardKey.Y),
        new(GameInputAction.TrainRareTray, KeyboardKey.U), new(GameInputAction.InspectIncident, KeyboardKey.I),
        new(GameInputAction.ReplayIncident, KeyboardKey.P), new(GameInputAction.NextLens, KeyboardKey.V),
        new(GameInputAction.ToggleProcessLens, KeyboardKey.L),
        new(GameInputAction.ProcessCaptureToggle, KeyboardKey.H),
        new(GameInputAction.ProcessEditorToggle, KeyboardKey.Enter),
        new(GameInputAction.ProcessEditorPrevious, KeyboardKey.Up), new(GameInputAction.ProcessEditorNext, KeyboardKey.Down),
        new(GameInputAction.ProcessEditorMoveUp, KeyboardKey.Q), new(GameInputAction.ProcessEditorMoveDown, KeyboardKey.E),
        new(GameInputAction.ProcessEditorToggleAssignment, KeyboardKey.A),
        new(GameInputAction.ProcessEditorNextRouting, KeyboardKey.R),
        new(GameInputAction.ProcessEditorApply, KeyboardKey.Enter), new(GameInputAction.ProcessEditorApply, KeyboardKey.Space),
        new(GameInputAction.ProcessEditorClose, KeyboardKey.Escape),
        new(GameInputAction.AutomationEditorToggle, KeyboardKey.Digit6),
        new(GameInputAction.AutomationEditorPrevious, KeyboardKey.Up), new(GameInputAction.AutomationEditorNext, KeyboardKey.Down),
        new(GameInputAction.AutomationEditorToggleValue, KeyboardKey.Space),
        new(GameInputAction.AutomationEditorApply, KeyboardKey.Enter),
        new(GameInputAction.AutomationEditorClose, KeyboardKey.Escape),
        new(GameInputAction.AutomationEditorSaveBaseline, KeyboardKey.B),
        new(GameInputAction.AutomationEditorSaveVariant, KeyboardKey.V),
        new(GameInputAction.AutomationEditorRunComparison, KeyboardKey.R),
        new(GameInputAction.TwoStationRoutingToggle, KeyboardKey.Digit7),
        new(GameInputAction.TwoStationRoutingPreviousStation, KeyboardKey.Left),
        new(GameInputAction.TwoStationRoutingNextStation, KeyboardKey.Right),
        new(GameInputAction.TwoStationRoutingPreviousPolicy, KeyboardKey.Up),
        new(GameInputAction.TwoStationRoutingNextPolicy, KeyboardKey.Down),
        new(GameInputAction.TwoStationRoutingCopy, KeyboardKey.C),
        new(GameInputAction.TwoStationRoutingRunTrial, KeyboardKey.Enter),
        new(GameInputAction.TwoStationRoutingClose, KeyboardKey.Escape),
        new(GameInputAction.PatternCodexToggle, KeyboardKey.Digit8),
        new(GameInputAction.PatternCodexReflect, KeyboardKey.Enter),
        new(GameInputAction.PatternCodexClose, KeyboardKey.Escape),
        new(GameInputAction.VendorComparisonToggle, KeyboardKey.Digit9),
        new(GameInputAction.VendorComparisonPrevious, KeyboardKey.Left),
        new(GameInputAction.VendorComparisonNext, KeyboardKey.Right),
        new(GameInputAction.VendorComparisonRunTrial, KeyboardKey.Enter),
        new(GameInputAction.VendorComparisonClose, KeyboardKey.Escape),
        new(GameInputAction.TogglePlacement, KeyboardKey.M),
        new(GameInputAction.PlacementPrevious, KeyboardKey.Q), new(GameInputAction.PlacementNext, KeyboardKey.E),
        new(GameInputAction.PlacementLeft, KeyboardKey.Left), new(GameInputAction.PlacementRight, KeyboardKey.Right),
        new(GameInputAction.PlacementUp, KeyboardKey.Up), new(GameInputAction.PlacementDown, KeyboardKey.Down),
        new(GameInputAction.PlacementConfirm, KeyboardKey.Enter), new(GameInputAction.PlacementUndo, KeyboardKey.Backspace),
        new(GameInputAction.PlacementReset, KeyboardKey.H),
        new(GameInputAction.CameraPanLeft, KeyboardKey.Left), new(GameInputAction.CameraPanRight, KeyboardKey.Right),
        new(GameInputAction.CameraPanUp, KeyboardKey.Up), new(GameInputAction.CameraPanDown, KeyboardKey.Down),
        new(GameInputAction.CameraZoomIn, KeyboardKey.Z), new(GameInputAction.CameraZoomOut, KeyboardKey.X),
        new(GameInputAction.CameraReset, KeyboardKey.C), new(GameInputAction.CameraReset, KeyboardKey.Home),
        new(GameInputAction.Exit, KeyboardKey.Escape),
        new(GameInputAction.MoveAway, KeyboardKey.W), new(GameInputAction.MoveLeft, KeyboardKey.A),
        new(GameInputAction.MoveToward, KeyboardKey.S), new(GameInputAction.MoveRight, KeyboardKey.D),
        new(GameInputAction.DeveloperToggle, KeyboardKey.F1), new(GameInputAction.DeveloperAddDirty, KeyboardKey.F2),
        new(GameInputAction.DeveloperSetCleanSupply, KeyboardKey.F3), new(GameInputAction.DeveloperReset, KeyboardKey.F4),
        new(GameInputAction.DeveloperTogglePause, KeyboardKey.F5), new(GameInputAction.DeveloperStep, KeyboardKey.F6),
        new(GameInputAction.DeveloperStickyReady, KeyboardKey.F7), new(GameInputAction.DeveloperToggleLayout, KeyboardKey.F8),
        new(GameInputAction.DeveloperToggleBenchmark, KeyboardKey.F9), new(GameInputAction.DeveloperQuickSave, KeyboardKey.F10),
        new(GameInputAction.DeveloperQuickLoad, KeyboardKey.F11),
    ]);

    public int SchemaVersion { get; }
    public KeyboardBinding[] Bindings => bindings.ToArray();

    [JsonConstructor]
    public InputBindingProfile(int schemaVersion, KeyboardBinding[] bindings)
    {
        if (schemaVersion is not (2 or 3 or 4 or 5 or 6 or 7 or CurrentSchemaVersion))
            throw new ArgumentOutOfRangeException(nameof(schemaVersion), $"Unsupported input binding schema {schemaVersion}.");
        ArgumentNullException.ThrowIfNull(bindings);
        SchemaVersion = CurrentSchemaVersion;
        var twoStationAdditions = new KeyboardBinding[]
        {
            new(GameInputAction.TwoStationRoutingToggle, KeyboardKey.Digit7),
            new(GameInputAction.TwoStationRoutingPreviousStation, KeyboardKey.Left),
            new(GameInputAction.TwoStationRoutingNextStation, KeyboardKey.Right),
            new(GameInputAction.TwoStationRoutingPreviousPolicy, KeyboardKey.Up),
            new(GameInputAction.TwoStationRoutingNextPolicy, KeyboardKey.Down),
            new(GameInputAction.TwoStationRoutingCopy, KeyboardKey.C),
            new(GameInputAction.TwoStationRoutingRunTrial, KeyboardKey.Enter),
            new(GameInputAction.TwoStationRoutingClose, KeyboardKey.Escape),
        };
        var codexAdditions = new KeyboardBinding[]
        {
            new(GameInputAction.PatternCodexToggle, KeyboardKey.Digit8),
            new(GameInputAction.PatternCodexClose, KeyboardKey.Escape),
        };
        var namingAdditions = new KeyboardBinding[]
        {
            new(GameInputAction.PatternCodexReflect, KeyboardKey.Enter),
        };
        var vendorAdditions = new KeyboardBinding[]
        {
            new(GameInputAction.VendorComparisonToggle, KeyboardKey.Digit9),
            new(GameInputAction.VendorComparisonPrevious, KeyboardKey.Left),
            new(GameInputAction.VendorComparisonNext, KeyboardKey.Right),
            new(GameInputAction.VendorComparisonRunTrial, KeyboardKey.Enter),
            new(GameInputAction.VendorComparisonClose, KeyboardKey.Escape),
        };
        var additions = schemaVersion switch
        {
            2 => new KeyboardBinding[]
            {
                new(GameInputAction.AutomationEditorToggle, KeyboardKey.Digit6),
                new(GameInputAction.AutomationEditorPrevious, KeyboardKey.Up),
                new(GameInputAction.AutomationEditorNext, KeyboardKey.Down),
                new(GameInputAction.AutomationEditorToggleValue, KeyboardKey.Space),
                new(GameInputAction.AutomationEditorApply, KeyboardKey.Enter),
                new(GameInputAction.AutomationEditorClose, KeyboardKey.Escape),
                new(GameInputAction.AutomationEditorSaveBaseline, KeyboardKey.B),
                new(GameInputAction.AutomationEditorSaveVariant, KeyboardKey.V),
                new(GameInputAction.AutomationEditorRunComparison, KeyboardKey.R),
            }.Concat(twoStationAdditions).Concat(codexAdditions).Concat(namingAdditions).Concat(vendorAdditions).ToArray(),
            3 =>
            [
                new(GameInputAction.AutomationEditorSaveBaseline, KeyboardKey.B),
                new(GameInputAction.AutomationEditorSaveVariant, KeyboardKey.V),
                new(GameInputAction.AutomationEditorRunComparison, KeyboardKey.R),
                .. twoStationAdditions,
                .. codexAdditions,
                .. namingAdditions,
                .. vendorAdditions,
            ],
            4 => twoStationAdditions.Concat(codexAdditions).Concat(namingAdditions).Concat(vendorAdditions).ToArray(),
            5 => codexAdditions.Concat(namingAdditions).Concat(vendorAdditions).ToArray(),
            6 => namingAdditions.Concat(vendorAdditions).ToArray(),
            7 => vendorAdditions,
            _ => [],
        };
        var missingAdditions = additions.Where(addition =>
            !bindings.Any(binding => binding.Action == addition.Action));
        this.bindings = bindings.Concat(missingAdditions).ToArray();
        keysByAction = new KeyboardKey[Enum.GetValues<GameInputAction>().Length][];
        displayNames = new string[keysByAction.Length];
        primaryDisplayNames = new string[keysByAction.Length];
        var grouped = new List<KeyboardKey>[keysByAction.Length];
        foreach (var binding in this.bindings)
        {
            if (!Enum.IsDefined(binding.Action) || !Enum.IsDefined(binding.Key))
                throw new ArgumentOutOfRangeException(nameof(bindings), "Input binding contains an unknown action or key.");
            var index = (int)binding.Action;
            grouped[index] ??= [];
            if (grouped[index]!.Contains(binding.Key))
                throw new ArgumentException($"Duplicate binding {binding.Key} for {binding.Action}.", nameof(bindings));
            grouped[index]!.Add(binding.Key);
        }
        for (var index = 0; index < keysByAction.Length; index++)
        {
            if (grouped[index] is not { Count: > 0 })
                throw new ArgumentException($"Input action {(GameInputAction)index} has no binding.", nameof(bindings));
            keysByAction[index] = grouped[index]!.ToArray();
            displayNames[index] = string.Join(" / ", keysByAction[index].Select(DisplayName));
            primaryDisplayNames[index] = DisplayName(keysByAction[index][0]);
        }
    }

    public ReadOnlySpan<KeyboardKey> KeysFor(GameInputAction action)
    {
        if (!Enum.IsDefined(action)) throw new ArgumentOutOfRangeException(nameof(action));
        return keysByAction[(int)action];
    }

    public bool Matches(GameInputAction action, KeyboardKey key, bool developerActionsAvailable = true)
    {
        if ((InputActionCatalog.ContextOf(action) & InputActionContext.Developer) != 0 && !developerActionsAvailable) return false;
        return KeysFor(action).Contains(key);
    }

    public string DisplayName(GameInputAction action) => displayNames[(int)action];

    public string PrimaryDisplayName(GameInputAction action) => primaryDisplayNames[(int)action];

    public InputBindingProfile WithBinding(GameInputAction action, params KeyboardKey[] keys)
    {
        ArgumentNullException.ThrowIfNull(keys);
        if (keys.Length == 0) throw new ArgumentException("At least one key is required.", nameof(keys));
        var replacement = bindings.Where(binding => binding.Action != action)
            .Concat(keys.Select(key => new KeyboardBinding(action, key)))
            .ToArray();
        return new(CurrentSchemaVersion, replacement);
    }

    private static string DisplayName(KeyboardKey key) => key switch
    {
        KeyboardKey.Digit1 => "1", KeyboardKey.Digit2 => "2", KeyboardKey.Digit3 => "3",
        KeyboardKey.Digit4 => "4", KeyboardKey.Digit5 => "5", KeyboardKey.Digit6 => "6", KeyboardKey.Digit7 => "7", KeyboardKey.Digit8 => "8", KeyboardKey.Digit9 => "9", KeyboardKey.Backspace => "BACKSPACE",
        _ => key.ToString().ToUpperInvariant(),
    };
}

public static class InputActionCatalog
{
    public static InputActionContext ContextOf(GameInputAction action) => action switch
    {
        GameInputAction.MenuPrevious or GameInputAction.MenuNext or GameInputAction.MenuConfirm or GameInputAction.MenuBack => InputActionContext.Menu,
        GameInputAction.IntroPrevious or GameInputAction.IntroNext or GameInputAction.IntroConfirm or GameInputAction.IntroExit => InputActionContext.Intro,
        GameInputAction.HelpClose => InputActionContext.Gameplay | InputActionContext.Help,
        GameInputAction.JournalToggle => InputActionContext.Gameplay | InputActionContext.Journal,
        GameInputAction.JournalBack or GameInputAction.JournalPrevious or GameInputAction.JournalNext or GameInputAction.JournalDetail => InputActionContext.Journal,
        GameInputAction.ShiftReportToggle or GameInputAction.ShiftReportClose => InputActionContext.ShiftReport,
        GameInputAction.SettingsToggle => InputActionContext.Menu | InputActionContext.Gameplay | InputActionContext.Settings,
        GameInputAction.SettingsPrevious or GameInputAction.SettingsNext or GameInputAction.SettingsDecrease or
        GameInputAction.SettingsIncrease or GameInputAction.SettingsConfirm or GameInputAction.SettingsReset or
            GameInputAction.SettingsClose => InputActionContext.Settings,
        GameInputAction.ProcessEditorPrevious or GameInputAction.ProcessEditorNext or GameInputAction.ProcessEditorMoveUp or
            GameInputAction.ProcessEditorMoveDown or GameInputAction.ProcessEditorToggleAssignment or
            GameInputAction.ProcessEditorNextRouting or GameInputAction.ProcessEditorApply or GameInputAction.ProcessEditorClose => InputActionContext.ProcessEditor,
        GameInputAction.ProcessEditorToggle or GameInputAction.ProcessCaptureToggle => InputActionContext.Gameplay,
        GameInputAction.AutomationEditorPrevious or GameInputAction.AutomationEditorNext or
            GameInputAction.AutomationEditorToggleValue or GameInputAction.AutomationEditorApply or
            GameInputAction.AutomationEditorClose or GameInputAction.AutomationEditorSaveBaseline or
            GameInputAction.AutomationEditorSaveVariant or GameInputAction.AutomationEditorRunComparison => InputActionContext.AutomationEditor,
        GameInputAction.AutomationEditorToggle => InputActionContext.Gameplay,
        GameInputAction.TwoStationRoutingPreviousStation or GameInputAction.TwoStationRoutingNextStation or
            GameInputAction.TwoStationRoutingPreviousPolicy or GameInputAction.TwoStationRoutingNextPolicy or
            GameInputAction.TwoStationRoutingCopy or GameInputAction.TwoStationRoutingRunTrial or
            GameInputAction.TwoStationRoutingClose => InputActionContext.TwoStationRouting,
        GameInputAction.TwoStationRoutingToggle => InputActionContext.Gameplay,
        GameInputAction.PatternCodexReflect or GameInputAction.PatternCodexClose => InputActionContext.PatternCodex,
        GameInputAction.PatternCodexToggle => InputActionContext.Gameplay,
        GameInputAction.VendorComparisonPrevious or GameInputAction.VendorComparisonNext or
            GameInputAction.VendorComparisonRunTrial or GameInputAction.VendorComparisonClose => InputActionContext.VendorComparison,
        GameInputAction.VendorComparisonToggle => InputActionContext.Gameplay,
        GameInputAction.PlacementPrevious or GameInputAction.PlacementNext or GameInputAction.PlacementLeft or GameInputAction.PlacementRight or
            GameInputAction.PlacementUp or GameInputAction.PlacementDown or GameInputAction.PlacementConfirm or GameInputAction.PlacementUndo or
            GameInputAction.PlacementReset => InputActionContext.Placement,
        >= GameInputAction.DeveloperToggle and <= GameInputAction.DeveloperQuickLoad => InputActionContext.Developer,
        _ when Enum.IsDefined(action) => InputActionContext.Gameplay,
        _ => throw new ArgumentOutOfRangeException(nameof(action)),
    };
}
