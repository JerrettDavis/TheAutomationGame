using Automation.Client.Stride;

var configuredSettingsPath = Environment.GetEnvironmentVariable("AUTOMATION_SETTINGS_PATH");
var settingsPath = string.IsNullOrWhiteSpace(configuredSettingsPath) ? ClientSettingsStore.DefaultPath : configuredSettingsPath;
var settings = ClientSettingsStore.LoadFileOrDefault(settingsPath, out var settingsFallback);
var forceWindowed = args.Contains("--windowed", StringComparer.OrdinalIgnoreCase) ||
                    Environment.GetEnvironmentVariable("AUTOMATION_WINDOWED") == "1";
var forceFullscreen = args.Contains("--fullscreen", StringComparer.OrdinalIgnoreCase);
var windowMode = ClientStartupSettings.ResolveWindowMode(settings, forceWindowed, forceFullscreen);
var windowed = windowMode == ClientWindowMode.Windowed;
var leftmostWorkArea = FullscreenWindow.LeftmostWorkArea;
if (args.Contains("--diagnose-assets", StringComparer.OrdinalIgnoreCase))
{
    var catalog = PresentationCatalog.Default;
    var washer = catalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
    var player = catalog.Resolve(PresentationIds.Player, PresentationIds.FallbackActor);
    var worker = catalog.Resolve(PresentationIds.NewHire, PresentationIds.FallbackActor);
    var item = catalog.Resolve(PresentationIds.Plate, PresentationIds.FallbackItem);
    var roomPlan = DishRoomModulePlan.Create(Automation.Domain.DishStationPlacements.Linear);
    var topology = new Automation.Domain.DishStationTopology(Automation.Domain.DishStationPlacements.Linear);
    var obstacleDetour = topology.FindPath(new Automation.Domain.FloorCell(6, 1), new Automation.Domain.FloorCell(8, 1));
    var characterWorld = new Automation.Simulation.DishStationWorld(42, Automation.Content.DishStationFirstHoursContent.ScenarioConfiguration);
    var characterPresenter = new DishStationCharacterPresenter();
    var idleCharacter = characterPresenter.Update(characterWorld.Snapshot(), 0, reducedMotion: false);
    characterWorld.ExecuteNow(new Automation.Simulation.MovePlayerCommand(characterWorld.Tick, new Automation.Domain.FloorCell(2, 3)));
    var walkingCharacter = characterPresenter.Update(characterWorld.Snapshot(), 0.05f, reducedMotion: false);
    var workPresenter = new DishStationCharacterPresenter();
    workPresenter.Update(characterWorld.Snapshot(), 0, reducedMotion: false);
    workPresenter.NotifyPlayerWork();
    var workingCharacter = workPresenter.Update(characterWorld.Snapshot(), 0.01f, reducedMotion: false);
    var bundleDirectory = Path.Combine(AppContext.BaseDirectory, "data", "db", "bundles");
    var bundles = Directory.Exists(bundleDirectory)
        ? Directory.GetFiles(bundleDirectory, "*.bundle", SearchOption.TopDirectoryOnly)
        : [];
    var bundleCatalogText = string.Concat(bundles.Select(path => System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(path))));
    var audioAssets = Enum.GetValues<AudioCue>().Count(cue => bundleCatalogText.Contains(AudioCueCatalog.ContentUrl(cue), StringComparison.Ordinal));
    Console.WriteLine($"washer={washer.Id} model={washer.ModelContentUrl} projection={WasherAssetPresentation.HasEmbeddedProjection()} " +
                      $"player={player.Id} worker={worker.Id} item={item.Id} fallback={washer.Fallback} " +
                      $"characterStates={idleCharacter.Player.Animation},{walkingCharacter.Player.Animation},{workingCharacter.Player.Animation} " +
                      $"audioAssets={audioAssets}/{Enum.GetValues<AudioCue>().Length} audioPlayback=not-started " +
                      $"roomModules={roomPlan.Modules.Count} roomKinds={roomPlan.Modules.Select(module => module.Kind).Distinct().Count()} " +
                      $"blockedFixtures=6 portsConnected={topology.AllInteractionPortsConnected()} detourSteps={obstacleDetour.Length - 1} " +
                      $"bundles={bundles.Length} bytes={bundles.Sum(path => new FileInfo(path).Length)} " +
                      $"leftmost={leftmostWorkArea.X},{leftmostWorkArea.Y},{leftmostWorkArea.Width}x{leftmostWorkArea.Height} gui=not-started");
    return;
}
if (args.Contains("--diagnose-startup", StringComparer.OrdinalIgnoreCase))
{
    Console.WriteLine($"settingsSchema={settings.SchemaVersion} fallback={settingsFallback} mode={windowMode} " +
                      $"ui={settings.UiScalePercent} camera={settings.CameraSensitivityPercent} volume={settings.MasterVolumePercent} " +
                      $"bindings={settings.InputBindings.SchemaVersion} leftmost={leftmostWorkArea.X},{leftmostWorkArea.Y}," +
                      $"{leftmostWorkArea.Width}x{leftmostWorkArea.Height}");
    return;
}
var displaySize = windowed ? (Width: 1280, Height: 720) : (leftmostWorkArea.Width, leftmostWorkArea.Height);
_ = windowed
    ? FullscreenWindow.ApplyWindowedToLeftmostWhenReadyAsync(leftmostWorkArea, displaySize.Width, displaySize.Height)
    : FullscreenWindow.ApplyBorderlessToLeftmostWhenReadyAsync(leftmostWorkArea);
using var game = new DishStationGame(!windowed, displaySize.Width, displaySize.Height,
    clientSettings: settings, clientSettingsPath: settingsPath);
game.Run();
