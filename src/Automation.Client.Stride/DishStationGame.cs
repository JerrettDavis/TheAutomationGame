using Automation.Content;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;
using Automation.Tools;
using System.Text.Json;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.Input;

namespace Automation.Client.Stride;

public sealed class DishStationGame : Game
{
    private const float VirtualWidth = 1024;
    private const float VirtualHeight = 600;
    private static readonly DishStationEpisodeDefinition Episode = DishStationEpisodeDefinition.FirstPlayable;
    private static readonly WorkstationPresentation[] Workstations =
    [
        new("SCRAPE", DishAction.Scrape, DishState.Dirty, new RectangleF(40, 165, 175, 120), new Color(89, 70, 55)),
        new("RACK", DishAction.Rack, DishState.Scraped, new RectangleF(245, 165, 175, 120), new Color(55, 78, 93)),
        new("WASHER", DishAction.StartWasher, DishState.Racked, new RectangleF(450, 165, 175, 120), new Color(47, 77, 102)),
        new("UNLOAD", DishAction.Unload, DishState.WashedInMachine, new RectangleF(655, 165, 175, 120), new Color(47, 91, 91)),
        new("DRY + RESTOCK", DishAction.DryAndRestock, DishState.CleanWet, new RectangleF(805, 330, 175, 120), new Color(60, 92, 65)),
    ];
    private static readonly DishState[] DishStates = Enum.GetValues<DishState>();
    private static readonly SystemLens[] SystemLenses = Enum.GetValues<SystemLens>();
    private InputBindingProfile inputBindings;
    private ClientSettings clientSettings;
    private readonly string clientSettingsPath;
    private readonly ClientScreenRouter screenRouter = new();
    private readonly PresentationCatalog presentationCatalog;

    private DishStationWorld world = new(42, DishStationFirstHoursContent.ScenarioConfiguration);
    private TwoStationRoutingWorld twoStationWorld = new(42, DishStationTwoStationsContent.Configuration);
    private VendorOutsourcingWorld vendorWorld = new(DishStationVendorContent.Configuration);
    private PatternKnowledgeProfile patternKnowledge = PatternKnowledgeProfile.Empty;
    private double simulationAccumulator;
    private DishKind selectedKind = DishKind.Plate;
    private int selectedWorkstation;
    private DishStationFixture? selectedInteractionFixture = DishStationFixture.Scrape;
    private bool godMode;
    private bool benchmarkVisible;
    private SyntheticWorkResult? benchmarkResult;
    private string? quickSaveJson;
    private bool paused;
    private SystemLens activeLens = SystemLens.Process;
    private readonly string? driverControlFile = Environment.GetEnvironmentVariable("AUTOMATION_UI_CONTROL_FILE");
    private readonly string? screenshotRequestFile = Environment.GetEnvironmentVariable("AUTOMATION_SCREENSHOT_REQUEST_FILE");
    private readonly bool developerToolsOptIn = string.Equals(Environment.GetEnvironmentVariable("AUTOMATION_DEVELOPER_TOOLS"), "1", StringComparison.Ordinal);
    private readonly bool diagnosticTitleOptIn = string.Equals(Environment.GetEnvironmentVariable("AUTOMATION_DIAGNOSTIC_TITLE"), "1", StringComparison.Ordinal);
    private readonly bool fullscreenPresentation;
    private readonly int requestedWidth;
    private readonly int requestedHeight;
    private long driverControlSequence;
    private long screenshotRequestSequence;
    private long pendingScreenshotSequence;
    private string? pendingScreenshotPath;
    private double driverPollAccumulator;
    private string commandFeedback = "";
    private SpriteBatch? spriteBatch;
    private Texture? pixel;
    private Texture? diamond;
    private Texture? washerProjection;
    private DishRoomNativeScene? nativeRoom;
    private string roomPresentationStatus = "pending";
    private IsometricCamera camera = IsometricCamera.Default;
    private bool placementMode;
    private DishStationFixture placementFixture;
    private FloorCell placementPreview = DishStationPlacements.Linear.Scrape;
    private readonly Stack<PlacementUndo> placementUndo = new();
    private readonly DishStationCharacterPresenter characterPresenter = new();
    private DishStationCharacterFrame characterFrame;
    private readonly DishStationAudioRouter audioRouter = new();
    private DishStationAudioPresenter? audioPresenter;
    private string audioStatus = "pending";
    private string audioCaption = "";
    private float audioCaptionSeconds;
    private readonly CharacterDialogueRouter dialogueRouter = new(DishStationFirstHoursContent.Catalog);
    private int observedNarrativeEvents;
    private CharacterBarkPresentation? activeCharacterBark;
    private float characterBarkSeconds;
    private float canvasScale = 1;
    private float canvasOffsetX;
    private float canvasOffsetY;
    private float uiCanvasScale = 1;
    private float uiCanvasOffsetX;
    private float uiCanvasOffsetY;
    private DishStationFixture? hoveredFixture;
    private float interactionTime;
    private DirectMovementInput activeMovementInput;
    private double movementRepeatRemaining;
    private readonly GameplayClickRoute clickMovement = new();
    private double clickMovementRepeatRemaining;
    private readonly Dictionary<string, Texture> interactionIcons = new(StringComparer.Ordinal);
    private string lastPointerAction = "NONE";
    private int introPage;
    private GuidanceMode selectedGuidance = GuidanceMode.Guided;
    private bool selectedReducedMotion;
    private bool selectedHighContrast;
    private readonly Dictionary<DishTutorialStage, HandbookVisitEvidence> handbookVisits = new();
    private int selectedJournalQuest;
    private int observedLevel = 1;
    private DishStationQuestId? observedActiveQuest = DishStationQuestId.ClockIn;
    private DishStationQuestId? progressionReceiptQuest;
    private int progressionReceiptLevel = 1;
    private bool progressionReceiptLeveledUp;
    private float progressionReceiptSeconds;
    private readonly string careerSavePath = Environment.GetEnvironmentVariable("AUTOMATION_SAVE_PATH")
        ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TheAutomationGame", "career.json");
    private readonly bool careerSaveEnabled = !string.Equals(Environment.GetEnvironmentVariable("AUTOMATION_DISABLE_CAREER_SAVE"), "1", StringComparison.Ordinal);
    private int startMenuSelection;
    private int settingsSelection;
    private int selectedProcessStep;
    private int selectedAutomationRuleRow;
    private int selectedRoutingStation;
    private string settingsStatus = "READY";
    private double autosaveAccumulator;
    private long lastAutosaveTick = -1;
    private string saveStatus = "NEW";
    private bool renderHighContrast;
    private bool renderReducedMotion;
    private readonly string? playtestEvidencePath = Environment.GetEnvironmentVariable("AUTOMATION_PLAYTEST_EVIDENCE_PATH");
    private readonly string playtestSessionId = Environment.GetEnvironmentVariable("AUTOMATION_PLAYTEST_SESSION_ID") ?? Guid.NewGuid().ToString("N");
    private readonly DateTimeOffset playtestStartedAtUtc = DateTimeOffset.UtcNow;
    private bool playtestEvidenceAttempted;
    private string playtestEvidenceStatus = "off";
    private bool DeveloperToolsAvailable => !string.IsNullOrWhiteSpace(driverControlFile) || developerToolsOptIn ||
        world.TutorialStage == DishTutorialStage.EpisodeComplete;
    private bool StartMenuVisible => screenRouter.Screen == ClientScreen.StartMenu;
    private bool BriefingVisible => screenRouter.Screen == ClientScreen.Briefing;
    private bool QuestJournalVisible => screenRouter.JournalVisible;
    private bool QuestDetailVisible => screenRouter.Modal == ClientModal.QuestDetail;
    private bool HelpVisible => screenRouter.Modal == ClientModal.Help;
    private bool ShiftReportVisible => screenRouter.Modal == ClientModal.ShiftReport;
    private bool SettingsVisible => screenRouter.Modal == ClientModal.Settings;
    private bool NewCareerConfirmationVisible => screenRouter.Modal == ClientModal.NewCareerConfirmation;
    private bool ProcessEditorVisible => screenRouter.Modal == ClientModal.ProcessEditor;
    private bool AutomationEditorVisible => screenRouter.Modal == ClientModal.AutomationEditor;
    private bool TwoStationRoutingVisible => screenRouter.Modal == ClientModal.TwoStationRouting;
    private bool PatternCodexVisible => screenRouter.Modal == ClientModal.PatternCodex;
    private bool VendorComparisonVisible => screenRouter.Modal == ClientModal.VendorComparison;

    public DishStationGame(
        bool fullscreenPresentation = false,
        int requestedWidth = 1280,
        int requestedHeight = 720,
        InputBindingProfile? inputBindings = null,
        ClientSettings? clientSettings = null,
        string? clientSettingsPath = null,
        PresentationCatalog? presentationCatalog = null)
    {
        this.clientSettings = clientSettings ?? ClientSettings.Default;
        this.inputBindings = inputBindings ?? this.clientSettings.InputBindings;
        this.clientSettingsPath = clientSettingsPath ?? ClientSettingsStore.DefaultPath;
        this.presentationCatalog = presentationCatalog ?? PresentationCatalog.Default;
        this.fullscreenPresentation = fullscreenPresentation;
        this.requestedWidth = requestedWidth;
        this.requestedHeight = requestedHeight;
        GraphicsDeviceManager.PreferredBackBufferWidth = requestedWidth;
        GraphicsDeviceManager.PreferredBackBufferHeight = requestedHeight;
    }

    protected override void BeginRun()
    {
        base.BeginRun();
        Window.AllowUserResizing = true;
        Window.IsBorderLess = fullscreenPresentation;
        Window.SetSize(new Int2(requestedWidth, requestedHeight));
        DebugTextSystem.Visible = true;
        spriteBatch = new SpriteBatch(GraphicsDevice);
        pixel = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource);
        pixel.SetData(GraphicsContext.CommandList, [Color.White]);
        diamond = IsometricStationScene.CreateDiamondTexture(GraphicsDevice, GraphicsContext);
        var washerPresentation = presentationCatalog.Resolve(PresentationIds.Washer, PresentationIds.FallbackWorkstation);
        washerProjection = WasherAssetPresentation.TryLoadProjection(GraphicsDevice, washerPresentation.ProjectionResourceSuffix);
        nativeRoom = DishRoomNativeScene.TryCreate(this, world.Placements, presentationCatalog, out roomPresentationStatus);
        audioPresenter = DishStationAudioPresenter.TryCreate(Content, clientSettings.MasterVolumePercent, out audioStatus);
        audioRouter.Initialize(world.Snapshot(), world.Notifications.Count);
        InitializeDialogue(world.Snapshot());
        audioRouter.Start(EmitAudio);
        foreach (var name in new[] { "pointer_a", "hand_point", "look_a", "disabled", "target_round_a" })
            interactionIcons[name] = LoadInteractionIcon(name);
        screenRouter.Initialize(careerSaveEnabled && File.Exists(careerSavePath));
        saveStatus = StartMenuVisible ? "FOUND" : careerSaveEnabled ? "NEW" : "OFF";
        playtestEvidenceStatus = string.IsNullOrWhiteSpace(playtestEvidencePath) ? "off" : "pending";
        UpdateWindowTitle();
    }

    protected override void Update(GameTime gameTime)
    {
        interactionTime += (float)gameTime.Elapsed.TotalSeconds;
        audioCaptionSeconds = Math.Max(0, audioCaptionSeconds - (float)gameTime.Elapsed.TotalSeconds);
        characterBarkSeconds = Math.Max(0, characterBarkSeconds - (float)gameTime.Elapsed.TotalSeconds);
        hoveredFixture = screenRouter.Screen == ClientScreen.Gameplay && screenRouter.Modal == ClientModal.None && activeLens is SystemLens.Reality or SystemLens.Process
            ? IsometricStationScene.HitTest(VirtualMousePosition().X, VirtualMousePosition().Y, world.Placements, camera,
                presentationCatalog, washerProjection is not null)
            : null;
        var characterSnapshot = world.Snapshot();
        var characterReducedMotion = characterSnapshot.Onboarding.ReducedMotion ||
                                     (!characterSnapshot.Onboarding.Complete && selectedReducedMotion);
        characterFrame = characterPresenter.Update(characterSnapshot, (float)gameTime.Elapsed.TotalSeconds, characterReducedMotion);

        simulationAccumulator += gameTime.Elapsed.TotalSeconds;
        while (simulationAccumulator >= 0.1)
        {
            if (ClientSimulationPolicy.ShouldAdvance(paused, screenRouter.Screen, screenRouter.Modal)) world.Advance();
            simulationAccumulator -= 0.1;
            if (world.Tick.Value % 10 == 0) UpdateWindowTitle();
        }
        audioRouter.Observe(world.Snapshot(), world.Notifications, EmitAudio);
        ObserveDialogue(world.Snapshot());

        progressionReceiptSeconds = Math.Max(0, progressionReceiptSeconds - (float)gameTime.Elapsed.TotalSeconds);
        ObserveProgression();

        if (SettingsVisible)
        {
            HandleSettingsPointer();
            if (Pressed(GameInputAction.SettingsPrevious)) HandleControl(ClientControl.SettingsPrevious);
            if (Pressed(GameInputAction.SettingsNext)) HandleControl(ClientControl.SettingsNext);
            if (Pressed(GameInputAction.SettingsDecrease)) HandleControl(ClientControl.SettingsDecrease);
            if (Pressed(GameInputAction.SettingsIncrease)) HandleControl(ClientControl.SettingsIncrease);
            if (Pressed(GameInputAction.SettingsConfirm)) HandleControl(ClientControl.SettingsConfirm);
            if (Pressed(GameInputAction.SettingsReset)) HandleControl(ClientControl.SettingsReset);
            if (Pressed(GameInputAction.SettingsClose)) HandleControl(ClientControl.ToggleSettings);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (StartMenuVisible)
        {
            HandleStartMenuPointer();
            if (Pressed(GameInputAction.MenuPrevious)) HandleControl(ClientControl.MenuPrevious);
            if (Pressed(GameInputAction.MenuNext)) HandleControl(ClientControl.MenuNext);
            if (Pressed(GameInputAction.MenuConfirm)) HandleControl(ClientControl.MenuConfirm);
            if (Pressed(GameInputAction.MenuBack)) HandleControl(NewCareerConfirmationVisible ? ClientControl.MenuBack : ClientControl.Exit);
            if (!NewCareerConfirmationVisible && Pressed(GameInputAction.SettingsToggle)) HandleControl(ClientControl.ToggleSettings);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (BriefingVisible)
        {
            HandleIntroPointer();
            if (Pressed(GameInputAction.IntroPrevious)) HandleControl(ClientControl.PreviousGuidance);
            if (Pressed(GameInputAction.IntroNext)) HandleControl(ClientControl.NextGuidance);
            if (Pressed(GameInputAction.IntroConfirm)) HandleControl(ClientControl.IntroNext);
            if (Pressed(GameInputAction.IntroExit)) HandleControl(ClientControl.Exit);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (HelpVisible)
        {
            if (LeftClickIn(802, 74, 116, 38)) HandleControl(ClientControl.ToggleHelp);
            if (Pressed(GameInputAction.HelpClose)) HandleControl(ClientControl.ToggleHelp);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (QuestJournalVisible)
        {
            HandleJournalPointer();
            if (Pressed(GameInputAction.JournalToggle)) HandleControl(ClientControl.ToggleQuestJournal);
            if (Pressed(GameInputAction.JournalBack)) HandleControl(ClientControl.JournalBack);
            if (Pressed(GameInputAction.JournalPrevious)) HandleControl(ClientControl.JournalPrevious);
            if (Pressed(GameInputAction.JournalNext)) HandleControl(ClientControl.JournalNext);
            if (Pressed(GameInputAction.JournalDetail)) HandleControl(ClientControl.ToggleQuestDetail);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (ShiftReportVisible)
        {
            if (LeftClickIn(784, 526, 148, 28)) HandleControl(ClientControl.ToggleShiftReport);
            if (Pressed(GameInputAction.ShiftReportClose)) HandleControl(ClientControl.ToggleShiftReport);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (ProcessEditorVisible)
        {
            if (Pressed(GameInputAction.ProcessEditorPrevious)) HandleControl(ClientControl.ProcessEditorPrevious);
            if (Pressed(GameInputAction.ProcessEditorNext)) HandleControl(ClientControl.ProcessEditorNext);
            if (Pressed(GameInputAction.ProcessEditorMoveUp)) HandleControl(ClientControl.ProcessEditorMoveUp);
            if (Pressed(GameInputAction.ProcessEditorMoveDown)) HandleControl(ClientControl.ProcessEditorMoveDown);
            if (Pressed(GameInputAction.ProcessEditorToggleAssignment)) HandleControl(ClientControl.ProcessEditorToggleAssignment);
            if (Pressed(GameInputAction.ProcessEditorNextRouting)) HandleControl(ClientControl.ProcessEditorNextRouting);
            if (Pressed(GameInputAction.ProcessEditorApply)) HandleControl(ClientControl.ProcessEditorApply);
            if (Pressed(GameInputAction.ProcessEditorClose)) HandleControl(ClientControl.ProcessEditorClose);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (AutomationEditorVisible)
        {
            if (Pressed(GameInputAction.AutomationEditorPrevious)) HandleControl(ClientControl.AutomationEditorPrevious);
            if (Pressed(GameInputAction.AutomationEditorNext)) HandleControl(ClientControl.AutomationEditorNext);
            if (Pressed(GameInputAction.AutomationEditorToggleValue)) HandleControl(ClientControl.AutomationEditorToggleValue);
            if (Pressed(GameInputAction.AutomationEditorApply)) HandleControl(ClientControl.AutomationEditorApply);
            if (Pressed(GameInputAction.AutomationEditorSaveBaseline)) HandleControl(ClientControl.AutomationEditorSaveBaseline);
            if (Pressed(GameInputAction.AutomationEditorSaveVariant)) HandleControl(ClientControl.AutomationEditorSaveVariant);
            if (Pressed(GameInputAction.AutomationEditorRunComparison)) HandleControl(ClientControl.AutomationEditorRunComparison);
            if (Pressed(GameInputAction.AutomationEditorClose)) HandleControl(ClientControl.AutomationEditorClose);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (TwoStationRoutingVisible)
        {
            if (Pressed(GameInputAction.TwoStationRoutingPreviousStation)) HandleControl(ClientControl.TwoStationRoutingPreviousStation);
            if (Pressed(GameInputAction.TwoStationRoutingNextStation)) HandleControl(ClientControl.TwoStationRoutingNextStation);
            if (Pressed(GameInputAction.TwoStationRoutingPreviousPolicy)) HandleControl(ClientControl.TwoStationRoutingPreviousPolicy);
            if (Pressed(GameInputAction.TwoStationRoutingNextPolicy)) HandleControl(ClientControl.TwoStationRoutingNextPolicy);
            if (Pressed(GameInputAction.TwoStationRoutingCopy)) HandleControl(ClientControl.TwoStationRoutingCopy);
            if (Pressed(GameInputAction.TwoStationRoutingRunTrial)) HandleControl(ClientControl.TwoStationRoutingRunTrial);
            if (Pressed(GameInputAction.TwoStationRoutingClose)) HandleControl(ClientControl.TwoStationRoutingClose);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (PatternCodexVisible)
        {
            if (Pressed(GameInputAction.PatternCodexReflect)) HandleControl(ClientControl.PatternCodexReflect);
            if (Pressed(GameInputAction.PatternCodexClose)) HandleControl(ClientControl.PatternCodexClose);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (VendorComparisonVisible)
        {
            if (Pressed(GameInputAction.VendorComparisonPrevious)) HandleControl(ClientControl.VendorComparisonPrevious);
            if (Pressed(GameInputAction.VendorComparisonNext)) HandleControl(ClientControl.VendorComparisonNext);
            if (Pressed(GameInputAction.VendorComparisonRunTrial)) HandleControl(ClientControl.VendorComparisonRunTrial);
            if (Pressed(GameInputAction.VendorComparisonClose)) HandleControl(ClientControl.VendorComparisonClose);
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }

        if (careerSaveEnabled && world.IntroComplete)
        {
            autosaveAccumulator += gameTime.Elapsed.TotalSeconds;
            if (autosaveAccumulator >= 5 && lastAutosaveTick != world.Tick.Value)
            {
                autosaveAccumulator = 0;
                SaveCareer();
            }
        }

        if (Pressed(placementMode ? GameInputAction.PlacementPrevious : GameInputAction.PreviousTarget))
            HandleControl(placementMode ? ClientControl.PreviousPlacementFixture : ClientControl.PreviousWorkstation);
        if (placementMode)
        {
            if (Pressed(GameInputAction.PlacementNext)) HandleControl(ClientControl.NextPlacementFixture);
        }
        else
        {
            var interactionActions = GameplayInteractionActions.None;
            if (Pressed(GameInputAction.Interact)) interactionActions |= GameplayInteractionActions.Interact;
            if (Pressed(GameInputAction.Inspect)) interactionActions |= GameplayInteractionActions.Inspect;
            var interactionIntent = GameplayInteractionInput.Resolve(interactionActions);
            if ((interactionIntent & GameplayInteractionIntent.Interact) != 0) HandleControl(ClientControl.ContextInteract);
            if ((interactionIntent & GameplayInteractionIntent.Inspect) != 0) HandleControl(ClientControl.ContextInspect);
        }
        if (Pressed(GameInputAction.ContextInteract)) HandleControl(ClientControl.ContextInteract);
        if (HandleHudPointer())
        {
            PollDriverControl(gameTime.Elapsed.TotalSeconds);
            base.Update(gameTime);
            return;
        }
        SelectWorkstationFromMouse();
        HandleMouseCameraInput();
        HandleDirectMovement(gameTime.Elapsed.TotalSeconds);
        if (Pressed(GameInputAction.DirectScrape)) HandleControl(ClientControl.Scrape);
        if (Pressed(GameInputAction.DirectRack)) HandleControl(ClientControl.Rack);
        if (Pressed(GameInputAction.DirectStartWasher)) HandleControl(ClientControl.StartWasher);
        if (Pressed(GameInputAction.DirectUnload)) HandleControl(ClientControl.Unload);
        if (Pressed(GameInputAction.DirectDryAndRestock)) HandleControl(ClientControl.DryAndRestock);
        if (Pressed(GameInputAction.NextDish)) HandleControl(ClientControl.NextDish);
        if (Pressed(GameInputAction.ToggleRush)) HandleControl(ClientControl.ToggleRush);
        if (Pressed(GameInputAction.ConfirmBottleneck)) HandleControl(ClientControl.ConfirmBottleneck);
        if (Pressed(GameInputAction.ConfigureFlowCell)) HandleControl(ClientControl.ConfigureFlowCell);
        if (Pressed(GameInputAction.ToggleNewHire)) HandleControl(ClientControl.ToggleNewHire);
        if (Pressed(GameInputAction.TrainHappyPath)) HandleControl(ClientControl.TrainHappyPath);
        if (Pressed(GameInputAction.TrainRushPriority)) HandleControl(ClientControl.TrainRushPriority);
        if (Pressed(GameInputAction.TrainRareTray)) HandleControl(ClientControl.TrainRareTray);
        if (Pressed(GameInputAction.InspectIncident)) HandleControl(ClientControl.InspectIncident);
        if (Pressed(GameInputAction.ReplayIncident)) HandleControl(ClientControl.ReplayIncident);
        if (Pressed(GameInputAction.NextLens)) HandleControl(ClientControl.NextLens);
        if (Pressed(GameInputAction.ToggleProcessLens)) HandleControl(ClientControl.ToggleProcessLens);
        if (!placementMode && Pressed(GameInputAction.ProcessCaptureToggle)) HandleControl(ClientControl.ToggleProcessCapture);
        if (!placementMode && Pressed(GameInputAction.ProcessEditorToggle)) HandleControl(ClientControl.ToggleProcessEditor);
        if (!placementMode && Pressed(GameInputAction.AutomationEditorToggle)) HandleControl(ClientControl.ToggleAutomationEditor);
        if (!placementMode && Pressed(GameInputAction.TwoStationRoutingToggle)) HandleControl(ClientControl.ToggleTwoStationRouting);
        if (!placementMode && Pressed(GameInputAction.PatternCodexToggle)) HandleControl(ClientControl.TogglePatternCodex);
        if (!placementMode && Pressed(GameInputAction.VendorComparisonToggle)) HandleControl(ClientControl.ToggleVendorComparison);
        if (Pressed(GameInputAction.DeveloperToggle)) HandleControl(ClientControl.ToggleGodMode);
        if (Pressed(GameInputAction.DeveloperAddDirty)) HandleControl(ClientControl.GodAddDirty);
        if (Pressed(GameInputAction.DeveloperSetCleanSupply)) HandleControl(ClientControl.GodSetCleanSupply);
        if (Pressed(GameInputAction.DeveloperReset)) HandleControl(ClientControl.GodReset);
        if (Pressed(GameInputAction.DeveloperTogglePause)) HandleControl(ClientControl.GodTogglePause);
        if (Pressed(GameInputAction.DeveloperStep)) HandleControl(ClientControl.GodStep);
        if (Pressed(GameInputAction.DeveloperStickyReady)) HandleControl(ClientControl.GodStickyReady);
        if (Pressed(GameInputAction.DeveloperToggleLayout)) HandleControl(ClientControl.GodToggleLayout);
        if (Pressed(GameInputAction.DeveloperToggleBenchmark)) HandleControl(ClientControl.GodToggleBenchmark);
        if (Pressed(GameInputAction.DeveloperQuickSave)) HandleControl(ClientControl.GodQuickSave);
        if (Pressed(GameInputAction.DeveloperQuickLoad)) HandleControl(ClientControl.GodQuickLoad);
        if (Pressed(GameInputAction.TogglePlacement)) HandleControl(ClientControl.TogglePlacementMode);
        if (Pressed(GameInputAction.PlacementConfirm)) HandleControl(ClientControl.ConfirmPlacement);
        if (Pressed(GameInputAction.PlacementUndo)) HandleControl(ClientControl.UndoPlacement);
        if (Pressed(GameInputAction.PlacementReset)) HandleControl(ClientControl.ResetSandboxLayout);
        if (Pressed(placementMode ? GameInputAction.PlacementLeft : GameInputAction.CameraPanLeft)) HandleControl(placementMode ? ClientControl.PlacementLeft : ClientControl.CameraPanLeft);
        if (Pressed(placementMode ? GameInputAction.PlacementRight : GameInputAction.CameraPanRight)) HandleControl(placementMode ? ClientControl.PlacementRight : ClientControl.CameraPanRight);
        if (Pressed(placementMode ? GameInputAction.PlacementUp : GameInputAction.CameraPanUp)) HandleControl(placementMode ? ClientControl.PlacementUp : ClientControl.CameraPanUp);
        if (Pressed(placementMode ? GameInputAction.PlacementDown : GameInputAction.CameraPanDown)) HandleControl(placementMode ? ClientControl.PlacementDown : ClientControl.CameraPanDown);
        if (Pressed(GameInputAction.CameraZoomIn)) HandleControl(ClientControl.CameraZoomIn);
        if (Pressed(GameInputAction.CameraZoomOut)) HandleControl(ClientControl.CameraZoomOut);
        if (Pressed(GameInputAction.CameraReset)) HandleControl(ClientControl.CameraReset);
        if (Pressed(GameInputAction.JournalToggle)) HandleControl(ClientControl.ToggleQuestJournal);
        if (Pressed(GameInputAction.ShiftReportToggle)) HandleControl(ClientControl.ToggleShiftReport);
        if (Pressed(GameInputAction.SettingsToggle)) HandleControl(ClientControl.ToggleSettings);
        if (Pressed(GameInputAction.HelpClose)) HandleControl(ClientControl.ToggleHelp);
        if (Pressed(GameInputAction.Exit)) HandleControl(ClientControl.Exit);
        PollDriverControl(gameTime.Elapsed.TotalSeconds);

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        var snapshot = world.Snapshot();
        nativeRoom?.Synchronize(snapshot.Layout.Placements);
        nativeRoom?.UpdateCamera(camera);
        base.Draw(gameTime);
        GraphicsContext.CommandList.SetRenderTargetAndViewport(null, GraphicsDevice.Presenter.BackBuffer);
        if (nativeRoom is null)
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, new Color4(0.025f, 0.04f, 0.055f, 1f));
        }
        DrawRoom(snapshot);
        WritePendingScreenshot();
    }

    protected override void Destroy()
    {
        audioPresenter?.Dispose();
        nativeRoom?.Dispose();
        diamond?.Dispose();
        washerProjection?.Dispose();
        pixel?.Dispose();
        foreach (var icon in interactionIcons.Values) icon.Dispose();
        spriteBatch?.Dispose();
        base.Destroy();
    }

    private void Perform(DishAction action) => Execute(new PerformDishActionCommand(world.Tick, action, selectedKind));

    private void HandleDirectMovement(double elapsedSeconds)
    {
        if (placementMode)
        {
            activeMovementInput = DirectMovementInput.None;
            movementRepeatRemaining = 0;
            clickMovement.Cancel();
            clickMovementRepeatRemaining = 0;
            return;
        }

        var movementInput = DirectMovementInput.None;
        if (Down(GameInputAction.MoveAway)) movementInput |= DirectMovementInput.Away;
        if (Down(GameInputAction.MoveLeft)) movementInput |= DirectMovementInput.Left;
        if (Down(GameInputAction.MoveToward)) movementInput |= DirectMovementInput.Toward;
        if (Down(GameInputAction.MoveRight)) movementInput |= DirectMovementInput.Right;

        movementRepeatRemaining -= elapsedSeconds;
        if (movementInput == DirectMovementInput.None)
        {
            activeMovementInput = movementInput;
            movementRepeatRemaining = 0;
            clickMovementRepeatRemaining -= elapsedSeconds;
            if (clickMovementRepeatRemaining <= 0 &&
                clickMovement.TakeNext(world.PlayerCell, world.Placements, world.Tick) is { } clickCommand)
            {
                if (!Execute(clickCommand)) clickMovement.Cancel();
                clickMovementRepeatRemaining = 0.12;
            }
            return;
        }

        clickMovement.Cancel();
        clickMovementRepeatRemaining = 0;
        if (movementInput == activeMovementInput && movementRepeatRemaining > 0) return;
        activeMovementInput = movementInput;
        movementRepeatRemaining = 0.12;
        if (GameplayMovementInput.CreateCommand(movementInput, world.PlayerCell, world.Tick) is { } command) Execute(command);
    }

    private bool Pressed(GameInputAction action)
    {
        if ((InputActionCatalog.ContextOf(action) & InputActionContext.Developer) != 0 && !DeveloperToolsAvailable) return false;
        foreach (var key in inputBindings.KeysFor(action))
            if (Input.IsKeyPressed(StrideKeyboardAdapter.ToStrideKey(key))) return true;
        return false;
    }

    private bool Down(GameInputAction action)
    {
        if ((InputActionCatalog.ContextOf(action) & InputActionContext.Developer) != 0 && !DeveloperToolsAvailable) return false;
        foreach (var key in inputBindings.KeysFor(action))
            if (Input.IsKeyDown(StrideKeyboardAdapter.ToStrideKey(key))) return true;
        return false;
    }

    private string Binding(GameInputAction action) => inputBindings.DisplayName(action);

    private string PrimaryBinding(GameInputAction action) => inputBindings.PrimaryDisplayName(action);

    private string MovementBindingLabel() => string.Join(" ",
    [
        PrimaryBinding(GameInputAction.MoveAway),
        PrimaryBinding(GameInputAction.MoveLeft),
        PrimaryBinding(GameInputAction.MoveToward),
        PrimaryBinding(GameInputAction.MoveRight),
    ]);

    private string DeveloperBindingLegend(bool vertical = false) => vertical
        ? $"{PrimaryBinding(GameInputAction.DeveloperAddDirty)} ADD DIRTY   {PrimaryBinding(GameInputAction.DeveloperSetCleanSupply)} CLEAN\n{PrimaryBinding(GameInputAction.DeveloperReset)} RESET       {PrimaryBinding(GameInputAction.DeveloperTogglePause)} PAUSE\n{PrimaryBinding(GameInputAction.DeveloperStep)} STEP        {PrimaryBinding(GameInputAction.DeveloperStickyReady)} STICKY\n{PrimaryBinding(GameInputAction.DeveloperToggleLayout)} LAYOUT      {PrimaryBinding(GameInputAction.DeveloperToggleBenchmark)} 100K\n{PrimaryBinding(GameInputAction.DeveloperQuickSave)} SAVE       {PrimaryBinding(GameInputAction.DeveloperQuickLoad)} RESTORE"
        : $"{PrimaryBinding(GameInputAction.DeveloperAddDirty)} DIRTY  {PrimaryBinding(GameInputAction.DeveloperSetCleanSupply)} CLEAN  {PrimaryBinding(GameInputAction.DeveloperReset)} RESET  {PrimaryBinding(GameInputAction.DeveloperTogglePause)} PAUSE  {PrimaryBinding(GameInputAction.DeveloperStep)} STEP\n{PrimaryBinding(GameInputAction.DeveloperStickyReady)} FAULT  {PrimaryBinding(GameInputAction.DeveloperToggleLayout)} LAYOUT  {PrimaryBinding(GameInputAction.DeveloperToggleBenchmark)} 100K  {PrimaryBinding(GameInputAction.DeveloperQuickSave)} SAVE  {PrimaryBinding(GameInputAction.DeveloperQuickLoad)} LOAD";

    private void HandleControl(ClientControl control)
    {
        switch (control)
        {
            case ClientControl.MenuPrevious: SelectStartMenu(-1); break;
            case ClientControl.MenuNext: SelectStartMenu(1); break;
            case ClientControl.MenuConfirm: ConfirmStartMenu(); break;
            case ClientControl.MenuBack:
                screenRouter.DismissNewCareerConfirmation();
                UpdateWindowTitle();
                break;
            case ClientControl.ToggleSettings:
                screenRouter.ToggleSettings();
                settingsStatus = "READY";
                UpdateWindowTitle();
                break;
            case ClientControl.SettingsPrevious: SelectSetting(-1); break;
            case ClientControl.SettingsNext: SelectSetting(1); break;
            case ClientControl.SettingsDecrease: AdjustSetting(-1); break;
            case ClientControl.SettingsIncrease: AdjustSetting(1); break;
            case ClientControl.SettingsConfirm: ConfirmSetting(); break;
            case ClientControl.SettingsReset:
                clientSettings = ClientSettings.Default;
                inputBindings = clientSettings.InputBindings;
                SaveClientSettings("DEFAULTS RESTORED");
                break;
            case ClientControl.IntroNext: AdvanceIntro(); break;
            case ClientControl.PreviousGuidance: SelectGuidance(-1); break;
            case ClientControl.NextGuidance: SelectGuidance(1); break;
            case ClientControl.ToggleQuestJournal:
                var journalOpened = screenRouter.ToggleJournal();
                if (journalOpened)
                    selectedJournalQuest = DishStationFirstHoursContent.Narrative.IndexOf(
                        world.Snapshot().Progression.ActiveQuest ?? DishStationQuestId.OwnTheShift);
                commandFeedback = journalOpened ? "Quest journal opened." : "Quest journal closed.";
                UpdateWindowTitle();
                break;
            case ClientControl.ToggleHelp:
                var helpOpened = screenRouter.ToggleHelp();
                if (helpOpened) RecordHandbookVisit();
                commandFeedback = helpOpened ? "Controls and current opportunities opened." : "Help closed.";
                UpdateWindowTitle();
                break;
            case ClientControl.JournalPrevious: SelectJournalQuest(-1); break;
            case ClientControl.JournalNext: SelectJournalQuest(1); break;
            case ClientControl.ToggleQuestDetail:
                screenRouter.ToggleJournalDetail();
                UpdateWindowTitle();
                break;
            case ClientControl.JournalBack:
                screenRouter.BackFromJournal();
                UpdateWindowTitle();
                break;
            case ClientControl.ToggleShiftReport:
                if (ShiftReportVisible)
                {
                    screenRouter.ToggleShiftReport();
                    commandFeedback = "Shift report closed.";
                }
                else if (world.Snapshot().Progression.IsUnlocked(CareerCapability.ShiftScorecard))
                {
                    activeLens = SystemLens.Process;
                    screenRouter.ToggleShiftReport();
                    commandFeedback = "Shift report opened.";
                }
                else
                {
                    ShowLockedCapability(CareerCapability.ShiftScorecard);
                    break;
                }
                UpdateWindowTitle();
                break;
            case ClientControl.PreviousWorkstation: SelectWorkstation(-1); break;
            case ClientControl.NextWorkstation: SelectWorkstation(1); break;
            case ClientControl.ContextWork:
                var selectedFixture = (DishStationFixture)selectedWorkstation;
                var interactionPort = world.Topology.InteractionPort(selectedFixture);
                if (world.PlayerCell != interactionPort) RequestClickMovement(world.Placements.At(selectedFixture), selectedFixture.ToString());
                else PerformContextInteraction();
                break;
            case ClientControl.ContextInteract: PerformContextInteraction(); break;
            case ClientControl.ContextInspect: InspectContextInteraction(); break;
            case ClientControl.Scrape: Perform(DishAction.Scrape); break;
            case ClientControl.Rack: Perform(DishAction.Rack); break;
            case ClientControl.StartWasher: Perform(DishAction.StartWasher); break;
            case ClientControl.Unload: Perform(DishAction.Unload); break;
            case ClientControl.DryAndRestock: Perform(DishAction.DryAndRestock); break;
            case ClientControl.NextDish:
                selectedKind = selectedKind switch
                {
                    DishKind.Plate => DishKind.Glass,
                    DishKind.Glass => DishKind.Tray,
                    _ => DishKind.Plate,
                };
                break;
            case ClientControl.ToggleRush: Execute(new SetRushCommand(world.Tick, !world.RushEnabled)); break;
            case ClientControl.ConfirmBottleneck: Execute(new ConfirmBottleneckCommand(world.Tick, Workstations[selectedWorkstation].QueueState)); break;
            case ClientControl.ConfigureFlowCell:
                if (godMode || world.Snapshot().Progression.IsUnlocked(CareerCapability.LayoutEditor))
                    Execute(new ConfigureDishStationLayoutCommand(world.Tick, DishStationLayout.UShapedCell));
                else ShowLockedCapability(CareerCapability.LayoutEditor);
                break;
            case ClientControl.ToggleNewHire: Execute(new SetNewHireEnabledCommand(world.Tick, !world.NewHireEnabled)); break;
            case ClientControl.TrainHappyPath: Execute(new TrainNewHireCommand(world.Tick, DishProcessSpecification.HappyPath)); break;
            case ClientControl.TrainRushPriority: Execute(new TrainNewHireCommand(world.Tick, DishProcessSpecification.RushAware)); break;
            case ClientControl.TrainRareTray: Execute(new TrainNewHireCommand(world.Tick, DishProcessSpecification.FullyDocumented)); break;
            case ClientControl.InspectIncident:
                Execute(new InspectAutomationIncidentCommand(world.Tick));
                if (world.Snapshot().Automation.Incident.Recorded) activeLens = SystemLens.Runtime;
                break;
            case ClientControl.ReplayIncident:
                Execute(new ReplayAutomationIncidentCommand(world.Tick));
                if (world.Snapshot().Automation.Incident.Recorded) activeLens = SystemLens.Runtime;
                break;
            case ClientControl.ToggleIncidentLens:
                activeLens = activeLens == SystemLens.Runtime ? SystemLens.Process : SystemLens.Runtime;
                UpdateWindowTitle();
                break;
            case ClientControl.NextLens: SelectNextLens(); break;
            case ClientControl.StartShiftTrial: Execute(new StartShiftTrialCommand(world.Tick)); break;
            case ClientControl.ToggleProcessLens:
                if (world.TutorialStage == DishTutorialStage.InspectShortage)
                {
                    activeLens = SystemLens.Process;
                    Execute(new InspectProcessCommand(world.Tick));
                }
                else
                {
                    activeLens = activeLens == SystemLens.Process ? SystemLens.Reality : SystemLens.Process;
                    UpdateWindowTitle();
                }
                break;
            case ClientControl.ToggleProcessCapture: ToggleProcessCapture(); break;
            case ClientControl.ToggleProcessEditor: ToggleProcessEditor(); break;
            case ClientControl.ProcessEditorPrevious: SelectProcessEditorStep(-1); break;
            case ClientControl.ProcessEditorNext: SelectProcessEditorStep(1); break;
            case ClientControl.ProcessEditorMoveUp: MoveSelectedProcessStep(-1); break;
            case ClientControl.ProcessEditorMoveDown: MoveSelectedProcessStep(1); break;
            case ClientControl.ProcessEditorToggleAssignment: ToggleSelectedProcessAssignment(); break;
            case ClientControl.ProcessEditorNextRouting: SelectNextProcessRouting(); break;
            case ClientControl.ProcessEditorApply:
                if (Execute(new ApplyProcessEditCommand(world.Tick))) screenRouter.ToggleProcessEditor();
                break;
            case ClientControl.ProcessEditorClose:
                if (world.Snapshot().ProcessCapture.ActiveEdit is not null)
                    Execute(new DiscardProcessEditCommand(world.Tick));
                screenRouter.ToggleProcessEditor();
                break;
            case ClientControl.ToggleAutomationEditor: ToggleAutomationEditor(); break;
            case ClientControl.AutomationEditorPrevious: SelectAutomationRuleRow(-1); break;
            case ClientControl.AutomationEditorNext: SelectAutomationRuleRow(1); break;
            case ClientControl.AutomationEditorToggleValue: ToggleSelectedAutomationRuleValue(); break;
            case ClientControl.AutomationEditorApply:
                if (Execute(new ApplyAutomationRuleEditCommand(world.Tick))) screenRouter.ToggleAutomationEditor();
                break;
            case ClientControl.AutomationEditorSaveBaseline:
                Execute(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Baseline));
                break;
            case ClientControl.AutomationEditorSaveVariant:
                Execute(new SaveAutomationRulePresetCommand(world.Tick, AutomationPresetSlot.Variant));
                break;
            case ClientControl.AutomationEditorRunComparison:
                Execute(new RunAutomationRuleComparisonCommand(world.Tick));
                break;
            case ClientControl.AutomationEditorClose:
                if (world.Snapshot().Automation.ActiveEdit is not null)
                    Execute(new DiscardAutomationRuleEditCommand(world.Tick));
                screenRouter.ToggleAutomationEditor();
                break;
            case ClientControl.ToggleTwoStationRouting: ToggleTwoStationRouting(); break;
            case ClientControl.TwoStationRoutingPreviousStation: SelectRoutingStation(-1); break;
            case ClientControl.TwoStationRoutingNextStation: SelectRoutingStation(1); break;
            case ClientControl.TwoStationRoutingPreviousPolicy: ChangeRoutingPolicy(-1); break;
            case ClientControl.TwoStationRoutingNextPolicy: ChangeRoutingPolicy(1); break;
            case ClientControl.TwoStationRoutingCopy:
                ExecuteRouting(new CopyRoutingStationPolicyCommand(twoStationWorld.Tick,
                    DishRoutingStationId.MainDishRoom, DishRoutingStationId.PatioServiceStation));
                break;
            case ClientControl.TwoStationRoutingRunTrial:
                ExecuteRouting(new RunTwoStationRoutingTrialCommand(twoStationWorld.Tick));
                break;
            case ClientControl.TwoStationRoutingClose:
                screenRouter.ToggleTwoStationRouting();
                UpdateWindowTitle();
                break;
            case ClientControl.TogglePatternCodex: TogglePatternCodex(); break;
            case ClientControl.PatternCodexReflect: RecordPatternReflection(); break;
            case ClientControl.PatternCodexClose:
                if (PatternCodexVisible) screenRouter.TogglePatternCodex();
                UpdateWindowTitle();
                break;
            case ClientControl.ToggleVendorComparison: ToggleVendorComparison(); break;
            case ClientControl.VendorComparisonPrevious: SelectVendorProposal(-1); break;
            case ClientControl.VendorComparisonNext: SelectVendorProposal(1); break;
            case ClientControl.VendorComparisonRunTrial:
                ExecuteVendor(new RunVendorProposalTrialCommand(vendorWorld.Tick));
                break;
            case ClientControl.VendorComparisonClose:
                if (VendorComparisonVisible) screenRouter.ToggleVendorComparison();
                UpdateWindowTitle();
                break;
            case ClientControl.ToggleGodMode:
                if (!DeveloperToolsAvailable)
                {
                    commandFeedback = "LOCKED: sandbox tools unlock after the first shift.";
                    break;
                }
                godMode = !godMode;
                commandFeedback = godMode ? "Sandbox tools opened." : "Sandbox tools closed.";
                break;
            case ClientControl.GodAddDirty when godMode: Execute(new AddDirtyDishesCommand(world.Tick, selectedKind, 5)); break;
            case ClientControl.GodSetCleanSupply when godMode: Execute(new ConfigureDishSupplyCommand(world.Tick, DishState.Available, selectedKind, 10)); break;
            case ClientControl.GodReset when godMode: Execute(new ResetDishStationCommand(world.Tick)); break;
            case ClientControl.GodTogglePause when godMode:
                paused = !paused;
                commandFeedback = paused ? "Simulation paused." : "Simulation resumed.";
                UpdateWindowTitle();
                break;
            case ClientControl.GodStep when godMode && paused:
                world.Advance();
                UpdateWindowTitle();
                break;
            case ClientControl.GodStickyReady when godMode: Execute(new InjectStickyReadyFaultCommand(world.Tick)); break;
            case ClientControl.GodToggleLayout when godMode:
                Execute(new ConfigureDishStationLayoutCommand(world.Tick, world.Layout == DishStationLayout.Linear ? DishStationLayout.UShapedCell : DishStationLayout.Linear));
                break;
            case ClientControl.GodToggleBenchmark when godMode:
                benchmarkVisible = !benchmarkVisible;
                if (benchmarkVisible) benchmarkResult ??= SyntheticWorkBenchmark.Run(100_000, 100, 10_000);
                commandFeedback = benchmarkVisible ? "Showing a 10k-actor projection of the 100k benchmark." : "Synthetic benchmark view closed.";
                UpdateWindowTitle();
                break;
            case ClientControl.GodQuickSave when godMode:
                quickSaveJson = DishStationSaveStore.Serialize(world);
                commandFeedback = $"Saved deterministic checkpoint at tick {world.Tick.Value}.";
                UpdateWindowTitle();
                break;
            case ClientControl.GodQuickLoad when godMode && quickSaveJson is not null:
                world = DishStationSaveStore.Deserialize(quickSaveJson);
                simulationAccumulator = 0;
                commandFeedback = $"Restored deterministic checkpoint at tick {world.Tick.Value}.";
                SaveCareer();
                UpdateWindowTitle();
                break;
            case ClientControl.CameraPanLeft: MoveCamera(-28, 0); break;
            case ClientControl.CameraPanRight: MoveCamera(28, 0); break;
            case ClientControl.CameraPanUp: MoveCamera(0, -18); break;
            case ClientControl.CameraPanDown: MoveCamera(0, 18); break;
            case ClientControl.CameraZoomIn: ZoomCamera(0.1f); break;
            case ClientControl.CameraZoomOut: ZoomCamera(-0.1f); break;
            case ClientControl.CameraReset:
                camera = GameplayCameraInput.Recenter();
                commandFeedback = "Camera centered on the sandbox floor.";
                UpdateWindowTitle();
                break;
            case ClientControl.TogglePlacementMode:
                if (!godMode && !world.Snapshot().Progression.IsUnlocked(CareerCapability.LayoutEditor))
                {
                    ShowLockedCapability(CareerCapability.LayoutEditor);
                    break;
                }
                placementMode = !placementMode;
                if (placementMode)
                {
                    activeLens = SystemLens.Process;
                    placementPreview = world.Placements.At(placementFixture);
                }
                commandFeedback = placementMode
                    ? $"Placement mode: {Binding(GameInputAction.PlacementPrevious)}/{Binding(GameInputAction.PlacementNext)} fixture, click or {Binding(GameInputAction.PlacementConfirm)} to place, {Binding(GameInputAction.PlacementUndo)} undo, {Binding(GameInputAction.PlacementReset)} reset."
                    : "Placement mode closed.";
                UpdateWindowTitle();
                break;
            case ClientControl.PreviousPlacementFixture: SelectPlacementFixture(-1); break;
            case ClientControl.NextPlacementFixture: SelectPlacementFixture(1); break;
            case ClientControl.PlacementLeft: MovePlacementPreview(-1, 0); break;
            case ClientControl.PlacementRight: MovePlacementPreview(1, 0); break;
            case ClientControl.PlacementUp: MovePlacementPreview(0, -1); break;
            case ClientControl.PlacementDown: MovePlacementPreview(0, 1); break;
            case ClientControl.ConfirmPlacement: ConfirmPlacement(); break;
            case ClientControl.UndoPlacement: UndoPlacement(); break;
            case ClientControl.ResetSandboxLayout:
                placementUndo.Clear();
                Execute(new ConfigureDishStationLayoutCommand(world.Tick, DishStationLayout.Linear));
                placementPreview = world.Placements.At(placementFixture);
                break;
            case ClientControl.Exit: Exit(); break;
        }
    }

    private void PollDriverControl(double elapsedSeconds)
    {
        if (string.IsNullOrWhiteSpace(driverControlFile) && string.IsNullOrWhiteSpace(screenshotRequestFile)) return;
        driverPollAccumulator += elapsedSeconds;
        if (driverPollAccumulator < 0.05) return;
        driverPollAccumulator = 0;

        PollScreenshotRequest();
        if (string.IsNullOrWhiteSpace(driverControlFile)) return;
        try
        {
            if (!File.Exists(driverControlFile)) return;
            var instruction = File.ReadAllText(driverControlFile);
            var separator = instruction.IndexOf('|');
            if (separator <= 0 ||
                !long.TryParse(instruction.AsSpan(0, separator), out var sequence) ||
                sequence <= driverControlSequence ||
                !Enum.TryParse<ClientControl>(instruction.AsSpan(separator + 1), true, out var control)) return;
            driverControlSequence = sequence;
            HandleControl(control);
        }
        catch (IOException)
        {
            // The external driver may be replacing its tiny instruction file; retry next poll.
        }
    }

    private void PollScreenshotRequest()
    {
        if (string.IsNullOrWhiteSpace(screenshotRequestFile)) return;
        try
        {
            if (!File.Exists(screenshotRequestFile)) return;
            var instruction = File.ReadAllText(screenshotRequestFile);
            var separator = instruction.IndexOf('|');
            if (separator <= 0 ||
                !long.TryParse(instruction.AsSpan(0, separator), out var sequence) ||
                sequence <= screenshotRequestSequence) return;
            var path = instruction[(separator + 1)..].Trim();
            if (string.IsNullOrWhiteSpace(path)) return;
            screenshotRequestSequence = sequence;
            pendingScreenshotSequence = sequence;
            pendingScreenshotPath = Path.GetFullPath(path);
        }
        catch (IOException)
        {
            // The external driver may be replacing its request file; retry next poll.
        }
    }

    private void WritePendingScreenshot()
    {
        if (pendingScreenshotPath is not { } path) return;
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
            using var stream = File.Create(path);
            GraphicsDevice.Presenter.BackBuffer.Save(GraphicsContext.CommandList, stream, ImageFileType.Png);
            pendingScreenshotPath = null;
            commandFeedback = $"Captured UI evidence {pendingScreenshotSequence}.";
        }
        catch (IOException)
        {
            // Keep the request pending and retry on the next presented frame.
        }
    }

    private void ToggleProcessCapture()
    {
        var capture = world.Snapshot().ProcessCapture;
        if (capture.Active is null)
            Execute(new StartProcessCaptureCommand(world.Tick, "Captured dish process"));
        else
            Execute(new CompleteProcessCaptureCommand(world.Tick));
    }

    private void ToggleProcessEditor()
    {
        var capture = world.Snapshot().ProcessCapture;
        if (ProcessEditorVisible)
        {
            if (capture.ActiveEdit is not null) Execute(new DiscardProcessEditCommand(world.Tick));
            screenRouter.ToggleProcessEditor();
            return;
        }
        var artifact = capture.Artifacts.LastOrDefault();
        if (artifact is null)
        {
            commandFeedback = $"Capture a process with {Binding(GameInputAction.ProcessCaptureToggle)} before opening the editor.";
            return;
        }
        if (!Execute(new BeginProcessEditCommand(world.Tick, artifact.Id))) return;
        selectedProcessStep = 0;
        screenRouter.ToggleProcessEditor();
    }

    private void SelectProcessEditorStep(int offset)
    {
        var count = world.Snapshot().ProcessCapture.ActiveEdit?.Steps.Length ?? 0;
        if (count == 0) return;
        selectedProcessStep = (selectedProcessStep + offset + count) % count;
    }

    private void ToggleAutomationEditor()
    {
        var automation = world.Snapshot().Automation;
        if (AutomationEditorVisible)
        {
            if (automation.ActiveEdit is not null) Execute(new DiscardAutomationRuleEditCommand(world.Tick));
            screenRouter.ToggleAutomationEditor();
            return;
        }
        if (!Execute(new BeginAutomationRuleEditCommand(world.Tick))) return;
        selectedAutomationRuleRow = 0;
        screenRouter.ToggleAutomationEditor();
    }

    private void SelectAutomationRuleRow(int offset)
    {
        selectedAutomationRuleRow = (selectedAutomationRuleRow + offset + AutomationRuleEditorPresenter.RowCount) %
                                    AutomationRuleEditorPresenter.RowCount;
    }

    private void ToggleTwoStationRouting()
    {
        if (TwoStationRoutingVisible)
        {
            screenRouter.ToggleTwoStationRouting();
            UpdateWindowTitle();
            return;
        }
        if (world.TutorialStage != DishTutorialStage.EpisodeComplete)
        {
            commandFeedback = "Two-station routing opens after the first shift.";
            UpdateWindowTitle();
            return;
        }
        selectedRoutingStation = 0;
        screenRouter.ToggleTwoStationRouting();
        commandFeedback = "Compare the same routing decision at both stations.";
        UpdateWindowTitle();
    }

    private void SelectRoutingStation(int offset)
    {
        var count = DishStationTwoStationsContent.Configuration.Stations.Length;
        selectedRoutingStation = (selectedRoutingStation + offset + count) % count;
        UpdateWindowTitle();
    }

    private void ChangeRoutingPolicy(int offset)
    {
        var profile = DishStationTwoStationsContent.Configuration.Stations[selectedRoutingStation];
        var policies = Enum.GetValues<ProcessRoutingPolicy>();
        var current = Array.IndexOf(policies, twoStationWorld.Snapshot().PolicyFor(profile.Id));
        var next = policies[(current + offset + policies.Length) % policies.Length];
        ExecuteRouting(new SetRoutingStationPolicyCommand(twoStationWorld.Tick, profile.Id, next));
    }

    private bool ExecuteRouting(ITwoStationRoutingCommand command)
    {
        var result = twoStationWorld.ExecuteNow(command);
        commandFeedback = result.Message;
        if (result.Success)
        {
            var pattern = DishStationPatternContent.Strategy.PatternId;
            var wasRecognized = patternKnowledge.For(pattern).Has(PatternKnowledgeMilestone.Recognized);
            patternKnowledge = RestaurantPatternEvidenceRecognizer.Recognize(patternKnowledge,
                twoStationWorld.Snapshot(), DishStationPatternContent.Strategy);
            if (!wasRecognized && patternKnowledge.For(pattern).Has(PatternKnowledgeMilestone.Recognized))
                commandFeedback = "Codex recorded a reusable routing shape from your two station trials.";
            SaveCareer();
        }
        UpdateWindowTitle();
        return result.Success;
    }

    private void TogglePatternCodex()
    {
        if (PatternCodexVisible)
        {
            screenRouter.TogglePatternCodex();
            UpdateWindowTitle();
            return;
        }
        var knowledge = patternKnowledge.For(DishStationPatternContent.Strategy.PatternId);
        if (!knowledge.Has(PatternKnowledgeMilestone.Recognized))
        {
            commandFeedback = "Codex needs evidence from both copied and fitted two-station trials.";
            UpdateWindowTitle();
            return;
        }
        screenRouter.TogglePatternCodex();
        commandFeedback = "Codex opened to your restaurant evidence.";
        UpdateWindowTitle();
    }

    private void RecordPatternReflection()
    {
        var pattern = DishStationPatternContent.Strategy.PatternId;
        var knowledge = patternKnowledge.For(pattern);
        if (knowledge.Has(PatternKnowledgeMilestone.Named))
        {
            commandFeedback = "The Strategy name is already recorded with your evidence.";
            UpdateWindowTitle();
            return;
        }
        try
        {
            patternKnowledge = PatternNamingService.RecordReflection(patternKnowledge, DishStationPatternContent.Strategy);
            commandFeedback = "Strategy Pattern named from your copied and fitted routing evidence.";
            SaveCareer();
        }
        catch (InvalidOperationException error)
        {
            commandFeedback = error.Message;
        }
        UpdateWindowTitle();
    }

    private void ToggleVendorComparison()
    {
        if (VendorComparisonVisible)
        {
            screenRouter.ToggleVendorComparison();
            UpdateWindowTitle();
            return;
        }
        if (!patternKnowledge.For(DishStationPatternContent.Strategy.PatternId).Has(PatternKnowledgeMilestone.Named))
        {
            commandFeedback = "Sam's proposal review opens after the reusable routing choice has a recorded name.";
            UpdateWindowTitle();
            return;
        }
        screenRouter.ToggleVendorComparison();
        commandFeedback = "Sam's build and vendor contract proposals are ready for the same incident trial.";
        UpdateWindowTitle();
    }

    private void SelectVendorProposal(int offset)
    {
        var proposals = Enum.GetValues<VendorProposalId>();
        var current = Array.IndexOf(proposals, vendorWorld.SelectedProposal);
        var next = proposals[(current + offset + proposals.Length) % proposals.Length];
        ExecuteVendor(new SelectVendorProposalCommand(vendorWorld.Tick, next));
    }

    private bool ExecuteVendor(IVendorOutsourcingCommand command)
    {
        var result = vendorWorld.ExecuteNow(command);
        commandFeedback = result.Message;
        if (result.Success)
        {
            audioRouter.Confirm(EmitAudio);
            SaveCareer();
        }
        UpdateWindowTitle();
        return result.Success;
    }

    private void ToggleSelectedAutomationRuleValue()
    {
        var draft = world.Snapshot().Automation.ActiveEdit;
        if (draft is null) return;
        switch (selectedAutomationRuleRow)
        {
            case 0:
                Execute(new SetAutomationRuleEnabledCommand(world.Tick, !draft.Enabled));
                break;
            case 1:
                Execute(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.RackPresent));
                break;
            case 2:
                Execute(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.ReportedReady));
                break;
            case 3:
                Execute(new ToggleAutomationRuleConditionCommand(world.Tick, AutomationObservable.PhysicalReady));
                break;
            case 4:
                commandFeedback = "START WASHER is the only action available in this editor.";
                break;
        }
    }

    private void MoveSelectedProcessStep(int offset)
    {
        var draft = world.Snapshot().ProcessCapture.ActiveEdit;
        if (draft is null || draft.Steps.Length == 0) return;
        selectedProcessStep = Math.Clamp(selectedProcessStep, 0, draft.Steps.Length - 1);
        if (Execute(new MoveProcessStepCommand(world.Tick, draft.Steps[selectedProcessStep].Id, offset)))
            selectedProcessStep = Math.Clamp(selectedProcessStep + offset, 0, draft.Steps.Length - 1);
    }

    private void ToggleSelectedProcessAssignment()
    {
        var draft = world.Snapshot().ProcessCapture.ActiveEdit;
        if (draft is null || draft.Steps.Length == 0) return;
        selectedProcessStep = Math.Clamp(selectedProcessStep, 0, draft.Steps.Length - 1);
        var step = draft.Steps[selectedProcessStep];
        Execute(new AssignProcessStepCommand(world.Tick, step.Id,
            step.AssignedActor.Value == 1 ? new ActorId(0) : new ActorId(1)));
    }

    private void SelectNextProcessRouting()
    {
        var draft = world.Snapshot().ProcessCapture.ActiveEdit;
        if (draft is null) return;
        var values = Enum.GetValues<ProcessRoutingPolicy>();
        var next = values[((int)draft.RoutingPolicy + 1) % values.Length];
        Execute(new SetProcessRoutingPolicyCommand(world.Tick, next));
    }

    private bool Execute(ISimulationCommand command)
    {
        var result = world.ExecuteNow(command);
        if (result.Success && command is ResetDishStationCommand)
        {
            characterPresenter.Reset();
            audioRouter.Initialize(world.Snapshot(), world.Notifications.Count);
            InitializeDialogue(world.Snapshot());
        }
        if (result.Success && command is PerformDishActionCommand or InteractWithDishStationFixtureCommand)
            characterPresenter.NotifyPlayerWork();
        audioRouter.ObserveCommand(command, result, EmitAudio);
        audioRouter.Observe(world.Snapshot(), world.Notifications, EmitAudio);
        commandFeedback = result.Success ? result.Message : $"BLOCKED: {result.Message}";
        if (result.Success && world.IntroComplete) SaveCareer();
        UpdateWindowTitle();
        return result.Success;
    }

    private void AdvanceIntro()
    {
        if (world.IntroComplete) return;
        if (introPage < 4)
        {
            introPage++;
            UpdateWindowTitle();
            return;
        }
        Execute(new CompleteIntroCommand(world.Tick, selectedGuidance, selectedReducedMotion, selectedHighContrast));
        if (world.IntroComplete) screenRouter.ShowCareer(briefingComplete: true);
    }

    private void SelectStartMenu(int offset)
    {
        if (NewCareerConfirmationVisible) return;
        startMenuSelection = (startMenuSelection + offset + 3) % 3;
        UpdateWindowTitle();
    }

    private void ConfirmStartMenu()
    {
        if (startMenuSelection == 0 && !NewCareerConfirmationVisible)
        {
            try
            {
                var career = AutomationCareerSaveStore.LoadFile(careerSavePath, 42,
                    DishStationTwoStationsContent.Configuration, DishStationVendorContent.Configuration);
                world = career.FirstShift;
                twoStationWorld = career.TwoStationRouting;
                vendorWorld = career.VendorOutsourcing;
                patternKnowledge = career.PatternKnowledge;
                var snapshot = world.Snapshot();
                observedLevel = snapshot.Progression.Level;
                observedActiveQuest = snapshot.Progression.ActiveQuest;
                characterPresenter.Reset();
                audioRouter.Initialize(snapshot, world.Notifications.Count);
                InitializeDialogue(snapshot);
                simulationAccumulator = 0;
                autosaveAccumulator = 0;
                lastAutosaveTick = world.Tick.Value;
                saveStatus = "LOADED";
                screenRouter.ShowCareer(snapshot.Onboarding.Complete);
                commandFeedback = $"Career resumed at level {snapshot.Progression.Level}.";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or InvalidDataException or NotSupportedException)
            {
                saveStatus = "ERROR";
                commandFeedback = "Career save could not be read. Start a new career or restore the file.";
                startMenuSelection = 1;
            }
            UpdateWindowTitle();
            return;
        }

        if (startMenuSelection == 2 && !NewCareerConfirmationVisible)
        {
            screenRouter.ToggleSettings();
            settingsStatus = "READY";
            UpdateWindowTitle();
            return;
        }

        if (!NewCareerConfirmationVisible)
        {
            screenRouter.ShowNewCareerConfirmation();
            UpdateWindowTitle();
            return;
        }

        world = new DishStationWorld(42, DishStationFirstHoursContent.ScenarioConfiguration);
        twoStationWorld = new(42, DishStationTwoStationsContent.Configuration);
        vendorWorld = new(DishStationVendorContent.Configuration);
        patternKnowledge = PatternKnowledgeProfile.Empty;
        introPage = 0;
        selectedGuidance = GuidanceMode.Guided;
        selectedReducedMotion = false;
        selectedHighContrast = false;
        observedLevel = 1;
        observedActiveQuest = DishStationQuestId.ClockIn;
        progressionReceiptQuest = null;
        progressionReceiptSeconds = 0;
        characterPresenter.Reset();
        audioRouter.Initialize(world.Snapshot(), world.Notifications.Count);
        InitializeDialogue(world.Snapshot());
        simulationAccumulator = 0;
        autosaveAccumulator = 0;
        lastAutosaveTick = -1;
        saveStatus = "NEW";
        screenRouter.ShowCareer(briefingComplete: false);
        commandFeedback = "New career ready. Complete the first-shift briefing to replace the previous checkpoint.";
        UpdateWindowTitle();
    }

    private void SaveCareer()
    {
        if (!careerSaveEnabled || !world.IntroComplete) return;
        try
        {
            AutomationCareerSaveStore.SaveFileAtomic(careerSavePath,
                new(world, twoStationWorld, vendorWorld, patternKnowledge));
            lastAutosaveTick = world.Tick.Value;
            saveStatus = "SAVED";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            saveStatus = "ERROR";
            commandFeedback = "AUTOSAVE FAILED: your current session is still running.";
        }
    }

    private void SelectGuidance(int offset)
    {
        if (introPage == 3)
        {
            var count = Enum.GetValues<GuidanceMode>().Length;
            selectedGuidance = (GuidanceMode)(((int)selectedGuidance + offset + count) % count);
        }
        else if (introPage == 4)
        {
            if (offset < 0) selectedReducedMotion = !selectedReducedMotion;
            else selectedHighContrast = !selectedHighContrast;
        }
        else return;
        UpdateWindowTitle();
    }

    private void SelectJournalQuest(int offset)
    {
        var count = DishStationFirstHoursContent.Quests.Count;
        selectedJournalQuest = (selectedJournalQuest + offset + count) % count;
        UpdateWindowTitle();
    }

    private void ObserveProgression()
    {
        var progression = world.Snapshot().Progression;
        if (progression.ActiveQuest != observedActiveQuest)
        {
            progressionReceiptQuest = observedActiveQuest;
            progressionReceiptLevel = progression.Level;
            progressionReceiptLeveledUp = progression.Level > observedLevel;
            progressionReceiptSeconds = 8f;
            observedActiveQuest = progression.ActiveQuest;
        }
        observedLevel = progression.Level;
        if (progression.ActiveQuest is null && !playtestEvidenceAttempted && !string.IsNullOrWhiteSpace(playtestEvidencePath))
            WritePlaytestEvidence();
    }

    private void WritePlaytestEvidence()
    {
        playtestEvidenceAttempted = true;
        try
        {
            var completedAtUtc = DateTimeOffset.UtcNow;
            var evidence = FirstHoursPlaytestEvidence.Create(playtestSessionId, playtestStartedAtUtc, completedAtUtc, world.Snapshot(),
                handbookVisits.Values.OrderBy(visit => visit.FirstOpenedAtTick).ToArray());
            FirstHoursPlaytestEvidenceStore.SaveFileAtomic(playtestEvidencePath!, evidence);
            playtestEvidenceStatus = "written";
            commandFeedback = $"Playtest evidence written for session {playtestSessionId}.";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or NotSupportedException)
        {
            playtestEvidenceStatus = "error";
            commandFeedback = "PLAYTEST EVIDENCE FAILED: the career remains playable and saved.";
        }
        UpdateWindowTitle();
    }

    private void RecordHandbookVisit()
    {
        var stage = world.TutorialStage;
        var tick = world.Tick.Value;
        handbookVisits[stage] = handbookVisits.TryGetValue(stage, out var visit)
            ? visit with { OpenCount = visit.OpenCount + 1, LastOpenedAtTick = tick }
            : new(stage, 1, tick, tick);
    }

    private void HandleStartMenuPointer()
    {
        if (NewCareerConfirmationVisible)
        {
            if (LeftClickIn(218, 388, 250, 48)) HandleControl(ClientControl.MenuConfirm);
            else if (LeftClickIn(548, 388, 250, 48)) HandleControl(ClientControl.MenuBack);
            return;
        }

        if (LeftClickIn(226, 235, 260, 92))
        {
            startMenuSelection = 0;
            HandleControl(ClientControl.MenuConfirm);
        }
        else if (LeftClickIn(526, 235, 260, 92))
        {
            startMenuSelection = 1;
            HandleControl(ClientControl.MenuConfirm);
        }
        else if (LeftClickIn(326, 349, 460, 52))
        {
            startMenuSelection = 2;
            HandleControl(ClientControl.MenuConfirm);
        }
    }

    private void HandleSettingsPointer()
    {
        for (var index = 0; index < 4; index++)
        {
            var y = 161 + index * 62;
            if (LeftClickIn(222, y, 580, 48)) settingsSelection = index;
            if (LeftClickIn(240, y + 8, 54, 32))
            {
                settingsSelection = index;
                AdjustSetting(-1);
                return;
            }
            if (LeftClickIn(730, y + 8, 54, 32))
            {
                settingsSelection = index;
                AdjustSetting(1);
                return;
            }
        }

        if (LeftClickIn(222, 409, 580, 48))
        {
            settingsSelection = (int)ClientSettingsOption.ResetDefaults;
            ConfirmSetting();
        }
        else if (LeftClickIn(696, 490, 106, 34))
        {
            HandleControl(ClientControl.ToggleSettings);
        }
    }

    private void SelectSetting(int offset)
    {
        var count = Enum.GetValues<ClientSettingsOption>().Length;
        settingsSelection = (settingsSelection + offset + count) % count;
        settingsStatus = "READY";
        UpdateWindowTitle();
    }

    private void AdjustSetting(int direction)
    {
        var option = (ClientSettingsOption)settingsSelection;
        if (option == ClientSettingsOption.ResetDefaults) return;
        clientSettings = clientSettings.Adjust(option, direction);
        inputBindings = clientSettings.InputBindings;
        var message = option switch
        {
            ClientSettingsOption.MasterVolume => clientSettings.MasterVolumePercent == 0 ? "SAVED • AUDIO MUTED" : "SAVED • AUDIO LIVE",
            ClientSettingsOption.WindowMode => "SAVED • WINDOW MODE APPLIES AFTER RESTART",
            _ => "SAVED",
        };
        if (option == ClientSettingsOption.MasterVolume)
        {
            audioPresenter?.SetMasterVolume(clientSettings.MasterVolumePercent);
            audioStatus = audioPresenter is null ? audioStatus : clientSettings.MasterVolumePercent == 0 ? "muted" : "ready";
        }
        SaveClientSettings(message);
    }

    private void ConfirmSetting()
    {
        var option = (ClientSettingsOption)settingsSelection;
        if (option == ClientSettingsOption.ResetDefaults)
        {
            clientSettings = ClientSettings.Default;
            inputBindings = clientSettings.InputBindings;
            audioPresenter?.SetMasterVolume(clientSettings.MasterVolumePercent);
            audioStatus = audioPresenter is null ? audioStatus : "ready";
            SaveClientSettings("DEFAULTS RESTORED");
            return;
        }
        AdjustSetting(1);
    }

    private void SaveClientSettings(string successStatus)
    {
        try
        {
            ClientSettingsStore.SaveFileAtomic(clientSettingsPath, clientSettings);
            settingsStatus = successStatus;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            settingsStatus = "SAVE FAILED • CURRENT SESSION KEPT";
        }
        UpdateWindowTitle();
    }

    private void HandleIntroPointer()
    {
        if (introPage == 3)
        {
            for (var index = 0; index < Enum.GetValues<GuidanceMode>().Length; index++)
            {
                if (!LeftClickIn(177 + index * 230, 325, 210, 76)) continue;
                selectedGuidance = (GuidanceMode)index;
                UpdateWindowTitle();
                return;
            }
        }
        else if (introPage == 4)
        {
            if (LeftClickIn(177, 325, 300, 86))
            {
                selectedReducedMotion = !selectedReducedMotion;
                UpdateWindowTitle();
                return;
            }
            if (LeftClickIn(507, 325, 300, 86))
            {
                selectedHighContrast = !selectedHighContrast;
                UpdateWindowTitle();
                return;
            }
        }

        if (LeftClickIn(640, 456, 218, 48)) HandleControl(ClientControl.IntroNext);
    }

    private void HandleJournalPointer()
    {
        if (QuestDetailVisible)
        {
            if (LeftClickIn(620, 486, 116, 34)) HandleControl(ClientControl.JournalBack);
            else if (LeftClickIn(750, 486, 116, 34)) HandleControl(ClientControl.ToggleQuestJournal);
            return;
        }

        for (var index = 0; index < DishStationFirstHoursContent.Quests.Count; index++)
        {
            if (!LeftClickIn(126, 141 + index * 43, 770, 42)) continue;
            selectedJournalQuest = index;
            UpdateWindowTitle();
            return;
        }
        if (LeftClickIn(620, 502, 116, 32)) HandleControl(ClientControl.ToggleQuestDetail);
        else if (LeftClickIn(750, 502, 116, 32)) HandleControl(ClientControl.ToggleQuestJournal);
    }

    private bool HandleHudPointer()
    {
        if (LeftClickIn(622, 522, 88, 26))
        {
            HandleControl(ClientControl.ToggleSettings);
            return true;
        }
        if (LeftClickIn(716, 522, 88, 26))
        {
            HandleControl(ClientControl.ToggleQuestJournal);
            return true;
        }
        if (LeftClickIn(810, 522, 88, 26))
        {
            HandleControl(ClientControl.ToggleHelp);
            return true;
        }
        if (LeftClickIn(904, 522, 88, 26))
        {
            HandleControl(ClientControl.ToggleShiftReport);
            return true;
        }
        return false;
    }

    private bool LeftClickIn(float x, float y, float width, float height)
    {
        if (!Input.IsMouseButtonPressed(MouseButton.Left)) return false;
        var point = UiMousePosition();
        return point.X >= x && point.X <= x + width && point.Y >= y && point.Y <= y + height;
    }

    private void DrawIntroWizard()
    {
        DrawModalScrim();
        DrawPanel(132, 78, 760, 444, new Color(17, 30, 38, 248), Color.DeepSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"STARTING BRIEF  {introPage + 1}/5", 166, 107, 1, Color.LightSkyBlue);
        var (title, body) = introPage < 3
            ? FirstShiftNarrativePresenter.Briefing(introPage)
            : introPage == 3
                ? new FirstShiftBriefingPresentation("CHOOSE YOUR GUIDANCE", "GUIDED OFFERS DETAILED NEXT-ACTION PROMPTS. CONTEXTUAL EMPHASIZES THE\nCURRENT OUTCOME AND EVIDENCE. MINIMAL LEAVES QUEST CONDITIONS AND\nWORKPLACE FEEDBACK. YOU CAN CHANGE THIS CHOICE LATER IN SETTINGS.")
                : new FirstShiftBriefingPresentation("COMFORT AND READABILITY", "REDUCED MOTION REMOVES CURSOR PULSING AND ACTOR EASING. HIGH CONTRAST\nUSES DARKER PANELS AND STRONGER EDGES. BOTH SETTINGS ARE INDEPENDENT\nAND SAVED WITH YOUR CAREER.");
        PixelFont.Draw(spriteBatch!, pixel!, title, 166, 147, 2, Color.White, 50);
        PixelFont.Draw(spriteBatch!, pixel!, body, 166, 207, 1, Color.LightGray, 92);
        if (introPage == 3)
        {
            var x = 177f;
            foreach (var mode in Enum.GetValues<GuidanceMode>())
            {
                var selected = mode == selectedGuidance;
                DrawPanel(x, 325, 210, 76, selected ? new Color(40, 75, 88) : new Color(27, 43, 51), selected ? Color.LightGreen : new Color(84, 103, 110));
                PixelFont.Draw(spriteBatch!, pixel!, mode.ToString().ToUpperInvariant(), x + 14, 340, 1, selected ? Color.LightGreen : Color.White);
                PixelFont.Draw(spriteBatch!, pixel!, mode switch { GuidanceMode.Guided => "FULL PROMPTS", GuidanceMode.Contextual => "OUTCOME + CLUES", _ => "WORLD SIGNALS" }, x + 14, 366, 1, Color.LightGray);
                x += 230;
            }
            PixelFont.Draw(spriteBatch!, pixel!, $"CLICK A CARD OR {Binding(GameInputAction.IntroPrevious)}/{Binding(GameInputAction.IntroNext)} CHOOSE", 166, 438, 1, Color.LightGray);
        }
        else if (introPage == 4)
        {
            DrawPanel(177, 325, 300, 86, selectedReducedMotion ? new Color(40, 75, 88) : new Color(27, 43, 51), selectedReducedMotion ? Color.LightGreen : new Color(84, 103, 110));
            PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.IntroPrevious)}  REDUCED MOTION  {(selectedReducedMotion ? "ON" : "OFF")}", 195, 344, 1, selectedReducedMotion ? Color.LightGreen : Color.White);
            PixelFont.Draw(spriteBatch!, pixel!, "SNAP ACTORS / STATIC RETICLE", 195, 375, 1, Color.LightGray);
            DrawPanel(507, 325, 300, 86, selectedHighContrast ? new Color(40, 75, 88) : new Color(27, 43, 51), selectedHighContrast ? Color.LightGreen : new Color(84, 103, 110));
            PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.IntroNext)}  HIGH CONTRAST  {(selectedHighContrast ? "ON" : "OFF")}", 525, 344, 1, selectedHighContrast ? Color.LightGreen : Color.White);
            PixelFont.Draw(spriteBatch!, pixel!, "DARK PANELS / BRIGHT EDGES", 525, 375, 1, Color.LightGray);
        }
        DrawPanel(640, 456, 218, 48, new Color(58, 52, 27), Color.Yellow);
        PixelFont.Draw(spriteBatch!, pixel!, introPage == 4 ? "START CAREER" : "CONTINUE", introPage == 4 ? 687 : 704, 474, 1, Color.Yellow);
    }

    private void DrawStartMenu()
    {
        DrawModalScrim();
        DrawPanel(186, 104, 652, 392, new Color(17, 30, 38, 250), Color.DeepSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "THE AUTOMATION GAME", 226, 140, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "RETURN TO THE CREW AND THE CONSEQUENCES OF YOUR LAST SHIFT.", 226, 184, 1, Color.LightGray, 78);
        if (NewCareerConfirmationVisible)
        {
            PixelFont.Draw(spriteBatch!, pixel!, "START A NEW CAREER?", 226, 244, 2, Color.OrangeRed);
            PixelFont.Draw(spriteBatch!, pixel!, "THE EXISTING CHECKPOINT IS KEPT UNTIL YOU COMPLETE THE NEW BRIEFING.\nAFTER THAT, THE NEW CAREER REPLACES IT.", 226, 292, 1, Color.White, 73);
            DrawPanel(218, 388, 250, 48, new Color(65, 48, 29), Color.Yellow);
            PixelFont.Draw(spriteBatch!, pixel!, "CONFIRM NEW CAREER", 249, 406, 1, Color.Yellow);
            DrawPanel(548, 388, 250, 48, new Color(31, 45, 51), Color.LightGray);
            PixelFont.Draw(spriteBatch!, pixel!, "KEEP EXISTING CAREER", 573, 406, 1, Color.LightGray);
            return;
        }

        DrawPanel(226, 235, 260, 92, startMenuSelection == 0 ? new Color(43, 76, 88) : new Color(25, 43, 51), startMenuSelection == 0 ? Color.LightGreen : new Color(78, 97, 104));
        PixelFont.Draw(spriteBatch!, pixel!, "CONTINUE", 246, 255, 2, startMenuSelection == 0 ? Color.LightGreen : Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, saveStatus == "ERROR" ? "CHECKPOINT UNREADABLE" : "RESUME LAST CHECKPOINT", 246, 295, 1, saveStatus == "ERROR" ? Color.OrangeRed : Color.LightGray);
        DrawPanel(526, 235, 260, 92, startMenuSelection == 1 ? new Color(74, 58, 43) : new Color(25, 43, 51), startMenuSelection == 1 ? Color.Goldenrod : new Color(78, 97, 104));
        PixelFont.Draw(spriteBatch!, pixel!, "NEW CAREER", 546, 255, 2, startMenuSelection == 1 ? Color.Goldenrod : Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, "RESTART FIRST SHIFT", 546, 295, 1, Color.LightGray);
        DrawPanel(326, 349, 460, 52, startMenuSelection == 2 ? new Color(43, 65, 75) : new Color(25, 43, 51), startMenuSelection == 2 ? Color.LightSkyBlue : new Color(78, 97, 104));
        PixelFont.Draw(spriteBatch!, pixel!, "SETTINGS", 350, 367, 1, startMenuSelection == 2 ? Color.LightSkyBlue : Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, $"{clientSettings.WindowMode.ToString().ToUpperInvariant()}  UI {clientSettings.UiScalePercent}%", 500, 367, 1, Color.LightGray, 43);
        PixelFont.Draw(spriteBatch!, pixel!, $"CLICK A CARD   OR {Binding(GameInputAction.MenuPrevious)}/{Binding(GameInputAction.MenuNext)} CHOOSE + {Binding(GameInputAction.MenuConfirm)}   {Binding(GameInputAction.MenuBack)} EXIT", 312, 438, 1, Color.Yellow);
    }

    private void DrawSettings()
    {
        DrawModalScrim();
        DrawPanel(186, 62, 652, 484, new Color(17, 30, 38, 252), Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "SETTINGS", 222, 88, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "CHANGES SAVE IMMEDIATELY. WINDOW MODE APPLIES ON RESTART.", 222, 122, 1, Color.LightGray, 80);

        for (var index = 0; index < Enum.GetValues<ClientSettingsOption>().Length; index++)
        {
            var option = (ClientSettingsOption)index;
            var y = 161 + index * 62;
            var selected = settingsSelection == index;
            DrawPanel(222, y, 580, 48, selected ? new Color(38, 60, 69) : new Color(24, 39, 45),
                selected ? Color.LightSkyBlue : new Color(72, 91, 98));
            PixelFont.Draw(spriteBatch!, pixel!, SettingsOptionLabel(option), 316, y + 10, 1, selected ? Color.White : Color.LightGray, 29);
            PixelFont.Draw(spriteBatch!, pixel!, SettingsOptionValue(option), 500, y + 10, 1,
                option == ClientSettingsOption.MasterVolume ? Color.Goldenrod : Color.LightGreen, 36);
            if (option == ClientSettingsOption.ResetDefaults) continue;
            DrawPanel(240, y + 8, 54, 32, new Color(31, 45, 51), Color.LightGray);
            PixelFont.Draw(spriteBatch!, pixel!, "-", 264, y + 19, 1, Color.White);
            DrawPanel(730, y + 8, 54, 32, new Color(31, 45, 51), Color.LightGray);
            PixelFont.Draw(spriteBatch!, pixel!, "+", 754, y + 19, 1, Color.White);
        }

        PixelFont.Draw(spriteBatch!, pixel!, $"{settingsStatus}   BINDINGS PROFILE V{clientSettings.InputBindings.SchemaVersion} / {Enum.GetValues<GameInputAction>().Length} ACTIONS", 222, 472, 1,
            settingsStatus.StartsWith("SAVE FAILED", StringComparison.Ordinal) ? Color.OrangeRed : Color.Yellow, 78);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.SettingsPrevious)}/{Binding(GameInputAction.SettingsNext)} SELECT   {Binding(GameInputAction.SettingsDecrease)}/{Binding(GameInputAction.SettingsIncrease)} CHANGE   {Binding(GameInputAction.SettingsReset)} RESET", 222, 501, 1, Color.LightGray, 72);
        DrawPanel(696, 490, 106, 34, new Color(31, 45, 51), Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "CLOSE", 727, 502, 1, Color.LightSkyBlue);
    }

    private static string SettingsOptionLabel(ClientSettingsOption option) => option switch
    {
        ClientSettingsOption.MasterVolume => "MASTER VOLUME",
        ClientSettingsOption.UiScale => "UI SCALE",
        ClientSettingsOption.CameraSensitivity => "CAMERA SENSITIVITY",
        ClientSettingsOption.WindowMode => "WINDOW MODE",
        ClientSettingsOption.ResetDefaults => "RESET DEFAULTS",
        _ => option.ToString().ToUpperInvariant(),
    };

    private string SettingsOptionValue(ClientSettingsOption option) => option switch
    {
        ClientSettingsOption.MasterVolume => $"{clientSettings.MasterVolumePercent}%  {(clientSettings.MasterVolumePercent == 0 ? "MUTED" : "LIVE")}",
        ClientSettingsOption.UiScale => $"{clientSettings.UiScalePercent}%  FITTED",
        ClientSettingsOption.CameraSensitivity => $"{clientSettings.CameraSensitivityPercent}%",
        ClientSettingsOption.WindowMode => clientSettings.WindowMode == ClientWindowMode.Windowed ? "WINDOWED / RESTART" : "BORDERLESS / RESTART",
        ClientSettingsOption.ResetDefaults => "PRESS ENTER",
        _ => "",
    };

    private void DrawQuestJournal(CareerProgressionSnapshot progression)
    {
        DrawModalScrim();
        DrawPanel(104, 55, 816, 490, new Color(17, 30, 38, 248), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, $"FIRST SHIFT JOURNAL   LEVEL {progression.Level}   XP {progression.Experience}   ACTIVE {PaceLabel(progression.ActivePlayTicks)}", 132, 78, 2, Color.White, 75);
        PixelFont.Draw(spriteBatch!, pixel!, "OUTCOMES UNLOCK WAYS OF SEEING AND ACTING; THEY DO NOT INCREASE MACHINE SPEED.", 132, 113, 1, Color.LightGray, 105);
        var y = 146f;
        for (var index = 0; index < DishStationFirstHoursContent.Quests.Count; index++)
        {
            var definition = DishStationFirstHoursContent.Quests[index];
            var state = progression.Quest(definition.Id);
            var active = progression.ActiveQuest == definition.Id;
            var selected = selectedJournalQuest == index;
            var color = state.Complete ? Color.LightGreen : active ? Color.Yellow : new Color(102, 117, 121);
            DrawRect(126, y - 5, 770, 42, active ? new Color(38, 51, 55) : new Color(23, 37, 43));
            if (selected) DrawBorder(new RectangleF(126, y - 5, 770, 42), 2, Color.DeepSkyBlue);
            PixelFont.Draw(spriteBatch!, pixel!, state.Complete ? PaceLabel(state.ElapsedTicks) : active ? $"{state.Percent}%" : "LOCK", 140, y + 4, 1, color);
            PixelFont.Draw(spriteBatch!, pixel!, definition.Title, 197, y + 4, 1, Color.White, 34);
            PixelFont.Draw(spriteBatch!, pixel!, $"+{definition.ExperienceReward} XP  {CapabilityLabel(definition.CapabilityReward)}", 500, y + 4, 1, color, 49);
            PixelFont.Draw(spriteBatch!, pixel!, definition.ObservableOutcome, 197, y + 21, 1, Color.LightGray, 92);
            y += 43;
        }
        PixelFont.Draw(spriteBatch!, pixel!, $"CLICK A ROW OR {Binding(GameInputAction.JournalPrevious)}/{Binding(GameInputAction.JournalNext)} TO SELECT", 132, 516, 1, Color.LightGray);
        DrawPanel(620, 502, 116, 32, new Color(58, 52, 27), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, "DETAILS", 647, 513, 1, Color.Goldenrod);
        DrawPanel(750, 502, 116, 32, new Color(31, 45, 51), Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, "CLOSE", 787, 513, 1, Color.LightGray);
    }

    private void DrawHelp(DishStationSnapshot snapshot)
    {
        DrawModalScrim();
        DrawPanel(72, 42, 880, 516, new Color(15, 29, 36, 252), Color.DeepSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "SHIFT HANDBOOK", 104, 68, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"GUIDANCE  {snapshot.Onboarding.GuidanceMode.ToString().ToUpperInvariant()}   LEVEL {snapshot.Progression.Level}   {Binding(GameInputAction.HelpClose)}  CLOSE", 104, 101, 1, Color.LightGray);
        DrawPanel(802, 74, 116, 38, new Color(31, 45, 51), Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, "CLOSE", 839, 88, 1, Color.LightGray);

        DrawPanel(98, 130, 828, 78, new Color(30, 42, 43, 248), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, "CURRENT OPPORTUNITY", 116, 146, 1, Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, OpportunityFor(snapshot.TutorialStage), 116, 171, 1, Color.White, 104);

        PixelFont.Draw(spriteBatch!, pixel!, "WORK THE STATION", 104, 234, 1, Color.MediumTurquoise);
        PixelFont.Draw(spriteBatch!, pixel!, $"{MovementBindingLabel(),-17}MOVE\n{Binding(GameInputAction.Interact),-17}INTERACT / WORK\n{Binding(GameInputAction.Inspect),-17}INSPECT\nCLICK             SELECT / MOVE", 104, 259, 1, Color.White, 37);
        PixelFont.Draw(spriteBatch!, pixel!, "READ THE SYSTEM", 375, 234, 1, Color.CornflowerBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.JournalToggle),-17}QUEST JOURNAL\n{Binding(GameInputAction.NextLens),-17}CYCLE UNLOCKED LENSES\n{Binding(GameInputAction.ToggleProcessLens),-17}PROCESS EVIDENCE\n{Binding(GameInputAction.ProcessCaptureToggle),-17}START / FINISH CAPTURE\n{Binding(GameInputAction.ProcessEditorToggle),-17}EDIT CAPTURED PROCESS\n{Binding(GameInputAction.AutomationEditorToggle),-17}EDIT WASHER RULE\n{Binding(GameInputAction.SettingsToggle),-17}SETTINGS", 375, 259, 1, Color.White, 42);
        PixelFont.Draw(spriteBatch!, pixel!, "MOVE THE VIEW", 694, 234, 1, new Color(220, 158, 239));
        PixelFont.Draw(spriteBatch!, pixel!, $"MIDDLE-DRAG      PAN\nWHEEL            ZOOM\n{Binding(GameInputAction.CameraReset),-17}CENTER", 694, 259, 1, Color.White, 31);

        DrawPanel(98, 350, 828, 142, new Color(21, 36, 42, 248), Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, "CAPABILITIES AVAILABLE NOW", 116, 366, 1, Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, CapabilityHelp(snapshot), 116, 392, 1, Color.White, 102);
        PixelFont.Draw(spriteBatch!, pixel!, "NEW ACTIONS APPEAR HERE ONLY AFTER THE PROBLEM THAT MAKES THEM USEFUL.", 116, 472, 1, Color.LightGray, 102);

        var tools = DeveloperToolsAvailable
            ? $"{Binding(GameInputAction.DeveloperToggle)}  SANDBOX TOOLS AVAILABLE"
            : "SANDBOX TOOLS UNLOCK AFTER THE FIRST SHIFT";
        PixelFont.Draw(spriteBatch!, pixel!, tools, 104, 520, 1, DeveloperToolsAvailable ? Color.Goldenrod : Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, "HELP EXPLAINS CONTROLS.\nTHE WORLD STILL PROVIDES THE EVIDENCE.", 510, 512, 1, Color.LightGray, 48);
    }

    private string CapabilityHelp(DishStationSnapshot snapshot)
    {
        var progression = snapshot.Progression;
        var lines = new List<string>
        {
            $"{Binding(GameInputAction.ToggleProcessLens)} / {Binding(GameInputAction.NextLens)}  PROCESS AND STATE EVIDENCE",
            $"{Binding(GameInputAction.ProcessCaptureToggle)} / {Binding(GameInputAction.ProcessEditorToggle)}  CAPTURE AND EDIT OWNED PROCESS",
        };
        if (progression.IsUnlocked(CareerCapability.LayoutEditor)) lines.Add($"{Binding(GameInputAction.ConfigureFlowCell)} / {Binding(GameInputAction.TogglePlacement)}  FLOW CELL AND FIXTURE LAYOUT");
        if (progression.IsUnlocked(CareerCapability.KnowledgeLens)) lines.Add($"{Binding(GameInputAction.ToggleNewHire)} / {Binding(GameInputAction.TrainHappyPath)} / {Binding(GameInputAction.TrainRushPriority)}  DELEGATE, TRANSFER, PRIORITIZE");
        if (progression.IsUnlocked(CareerCapability.ExceptionNotebook)) lines.Add($"{Binding(GameInputAction.TrainRareTray)}  RECORD THE DISCOVERED EXCEPTION");
        if (progression.IsUnlocked(CareerCapability.AutomationWorkbench)) lines.Add($"{Binding(GameInputAction.AutomationEditorToggle)}  CREATE OR REFINE THE WASHER RULE");
        if (progression.IsUnlocked(CareerCapability.RuntimeTrace)) lines.Add($"{Binding(GameInputAction.InspectIncident)} / {Binding(GameInputAction.ReplayIncident)}  INSPECT AND RETEST INCIDENT EVIDENCE");
        if (progression.IsUnlocked(CareerCapability.ShiftScorecard)) lines.Add($"{Binding(GameInputAction.ShiftReportToggle)}  FIRST-SHIFT SCORECARD");
        return string.Join('\n', lines.Take(6));
    }

    private string OpportunityFor(DishTutorialStage stage) => stage switch
    {
        DishTutorialStage.RestockFirstDish => "FOLLOW ONE PLATE THROUGH EVERY VISIBLE WORK STATE; CLICK A FIXTURE AGAIN WHEN YOU ARRIVE.",
        DishTutorialStage.EnableDinnerRush => $"AVERY HAS THE FIRST PLATE. {Binding(GameInputAction.ToggleRush)} LETS TESSA OPEN DINNER SERVICE.",
        DishTutorialStage.AwaitServiceShortage => "KEEP WORK MOVING, BUT WATCH THE DISH SERVICE ACTUALLY NEEDS.",
        DishTutorialStage.InspectShortage or DishTutorialStage.ChooseBottleneck => $"{Binding(GameInputAction.ToggleProcessLens)} OPENS QUEUE EVIDENCE. SELECT A STATION AND {Binding(GameInputAction.ConfirmBottleneck)} RECORDS YOUR CONSTRAINT HYPOTHESIS.",
        DishTutorialStage.ImproveLayout => $"{Binding(GameInputAction.ConfigureFlowCell)} TRIES A COMPACT FLOW CELL; {Binding(GameInputAction.TogglePlacement)} OPENS THE FREEFORM LAYOUT SANDBOX.",
        DishTutorialStage.ValidateBottleneck or DishTutorialStage.AwaitValidationDemand => "RUN THE SCARCE DISH THROUGH THE SAME STATES AND WATCH WHETHER SERVICE CONSUMES IT.",
        DishTutorialStage.InviteNewHire or DishTutorialStage.TrainNewHire => $"{Binding(GameInputAction.ToggleNewHire)} BRINGS JULES ON SHIFT; {Binding(GameInputAction.TrainHappyPath)} SHARES THE BASIC FLOW.",
        DishTutorialStage.ObserveNewHire or DishTutorialStage.DocumentGlassPriority or DishTutorialStage.ValidateDelegation => $"WATCH JULES FOLLOW THE SHARED FLOW; {Binding(GameInputAction.TrainRushPriority)} ADDS RAY'S MISSING GLASS PRIORITY.",
        DishTutorialStage.ObserveRareTray or DishTutorialStage.DocumentRareTray or DishTutorialStage.ValidateRareTray => $"WATCH THE UNCOMMON TRAY CONSEQUENCE; {Binding(GameInputAction.TrainRareTray)} RECORDS RAY'S HANDLING EXCEPTION.",
        DishTutorialStage.OfferAutomation => $"{Binding(GameInputAction.AutomationEditorToggle)} OPENS THE RULE EDITOR. ENABLE AND APPLY THE REPORTED-READY RULE.",
        DishTutorialStage.ObserveAutomation => "WATCH YOUR REPORTED-READY RULE RUN, THEN COMPARE THE PANEL WITH DEVON'S PHYSICAL CHECK.",
        DishTutorialStage.InvestigateAutomation or DishTutorialStage.ReplayAutomation => $"{Binding(GameInputAction.InspectIncident)} INSPECTS THE FIRST DIVERGENCE; {Binding(GameInputAction.ReplayIncident)} REPLAYS THE CAPTURED DECISION.",
        DishTutorialStage.RefineAutomation => $"{Binding(GameInputAction.AutomationEditorToggle)} OPENS YOUR RULE. ADD PHYSICAL READY AND APPLY.",
        DishTutorialStage.ValidateAutomation or DishTutorialStage.ValidateRegression => $"CORROBORATE REPORTED READY WITH PHYSICAL STATE; {Binding(GameInputAction.ReplayIncident)} RETESTS THE CAPTURED CASE.",
        DishTutorialStage.ShiftReview => "STAGE CLEAN GLASSES BEFORE AVERY HANDS OVER THE SHIFT.",
        DishTutorialStage.ValidateShift => "KEEP TESSA SUPPLIED THROUGH THREE LIVE SERVICE CHECKS.",
        DishTutorialStage.EpisodeComplete => $"{Binding(GameInputAction.ShiftReportToggle)} OPENS AVERY'S FIRST-SHIFT REPORT.",
        _ => "OBSERVE WHAT CHANGED, THEN CHOOSE AN ACTION THAT COULD AFFECT THE OUTCOME.",
    };

    private void DrawQuestDetail(CareerProgressionSnapshot progression)
    {
        var definition = DishStationFirstHoursContent.Quests[selectedJournalQuest];
        var state = progression.Quest(definition.Id);
        var active = progression.ActiveQuest == definition.Id;
        var statusColor = state.Complete ? Color.LightGreen : active ? Color.Yellow : new Color(125, 140, 145);
        DrawModalScrim();
        DrawPanel(132, 65, 760, 470, new Color(17, 30, 38, 250), statusColor);
        PixelFont.Draw(spriteBatch!, pixel!, $"QUEST {selectedJournalQuest + 1}/{DishStationFirstHoursContent.Quests.Count}   {definition.Title}", 166, 94, 2, Color.White, 62);
        PixelFont.Draw(spriteBatch!, pixel!, state.Complete ? "COMPLETED" : active ? $"ACTIVE  {state.Percent}%" : "LOCKED", 166, 132, 1, statusColor);
        PixelFont.Draw(spriteBatch!, pixel!, $"+{definition.ExperienceReward} XP   {CapabilityLabel(definition.CapabilityReward)}", 520, 132, 1, statusColor, 48);
        var participants = GameplayHudPresenter.QuestParticipants(definition);
        PixelFont.Draw(spriteBatch!, pixel!, $"WITH  {string.Join(" / ", participants.Select(participant => participant.DisplayName))}", 166, 153, 1, Color.Goldenrod, 91);
        PixelFont.Draw(spriteBatch!, pixel!, $"ROLES  {string.Join(", ", participants.Select(participant => participant.Role))}", 166, 169, 1, Color.Goldenrod, 91);

        PixelFont.Draw(spriteBatch!, pixel!, "SITUATION", 166, 190, 1, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, definition.Situation.ToUpperInvariant(), 166, 211, 1, Color.White, 91);
        PixelFont.Draw(spriteBatch!, pixel!, "OUTCOME", 166, 260, 1, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, definition.ObservableOutcome.ToUpperInvariant(), 166, 281, 1, Color.White, 91);
        PixelFont.Draw(spriteBatch!, pixel!, state.Complete ? "WHAT THE CONSEQUENCE REVEALED" : "DISCOVERY RECORD", 166, 331, 1, state.Complete ? Color.LightGreen : Color.Goldenrod);
        var discovery = state.Complete
            ? definition.Discovery.ToUpperInvariant()
            : active
                ? "COMPLETE THE OUTCOME TO RECORD WHAT THE EVIDENCE REVEALS."
                : "THIS RECORD OPENS AFTER THE QUEST BECOMES ACTIVE AND ITS OUTCOME IS OBSERVED.";
        DrawPanel(166, 356, 692, 76, new Color(23, 37, 43), state.Complete ? Color.LightGreen : new Color(90, 104, 109));
        PixelFont.Draw(spriteBatch!, pixel!, discovery, 184, 376, 1, state.Complete ? Color.White : Color.LightGray, 88);

        var timing = state.StartedAtTick < 0
            ? "NOT STARTED"
            : state.Complete
                ? $"COMPLETED AFTER {PaceLabel(state.ElapsedTicks)} OF ACTIVE WORK"
                : $"ACTIVE FOR {PaceLabel(state.ElapsedTicks)}";
        PixelFont.Draw(spriteBatch!, pixel!, timing, 166, 463, 1, Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.JournalPrevious)} / {Binding(GameInputAction.JournalNext)}  CHANGE QUEST", 166, 500, 1, Color.LightGray);
        DrawPanel(620, 486, 116, 34, new Color(58, 52, 27), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, "BACK", 659, 498, 1, Color.Goldenrod);
        DrawPanel(750, 486, 116, 34, new Color(31, 45, 51), Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, "CLOSE", 787, 498, 1, Color.LightGray);
    }

    private void DrawShiftReport(DishStationSnapshot snapshot)
    {
        var progression = snapshot.Progression;
        var report = snapshot.ShiftReport;
        var economy = GameplayHudPresenter.Economy(report.Economy);
        var completed = progression.Quests.Count(quest => quest.Complete);
        DrawModalScrim();
        DrawPanel(58, 38, 908, 524, new Color(14, 28, 34, 252), Color.LightGreen);
        var debrief = FirstShiftNarrativePresenter.Debrief();
        PixelFont.Draw(spriteBatch!, pixel!, debrief.ChapterTitle, 88, 66, 2, Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, $"LEVEL {progression.Level}   XP {progression.Experience}   ACTIVE {PaceLabel(progression.ActivePlayTicks)}   OUTCOMES {completed}/{progression.Quests.Count}", 88, 101, 1, Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, debrief.Summary, 88, 123, 1, Color.LightGray, 112);
        FlushReportBatch();

        DrawPanel(84, 154, 260, 218, new Color(24, 42, 47, 245), Color.MediumTurquoise);
        PixelFont.Draw(spriteBatch!, pixel!, "OPERATING OUTCOME", 102, 172, 1, Color.MediumTurquoise);
        PixelFont.Draw(spriteBatch!, pixel!, $"RELIABILITY   {snapshot.ShiftTrial.Status.ToString().ToUpperInvariant()}\nDEMAND CHECKS {snapshot.ShiftTrial.SuccessfulDemandChecks}/{snapshot.ShiftTrial.TargetDemandChecks}\nATTEMPTS      {snapshot.ShiftTrial.Attempts}\nSHORTAGES     {report.ServiceShortages} HISTORICAL\nDISHES READY  {report.CompletedDishes}", 102, 205, 1, Color.White, 34);
        PixelFont.Draw(spriteBatch!, pixel!, "READINESS IS AN OBSERVED WINDOW,\nNOT A CLAIM THAT FAILURE IS GONE.", 102, 326, 1, Color.LightGray, 34);
        FlushReportBatch();

        DrawPanel(368, 154, 276, 218, new Color(31, 37, 49, 245), Color.CornflowerBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "EVIDENCE RETAINED", 386, 172, 1, Color.CornflowerBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"ROUTE       {report.BaselineRouteSteps} -> {report.ValidatedRouteSteps} STEPS\nFINAL       {report.FinalRouteSteps} STEPS\nWORKER      {report.WorkerActions} ACTIONS\nREWORK      {report.TrayReworkIncidents} OBSERVED\nAUTO STARTS {report.AutomatedStarts}\nINCIDENTS   {report.AutomationIncidents}\nPREVENTED   {report.PreventedUnsafeStarts}", 386, 205, 1, Color.White, 36);
        FlushReportBatch();

        DrawPanel(668, 154, 272, 218, new Color(41, 35, 49, 245), new Color(195, 133, 219));
        PixelFont.Draw(spriteBatch!, pixel!, "SHIFT ECONOMY", 686, 172, 1, new Color(220, 158, 239));
        PixelFont.Draw(spriteBatch!, pixel!, economy.Summary, 686, 194, 1, Color.White, 34);
        PixelFont.Draw(spriteBatch!, pixel!, economy.Details, 686, 222, 1, Color.LightGray, 34);
        FlushReportBatch();

        DrawPanel(84, 394, 856, 124, new Color(20, 35, 38, 248), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, "SHIFT DEBRIEF", 102, 412, 1, Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, string.Join('\n', debrief.Questions.Select((question, index) => $"{index + 1}  {question}")), 102, 442, 1, Color.White, 105);
        DrawPanel(784, 526, 148, 28, new Color(27, 54, 42), Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, "CLOSE REPORT", 808, 536, 1, Color.LightGreen);
    }

    private void FlushReportBatch()
    {
        spriteBatch!.End();
        spriteBatch.Begin(GraphicsContext, UiCanvasTransform());
    }

    private void DrawProgressionToasts(CareerProgressionSnapshot progression)
    {
        if (progressionReceiptSeconds <= 0 || progressionReceiptQuest is not { } completed) return;
        var quest = DishStationFirstHoursContent.Quest(completed);
        DrawPanel(614, 138, 396, 126, new Color(24, 42, 39, 245), Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, progressionReceiptLeveledUp ? $"OUTCOME COMPLETE  /  LEVEL {progressionReceiptLevel}" : "OUTCOME COMPLETE", 632, 151, 1, Color.LightGreen, 52);
        PixelFont.Draw(spriteBatch!, pixel!, quest.Title, 632, 174, 2, Color.White, 43);
        PixelFont.Draw(spriteBatch!, pixel!, $"+{quest.ExperienceReward} XP  /  {progression.Experience} TOTAL", 632, 204, 1, Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, $"UNLOCKED  {CapabilityLabel(quest.CapabilityReward)}", 632, 223, 1, Color.LightSkyBlue, 47);
        PixelFont.Draw(spriteBatch!, pixel!, $"WHY NOW  {quest.UnlockRationale.ToUpperInvariant()}", 632, 242, 1, Color.LightGray, 47);
    }

    private static string CapabilityLabel(CareerCapability capability) => capability switch
    {
        CareerCapability.LayoutEditor => "LAYOUT TOOLS",
        CareerCapability.KnowledgeLens => "KNOWLEDGE LENS",
        CareerCapability.ExceptionNotebook => "EXCEPTION NOTES",
        CareerCapability.AutomationWorkbench => "AUTOMATION",
        CareerCapability.RuntimeTrace => "INCIDENT EVIDENCE",
        CareerCapability.ResponsibilityMap => "OWNERSHIP MAP",
        CareerCapability.ShiftScorecard => "SHIFT SCORECARD",
        _ => "STATE LENS",
    };

    private static string PaceLabel(long ticks)
    {
        var seconds = Math.Max(0, ticks / 10);
        return $"{seconds / 60}:{seconds % 60:00}";
    }

    private void DrawRoom(DishStationSnapshot snapshot)
    {
        if (spriteBatch is null || pixel is null || diamond is null) return;

        renderHighContrast = snapshot.Onboarding.HighContrast || (!snapshot.Onboarding.Complete && selectedHighContrast);
        renderReducedMotion = snapshot.Onboarding.ReducedMotion || (!snapshot.Onboarding.Complete && selectedReducedMotion);
        UpdateCanvasTransform();
        var canvasTransform = Matrix.Scaling(canvasScale, canvasScale, 1) * Matrix.Translation(canvasOffsetX, canvasOffsetY, 0);
        spriteBatch.Begin(GraphicsContext, canvasTransform);
        if (nativeRoom is null) DrawRect(0, 0, VirtualWidth, VirtualHeight, new Color(12, 22, 27));
        IsometricStationScene.Draw(spriteBatch, pixel, diamond, washerProjection, presentationCatalog, nativeRoom is null,
            snapshot, selectedKind, selectedWorkstation,
            activeLens == SystemLens.Process, camera, characterFrame, renderReducedMotion, placementMode, placementFixture,
            placementPreview, IsPlacementPreviewValid(), hoveredFixture, InteractionColor(), InteractionPulse());
        if (screenRouter.Screen == ClientScreen.Gameplay && screenRouter.Modal == ClientModal.None && activeLens is SystemLens.Reality or SystemLens.Process) DrawInteractionCursor();
        spriteBatch.End();

        spriteBatch.Begin(GraphicsContext, UiCanvasTransform());
        DrawGameplayHud(snapshot);
        DrawActiveLens(snapshot);
        if (godMode) DrawGodTools();
        if (benchmarkVisible && benchmarkResult is not null) DrawSyntheticBenchmark(benchmarkResult);
        if (StartMenuVisible)
        {
            DrawStartMenu();
            if (SettingsVisible) DrawSettings();
        }
        else if (BriefingVisible) DrawIntroWizard();
        else
        {
            if (SettingsVisible) DrawSettings();
            else if (HelpVisible) DrawHelp(snapshot);
            else if (ShiftReportVisible) DrawShiftReport(snapshot);
            else if (ProcessEditorVisible) DrawProcessEditor(snapshot.ProcessCapture);
            else if (AutomationEditorVisible) DrawAutomationRuleEditor(snapshot.Automation);
            else if (TwoStationRoutingVisible) DrawTwoStationRouting();
            else if (PatternCodexVisible) DrawPatternCodex();
            else if (VendorComparisonVisible) DrawVendorComparison();
            else if (QuestJournalVisible)
            {
                if (QuestDetailVisible) DrawQuestDetail(snapshot.Progression);
                else DrawQuestJournal(snapshot.Progression);
            }
            if (screenRouter.Modal == ClientModal.None) DrawProgressionToasts(snapshot.Progression);
        }
        spriteBatch.End();
    }

    private void DrawGameplayHud(DishStationSnapshot snapshot)
    {
        DrawPanel(14, 12, 380, 56, new Color(17, 30, 38, 226), Color.DeepSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "DISH STATION", 27, 23, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"LEVEL {snapshot.Progression.Level}  XP {snapshot.Progression.Experience}  {activeLens.ToString().ToUpperInvariant()}  {(paused ? "PAUSED" : "LIVE SHIFT")}", 27, 49, 1, paused ? Color.Yellow : Color.LightGray);
        var xpFraction = snapshot.Progression.NextLevelExperience == 0
            ? 1f
            : (snapshot.Progression.Experience - snapshot.Progression.CurrentLevelExperience) /
              (float)(snapshot.Progression.NextLevelExperience - snapshot.Progression.CurrentLevelExperience);
        DrawRect(296, 51, 82, 5, new Color(43, 58, 63));
        DrawRect(296, 51, 82 * Math.Clamp(xpFraction, 0, 1), 5, Color.LightGreen);

        var economy = GameplayHudPresenter.Economy(snapshot.Economy);
        var economyAccent = snapshot.Economy.NetValue >= 0 ? Color.LightGreen : Color.OrangeRed;
        DrawPanel(410, 12, 264, 56, new Color(17, 30, 38, 226), economyAccent);
        PixelFont.Draw(spriteBatch!, pixel!, "SHIFT VALUE", 424, 23, 1, economyAccent);
        PixelFont.Draw(spriteBatch!, pixel!, economy.Summary, 424, 45, 1, Color.White, 38);

        var (serviceLabel, serviceAccent) = snapshot.ShiftTrial.Status switch
        {
            ShiftTrialStatus.Running => ("RELIABILITY WINDOW", Color.Yellow),
            ShiftTrialStatus.Failed => ("WINDOW FAILED", Color.OrangeRed),
            ShiftTrialStatus.Passed => ("WINDOW PASSED", Color.LightGreen),
            _ when snapshot.ServiceShortages > 0 => ("SERVICE RECORD", Color.Orange),
            _ => ("SERVICE SUPPLIED", Color.LightGreen),
        };
        DrawPanel(690, 12, 320, 72, new Color(17, 30, 38, 226), serviceAccent);
        PixelFont.Draw(spriteBatch!, pixel!, serviceLabel, 704, 23, 1, serviceAccent);
        PixelFont.Draw(spriteBatch!, pixel!, $"DONE {snapshot.Completed}  SHORT {snapshot.ServiceShortages}  RUSH {(snapshot.RushEnabled ? "ON" : "OFF")}", 704, 42, 1, Color.White);
        var serviceDetail = snapshot.ShiftTrial.Status is ShiftTrialStatus.Running or ShiftTrialStatus.Failed or ShiftTrialStatus.Passed
            ? $"WINDOW {snapshot.ShiftTrial.Status.ToString().ToUpperInvariant()}  {snapshot.ShiftTrial.SuccessfulDemandChecks}/{snapshot.ShiftTrial.TargetDemandChecks}  TRY {snapshot.ShiftTrial.Attempts}"
            : $"{LayoutLabel(snapshot.Layout.Layout)} ROUTE {snapshot.Layout.EstimatedRouteSteps}  WALKED {snapshot.Layout.SandboxMovementSteps}";
        PixelFont.Draw(spriteBatch!, pixel!, serviceDetail, 704, 60, 1, Color.LightGray);

        var objectiveColor = snapshot.Progression.ActiveQuest is null ? Color.LightGreen : Color.Yellow;
        DrawPanel(14, 76, 650, 52, new Color(17, 30, 38, 220), objectiveColor);
        if (snapshot.Progression.ActiveQuest is { } activeQuest)
        {
            var quest = DishStationFirstHoursContent.Quest(activeQuest);
            var progress = snapshot.Progression.Quest(activeQuest).Percent;
            PixelFont.Draw(spriteBatch!, pixel!, $"QUEST  {quest.Title}  {progress}%", 27, 86, 1, objectiveColor, 75);
            var prompt = snapshot.Onboarding.GuidanceMode switch
            {
                GuidanceMode.Guided => GameplayHudPresenter.GuidedGoalHint(snapshot.TutorialStage, inputBindings),
                GuidanceMode.Contextual => quest.ObservableOutcome.ToUpperInvariant(),
                _ => "OPEN J FOR THE OUTCOME; READ THE WORLD FOR WHAT CHANGED.",
            };
            PixelFont.Draw(spriteBatch!, pixel!, prompt, 27, 105, 1, Color.White, 100);
        }
        else
        {
            PixelFont.Draw(spriteBatch!, pixel!, "FIRST SHIFT ARC COMPLETE", 27, 88, 1, Color.LightGreen);
            var codexHint = patternKnowledge.For(DishStationPatternContent.Strategy.PatternId).Has(PatternKnowledgeMilestone.Recognized)
                ? $" OR {Binding(GameInputAction.PatternCodexToggle)} FOR CODEX"
                : "";
            var vendorHint = patternKnowledge.For(DishStationPatternContent.Strategy.PatternId).Has(PatternKnowledgeMilestone.Named)
                ? $" OR {Binding(GameInputAction.VendorComparisonToggle)} FOR VENDOR REVIEW"
                : "";
            PixelFont.Draw(spriteBatch!, pixel!, $"PRESS {Binding(GameInputAction.TwoStationRoutingToggle)} TWO STATIONS, {Binding(GameInputAction.ShiftReportToggle)} REPORT{codexHint}{vendorHint}.", 27, 107, 1, Color.White, 100);
        }

        var processHint = snapshot.ProcessCapture.Active is { } activeCapture
            ? $"{Binding(GameInputAction.ProcessCaptureToggle)} FINISH CAPTURE  •  {activeCapture.Steps.Length} STEPS"
            : snapshot.ProcessCapture.Artifacts.Count > 0
                ? $"{Binding(GameInputAction.ProcessCaptureToggle)} CAPTURE AGAIN  •  {Binding(GameInputAction.ProcessEditorToggle)} EDIT PROCESS"
                : $"{Binding(GameInputAction.ProcessCaptureToggle)} CAPTURE MANUAL PROCESS";
        DrawPanel(14, 454, 470, 32, new Color(17, 30, 38, 220), Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, processHint, 28, 465, 1, Color.White, 64);

        if (characterBarkSeconds > 0 && activeCharacterBark is { } bark)
        {
            var barkColor = bark.Priority == CharacterDialoguePriority.Critical ? Color.OrangeRed :
                bark.Priority == CharacterDialoguePriority.Important ? Color.Goldenrod : Color.LightGray;
            DrawPanel(494, 388, 516, 98, new Color(17, 30, 38, 238), barkColor);
            DrawCastBadge(510, 400, bark.Badge);
            PixelFont.Draw(spriteBatch!, pixel!, $"{bark.Speaker}  •  {bark.Role}", 546, 402, 1, barkColor, 64);
            PixelFont.Draw(spriteBatch!, pixel!, bark.Line, 510, 428, 1, Color.White, 67);
        }

        DrawPanel(14, 496, 996, 90, new Color(15, 27, 34, 235), selectedKind == DishKind.Glass ? Color.MediumTurquoise : selectedKind == DishKind.Tray ? Color.Goldenrod : Color.CornflowerBlue);
        var interactionTarget = CurrentInteractionFixture();
        var interactionHud = GameplayHudPresenter.Interaction(
            world.InteractionAt(interactionTarget, selectedKind),
            selectedKind,
            Binding(GameInputAction.Interact),
            Binding(GameInputAction.Inspect),
            MovementBindingLabel());
        PixelFont.Draw(spriteBatch!, pixel!, $"TARGET {interactionHud.Target}  •  {interactionHud.State}", 28, 507, 1, Color.White, 90);
        var toolHint = DeveloperToolsAvailable ? $"  {Binding(GameInputAction.DeveloperToggle)} TOOLS" : "";
        PixelFont.Draw(spriteBatch!, pixel!, $"{interactionHud.ActionPrompt}{toolHint}", 28, 527, 1,
            interactionHud.DisabledReason is null ? Color.LightGreen : Color.LightGray, 78);
        DrawPanel(622, 522, 88, 26, new Color(31, 45, 51), Color.CornflowerBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.SettingsToggle)} SET", 638, 531, 1, Color.CornflowerBlue);
        DrawPanel(716, 522, 88, 26, new Color(31, 45, 51), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.JournalToggle)} QUEST", 729, 531, 1, Color.Goldenrod);
        DrawPanel(810, 522, 88, 26, new Color(31, 45, 51), Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"{PrimaryBinding(GameInputAction.HelpClose)} HELP", 818, 531, 1, Color.LightSkyBlue);
        DrawPanel(904, 522, 88, 26, new Color(31, 45, 51), Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.ShiftReportToggle)} REPORT", 912, 531, 1, Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, interactionHud.DisabledReason ?? (string.IsNullOrWhiteSpace(commandFeedback) ? "READY" : commandFeedback),
            28, 546, 1, interactionHud.DisabledReason is null ? Color.LightGray : Color.OrangeRed, 139);
        if (audioCaptionSeconds > 0)
            PixelFont.Draw(spriteBatch!, pixel!, audioCaption, 28, 557, 1, Color.Goldenrod, 139);
        if (snapshot.LatestNotification is { } note)
        {
            var notification = GameplayHudPresenter.Notification(note);
            PixelFont.Draw(spriteBatch!, pixel!, notification.Text, 28, 570, 1, NotificationColor(notification.Priority), 139);
        }

        if (placementMode) DrawPlacementTools(snapshot);
    }

    private void DrawProcessEditor(ProcessCaptureSnapshot capture)
    {
        if (capture.ActiveEdit is null) return;
        var view = ProcessEditorPresenter.Present(capture, selectedProcessStep);
        DrawPanel(62, 86, 900, 414, new Color(17, 32, 43, 248), view.CanApply ? Color.LightGreen : Color.OrangeRed);
        PixelFont.Draw(spriteBatch!, pixel!, "PROCESS EDITOR", 84, 105, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, $"{view.Name.ToUpperInvariant()}  BASELINE V{view.BaselineVersion}  CURRENT V{view.CurrentVersion}  DRAFT FROM V{view.BasedOnVersion}", 84, 137, 1, Color.White, 120);
        PixelFont.Draw(spriteBatch!, pixel!, $"ROUTING  {view.Routing}", 84, 160, 1, Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, "ORDER  WORKSTATION       ACTION                  TRANSITION                          ASSIGNED", 84, 194, 1, Color.LightGray, 125);
        var y = 220f;
        foreach (var step in view.Steps)
        {
            if (step.Selected) DrawRect(78, y - 5, 854, 31, new Color(48, 73, 88));
            PixelFont.Draw(spriteBatch!, pixel!, $"{step.Sequence,2}     {step.Workstation,-17} {step.Action,-23} {step.Transition,-35} {step.Assignment}", 88, y, 1,
                step.Selected ? Color.Yellow : Color.White, 128);
            y += 35;
        }
        PixelFont.Draw(spriteBatch!, pixel!, view.Validation, 84, 403, 1, view.CanApply ? Color.LightGreen : Color.OrangeRed, 125);
        PixelFont.Draw(spriteBatch!, pixel!,
            $"{Binding(GameInputAction.ProcessEditorPrevious)}/{Binding(GameInputAction.ProcessEditorNext)} SELECT   {Binding(GameInputAction.ProcessEditorMoveUp)}/{Binding(GameInputAction.ProcessEditorMoveDown)} REORDER   {Binding(GameInputAction.ProcessEditorToggleAssignment)} ASSIGN\n{Binding(GameInputAction.ProcessEditorNextRouting)} ROUTING   {Binding(GameInputAction.ProcessEditorApply)} VALIDATE + APPLY   {Binding(GameInputAction.ProcessEditorClose)} DISCARD",
            84, 438, 1, Color.LightGray, 125);
    }

    private void DrawAutomationRuleEditor(AutomationSnapshot automation)
    {
        if (automation.ActiveEdit is null) return;
        var view = AutomationRuleEditorPresenter.Present(automation, selectedAutomationRuleRow);
        DrawPanel(82, 72, 860, 466, new Color(17, 32, 43, 248), view.CanApply ? Color.LightGreen : Color.OrangeRed);
        PixelFont.Draw(spriteBatch!, pixel!, "AUTOMATION RULE EDITOR", 104, 91, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, view.RuleId.ToUpperInvariant(), 104, 124, 1, Color.LightGray, 105);
        var y = 158f;
        foreach (var row in view.Rows)
        {
            if (row.Selected) DrawRect(98, y - 5, 390, 31, new Color(48, 73, 88));
            PixelFont.Draw(spriteBatch!, pixel!, $"{row.Label,-25} {row.Value}", 108, y, 1,
                row.Selected ? Color.Yellow : row.Editable ? Color.White : Color.LightGray, 58);
            y += 39;
        }
        PixelFont.Draw(spriteBatch!, pixel!, "LATEST RULE EVIDENCE", 525, 158, 1, Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, string.Join('\n', view.TraceLines), 525, 186, 1, Color.White, 54);
        PixelFont.Draw(spriteBatch!, pixel!, view.Validation, 104, 405, 1,
            view.CanApply ? Color.LightGreen : Color.OrangeRed, 105);
        PixelFont.Draw(spriteBatch!, pixel!, $"{view.BaselinePreset}\n{view.VariantPreset}", 104, 348, 1, Color.Goldenrod, 58);
        PixelFont.Draw(spriteBatch!, pixel!, string.Join('\n', view.ComparisonLines), 525, 338, 1, Color.White, 54);
        PixelFont.Draw(spriteBatch!, pixel!,
            $"{Binding(GameInputAction.AutomationEditorPrevious)}/{Binding(GameInputAction.AutomationEditorNext)} SELECT   {Binding(GameInputAction.AutomationEditorToggleValue)} CHANGE   {Binding(GameInputAction.AutomationEditorApply)} APPLY   {Binding(GameInputAction.AutomationEditorClose)} DISCARD",
            104, 443, 1, Color.LightGray, 105);
        PixelFont.Draw(spriteBatch!, pixel!,
            $"{Binding(GameInputAction.AutomationEditorSaveBaseline)} SAVE BASELINE   {Binding(GameInputAction.AutomationEditorSaveVariant)} SAVE VARIANT   {Binding(GameInputAction.AutomationEditorRunComparison)} RUN SAME-SEED COMPARE",
            104, 468, 1, Color.LightGray, 105);
        PixelFont.Draw(spriteBatch!, pixel!, "BOTH PRESETS FACE THE SAME STARTING CONDITIONS; COMPARE THE MEASURED CONSEQUENCES.",
            104, 499, 1, Color.LightGray, 105);
    }

    private void DrawTwoStationRouting()
    {
        var view = TwoStationRoutingPresenter.Present(DishStationTwoStationsContent.Configuration,
            twoStationWorld.Snapshot(), DishStationTwoStationsContent.Quest, selectedRoutingStation);
        var accent = view.OutcomeMet ? Color.LightGreen : Color.Goldenrod;
        DrawPanel(54, 62, 916, 478, new Color(17, 32, 43, 250), accent);
        PixelFont.Draw(spriteBatch!, pixel!, view.Title, 78, 82, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, view.Situation, 78, 118, 1, Color.White, 122);
        var x = 78f;
        foreach (var station in view.Stations)
        {
            var stationAccent = station.Selected ? Color.Yellow : Color.CornflowerBlue;
            DrawPanel(x, 176, 416, 176, new Color(24, 43, 54, 245), stationAccent);
            PixelFont.Draw(spriteBatch!, pixel!, station.Name.ToUpperInvariant(), x + 18, 193, 2, stationAccent, 46);
            PixelFont.Draw(spriteBatch!, pixel!, $"DEMAND  {station.Demand}\nSAME DECISION SLOT\nPOLICY  {station.Policy}", x + 18, 231, 1, Color.White, 50);
            var evidence = station.Shortages is null
                ? "NO TRIAL EVIDENCE YET"
                : $"DONE {station.Completed}  SHORT {station.Shortages}  NET {station.NetValue}";
            PixelFont.Draw(spriteBatch!, pixel!, evidence, x + 18, 319, 1,
                station.Shortages == 0 ? Color.LightGreen : station.Shortages is null ? Color.LightGray : Color.OrangeRed, 50);
            x += 438;
        }
        PixelFont.Draw(spriteBatch!, pixel!, view.Evidence.ToUpperInvariant(), 78, 378, 1, accent, 122);
        if (view.OutcomeMet)
        {
            PixelFont.Draw(spriteBatch!, pixel!, "OUTCOME MET", 78, 409, 2, Color.LightGreen);
            PixelFont.Draw(spriteBatch!, pixel!, view.Discovery, 250, 411, 1, Color.White, 94);
        }
        else
            PixelFont.Draw(spriteBatch!, pixel!, $"COPIES {view.CopyCount}  TRIALS {view.TrialCount}", 78, 409, 1, Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!,
            $"{Binding(GameInputAction.TwoStationRoutingPreviousStation)}/{Binding(GameInputAction.TwoStationRoutingNextStation)} STATION   {Binding(GameInputAction.TwoStationRoutingPreviousPolicy)}/{Binding(GameInputAction.TwoStationRoutingNextPolicy)} POLICY   {Binding(GameInputAction.TwoStationRoutingCopy)} COPY MAIN TO PATIO",
            78, 467, 1, Color.LightGray, 122);
        PixelFont.Draw(spriteBatch!, pixel!,
            $"{Binding(GameInputAction.TwoStationRoutingRunTrial)} RUN BOTH STATIONS   {Binding(GameInputAction.TwoStationRoutingClose)} CLOSE",
            78, 495, 1, Color.LightGray, 122);
        if (view.OutcomeMet)
            PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.PatternCodexToggle)} OPEN CODEX RECORD", 680, 495, 1, Color.LightGreen, 40);
    }

    private void DrawPatternCodex()
    {
        var knowledge = patternKnowledge.For(DishStationPatternContent.Strategy.PatternId);
        var view = PatternCodexPresenter.Present(DishStationPatternContent.Strategy, knowledge);
        if (view.Named is { } named)
        {
            DrawNamedPatternCodex(view, named);
            return;
        }
        DrawPanel(48, 48, 928, 504, new Color(14, 29, 39, 252), Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, "PATTERN CODEX  •  LIVED", 72, 67, 1, Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, view.Title, 72, 94, 2, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, view.Status, 72, 128, 1, Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, view.NameStatus, 632, 128, 1, Color.Goldenrod, 42);
        PixelFont.Draw(spriteBatch!, pixel!, view.EvidenceSummary, 72, 151, 1, Color.LightGray, 125);
        var y = 184f;
        foreach (var evidence in view.Evidence)
        {
            var accent = evidence.Milestone == "APPLIED" ? Color.LightGreen : Color.CornflowerBlue;
            DrawPanel(72, y, 880, 132, new Color(24, 43, 54, 245), accent);
            PixelFont.Draw(spriteBatch!, pixel!, evidence.Milestone, 92, y + 15, 2, accent);
            PixelFont.Draw(spriteBatch!, pixel!, evidence.Place, 252, y + 18, 1, Color.White, 88);
            PixelFont.Draw(spriteBatch!, pixel!, $"PROBLEM      {evidence.Problem}", 92, y + 51, 1, Color.LightGray, 116);
            PixelFont.Draw(spriteBatch!, pixel!, $"YOUR MOVE    {evidence.Solution}", 92, y + 75, 1, Color.White, 116);
            PixelFont.Draw(spriteBatch!, pixel!, $"CONSEQUENCE  {evidence.Consequence}", 92, y + 99, 1, Color.Goldenrod, 116);
            y += 142;
        }
        PixelFont.Draw(spriteBatch!, pixel!, view.ReflectionPrompt, 72, 474, 1, Color.LightGray, 108);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.PatternCodexReflect)}  {view.ReflectionAction}",
            72, 520, 1, Color.LightGreen, 72);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.PatternCodexClose)} CLOSE", 824, 520, 1, Color.LightGray, 18);
    }

    private void DrawNamedPatternCodex(PatternCodexView view, PatternCodexNamedView named)
    {
        DrawPanel(48, 36, 928, 528, new Color(14, 29, 39, 252), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, $"PATTERN CODEX  •  {named.Category}", 72, 55, 1, Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, view.Title, 72, 80, 2, Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, view.Status, 72, 113, 1, Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, view.EvidenceSummary, 530, 113, 1, Color.LightGray, 62);

        DrawPanel(72, 137, 880, 58, new Color(24, 43, 54, 245), Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "INTENT", 88, 151, 1, Color.LightSkyBlue);
        PixelFont.Draw(spriteBatch!, pixel!, named.Intent, 166, 151, 1, Color.White, 126);

        DrawPanel(72, 207, 424, 218, new Color(24, 43, 54, 245), Color.CornflowerBlue);
        PixelFont.Draw(spriteBatch!, pixel!, "STRUCTURE", 88, 222, 2, Color.CornflowerBlue);
        var structureY = 257f;
        foreach (var item in named.Structure)
        {
            PixelFont.Draw(spriteBatch!, pixel!, $"• {item}", 88, structureY, 1, Color.White, 62);
            structureY += 48;
        }

        DrawPanel(508, 207, 444, 116, new Color(24, 43, 54, 245), Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, "BENEFITS", 524, 222, 2, Color.LightGreen);
        var benefitY = 256f;
        foreach (var item in named.Benefits)
        {
            PixelFont.Draw(spriteBatch!, pixel!, $"+ {item}", 524, benefitY, 1, Color.White, 65);
            benefitY += 34;
        }

        DrawPanel(508, 335, 444, 126, new Color(24, 43, 54, 245), Color.Goldenrod);
        PixelFont.Draw(spriteBatch!, pixel!, "TRADEOFFS", 524, 350, 2, Color.Goldenrod);
        var costY = 384f;
        foreach (var item in named.Costs)
        {
            PixelFont.Draw(spriteBatch!, pixel!, $"- {item}", 524, costY, 1, Color.White, 70);
            costY += 36;
        }

        DrawPanel(72, 473, 880, 58, new Color(18, 35, 46, 245), Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, "YOUR EVIDENCE", 88, 486, 1, Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, view.Evidence[0].Consequence, 198, 486, 1, Color.LightGray, 92);
        PixelFont.Draw(spriteBatch!, pixel!, view.Evidence[1].Consequence, 198, 510, 1, Color.LightGreen, 92);
        PixelFont.Draw(spriteBatch!, pixel!, "THE NAME DESCRIBES THE SHAPE. YOUR TRIALS EXPLAIN WHY IT MATTERS.",
            72, 540, 1, Color.LightGray, 105);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.PatternCodexClose)} CLOSE", 824, 544, 1, Color.LightGray, 18);
    }

    private void DrawVendorComparison()
    {
        var view = VendorComparisonPresenter.Present(vendorWorld.Configuration, vendorWorld.Snapshot(),
            DishStationVendorContent.Quest);
        DrawPanel(16, 16, 992, 568, new Color(14, 29, 39, 252), Color.CornflowerBlue);
        PixelFont.Draw(spriteBatch!, pixel!, view.Title, 52, 38, 2, Color.LightSkyBlue);
        DrawCastBadge(672, 32, RestaurantCastBadgeCatalog.Resolve("character.recurring.sam-rivera"));
        PixelFont.Draw(spriteBatch!, pixel!, view.Speaker, 708, 42, 1, Color.MediumPurple, 40);
        PixelFont.Draw(spriteBatch!, pixel!, view.Pitch, 52, 80, 1, Color.LightGray, 148);
        PixelFont.Draw(spriteBatch!, pixel!, view.SharedIncident, 52, 119, 1, Color.Goldenrod, 148);
        var x = 52f;
        foreach (var card in view.Proposals)
        {
            var accent = card.Id switch
            {
                VendorProposalId.BuildInHouse => Color.CornflowerBlue,
                VendorProposalId.ManagedVendor => Color.Goldenrod,
                VendorProposalId.ObservableVendor => Color.LightGreen,
                _ => Color.White,
            };
            DrawPanel(x, 145, 288, 198, new Color(24, 43, 54, 245), card.Selected ? accent : new Color(55, 75, 85));
            PixelFont.Draw(spriteBatch!, pixel!, card.Selected ? $"SELECTED  {card.Title}" : card.Title, x + 14, 159, 1, accent, 42);
            PixelFont.Draw(spriteBatch!, pixel!, card.Source, x + 14, 184, 1, Color.LightGray, 42);
            PixelFont.Draw(spriteBatch!, pixel!, card.Boundary, x + 14, 207, 1, Color.White, 42);
            PixelFont.Draw(spriteBatch!, pixel!, card.Contract, x + 14, 230, 1, Color.LightGray, 42);
            PixelFont.Draw(spriteBatch!, pixel!, card.Ownership, x + 14, 264, 1, Color.White, 42);
            PixelFont.Draw(spriteBatch!, pixel!, card.NormalEconomy, x + 14, 287, 1, Color.LightSkyBlue, 42);
            PixelFont.Draw(spriteBatch!, pixel!, card.Risk, x + 14, 310, 1, Color.Goldenrod, 42);
            if (card.IncidentOutcome is { } outcome)
                PixelFont.Draw(spriteBatch!, pixel!, outcome, x + 14, 329, 1,
                    card.Viable == true ? Color.LightGreen : Color.OrangeRed, 42);
            x += 306;
        }

        DrawPanel(52, 357, 900, 142, new Color(18, 35, 46, 245), Color.MediumPurple);
        PixelFont.Draw(spriteBatch!, pixel!, "SELECTED INCIDENT / SUPPORT TRACE", 68, 371, 1, Color.MediumPurple);
        var traceY = 395f;
        if (view.SelectedTrace.Count == 0)
            PixelFont.Draw(spriteBatch!, pixel!, "RUN THE SELECTED PROPOSAL TO OBSERVE ITS CONTRACT BOUNDARY.", 68, traceY, 1, Color.LightGray, 140);
        else foreach (var entry in view.SelectedTrace)
        {
            PixelFont.Draw(spriteBatch!, pixel!, entry, 68, traceY, 1, Color.LightGray, 140);
            traceY += 20;
        }
        PixelFont.Draw(spriteBatch!, pixel!, view.Outcome, 52, 516, 1,
            vendorWorld.Snapshot().ComparedProposalCount >= 2 ? Color.LightGreen : Color.Goldenrod, 148);
        PixelFont.Draw(spriteBatch!, pixel!,
            $"{Binding(GameInputAction.VendorComparisonPrevious)} / {Binding(GameInputAction.VendorComparisonNext)} SELECT   {Binding(GameInputAction.VendorComparisonRunTrial)} RUN SAME INCIDENT   {Binding(GameInputAction.VendorComparisonClose)} CLOSE",
            52, 548, 1, Color.LightGray, 145);
    }

    private void DrawPlacementTools(DishStationSnapshot snapshot)
    {
        var valid = IsPlacementPreviewValid();
        var accent = valid ? Color.LightGreen : Color.OrangeRed;
        DrawPanel(722, 96, 288, 132, new Color(20, 42, 35, 238), accent);
        PixelFont.Draw(spriteBatch!, pixel!, "LAYOUT TOOLS", 738, 108, 2, accent);
        PixelFont.Draw(spriteBatch!, pixel!, $"FIXTURE  {PlacementLabel(placementFixture)}\nCELL     {placementPreview.X},{placementPreview.Y}\nPREVIEW  {(valid ? "VALID" : "BLOCKED")}\nROUTE    {snapshot.Layout.EstimatedRouteSteps} STEPS", 738, 137, 1, Color.White, 34);
        PixelFont.Draw(spriteBatch!, pixel!, $"{Binding(GameInputAction.PlacementPrevious)}/{Binding(GameInputAction.PlacementNext)} ITEM  {Binding(GameInputAction.PlacementLeft)}/{Binding(GameInputAction.PlacementRight)}/{Binding(GameInputAction.PlacementUp)}/{Binding(GameInputAction.PlacementDown)} MOVE\n{Binding(GameInputAction.PlacementConfirm)} PLACE  {Binding(GameInputAction.PlacementUndo)} UNDO\n{Binding(GameInputAction.PlacementReset)} RESET  {Binding(GameInputAction.TogglePlacement)} CLOSE", 882, 137, 1, Color.LightGray, 17);
    }

    private void DrawGodTools()
    {
        if (activeLens is not (SystemLens.Reality or SystemLens.Process))
        {
            DrawPanel(32, 526, 960, 58, new Color(60, 22, 68, 245), new Color(214, 111, 232));
            PixelFont.Draw(spriteBatch!, pixel!, "SANDBOX TOOLS", 48, 539, 2, new Color(232, 151, 245));
            PixelFont.Draw(spriteBatch!, pixel!, DeveloperBindingLegend(), 270, 537, 1, Color.White, 62);
            return;
        }
        DrawPanel(722, placementMode ? 238 : 96, 288, 146, new Color(60, 22, 68, 240), new Color(214, 111, 232));
        var y = placementMode ? 250 : 108;
        PixelFont.Draw(spriteBatch!, pixel!, "SANDBOX TOOLS", 738, y, 2, new Color(232, 151, 245));
        PixelFont.Draw(spriteBatch!, pixel!, DeveloperBindingLegend(true), 738, y + 30, 1, Color.White, 36);
    }

    private void DrawPanel(float x, float y, float width, float height, Color fill, Color accent)
    {
        DrawRect(x + 5, y + 5, width, height, new Color(2, 7, 10, 150));
        DrawRect(x, y, width, height, renderHighContrast ? new Color(2, 6, 8, 252) : fill);
        DrawRect(x, y, renderHighContrast ? 6 : 4, height, accent);
        if (renderHighContrast)
        {
            DrawRect(x, y, width, 2, accent);
            DrawRect(x, y + height - 2, width, 2, accent);
            DrawRect(x + width - 2, y, 2, height, accent);
        }
    }

    private void DrawCastBadge(float x, float y, CastBadgePresentation badge)
    {
        DrawRect(x, y, 28, 24, new Color(7, 14, 18, 245));
        DrawRect(x, y, 4, 24, badge.Color);
        DrawRect(x + 4, y, 24, 2, badge.Color);
        DrawRect(x + 4, y + 22, 24, 2, badge.Color);
        PixelFont.Draw(spriteBatch!, pixel!, badge.Monogram, x + 8, y + 8, 1, badge.Color, 2);
    }

    private void DrawWorkstation(DishStationSnapshot snapshot, int index)
    {
        var station = Workstations[index];
        var bounds = WorkstationBounds(index, snapshot.Layout.Layout);
        DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, station.Color);
        if (activeLens == SystemLens.Process && snapshot.Bottleneck == station.QueueState) DrawBorder(bounds, 4, Color.OrangeRed);
        if (index == selectedWorkstation) DrawBorder(bounds, 5, Color.Yellow);
        if (index == 4 && snapshot.Layout.Layout == DishStationLayout.UShapedCell) DrawBorder(bounds, 3, Color.LightGreen);
        PixelFont.Draw(spriteBatch!, pixel!, station.Name, bounds.X + 12, bounds.Y + 12, 2, Color.White);

        var counts = snapshot.At(station.QueueState);
        if (station.Action == DishAction.StartWasher)
        {
            counts = new(
                counts.Plates + snapshot.At(DishState.Washing).Plates,
                counts.Glasses + snapshot.At(DishState.Washing).Glasses,
                counts.Trays + snapshot.At(DishState.Washing).Trays);
        }
        PixelFont.Draw(spriteBatch!, pixel!, activeLens == SystemLens.Process ? $"{station.QueueState} P {counts.Plates} G {counts.Glasses} T {counts.Trays}" : $"P {counts.Plates} G {counts.Glasses} T {counts.Trays}", bounds.X + 12, bounds.Y + 42, 1, Color.LightGray);
        if (activeLens == SystemLens.Process)
        {
            var metric = snapshot.MetricAt(station.QueueState);
            PixelFont.Draw(spriteBatch!, pixel!, $"P {metric.TotalItemTicks} OLD {metric.OldestAge(selectedKind)} AVG {metric.AverageResidenceTicks(selectedKind)}", bounds.X + 12, bounds.Y + 58, 1, snapshot.Bottleneck == station.QueueState ? Color.OrangeRed : Color.LightGray);
        }
        DrawDishTokens(bounds.X + 12, bounds.Y + 78, counts);
        if (index == selectedWorkstation) PixelFont.Draw(spriteBatch!, pixel!, "YOU", bounds.X + 125, bounds.Y + 96, 1, Color.Yellow);
    }

    private void DrawService(DishStationSnapshot snapshot)
    {
        var bounds = new RectangleF(570, 330, 175, 120);
        DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, new Color(46, 82, 55));
        PixelFont.Draw(spriteBatch!, pixel!, "SERVICE", bounds.X + 12, bounds.Y + 12, 2, Color.White);
        var counts = snapshot.At(DishState.Available);
        PixelFont.Draw(spriteBatch!, pixel!, $"AVAILABLE P {counts.Plates} G {counts.Glasses} T {counts.Trays}", bounds.X + 12, bounds.Y + 44, 1, Color.LightGray);
        DrawDishTokens(bounds.X + 12, bounds.Y + 70, counts);
    }

    private void DrawNewHire(NewHireSnapshot worker)
    {
        var bounds = new RectangleF(40, 330, 265, 120);
        DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, worker.Enabled ? new Color(77, 54, 92) : new Color(54, 49, 59));
        PixelFont.Draw(spriteBatch!, pixel!, $"NEW HIRE A{worker.Id.Value} {(worker.Enabled ? "ACTIVE" : "OFF SHIFT")}", bounds.X + 12, bounds.Y + 12, 2, Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, $"FLOW {(worker.Specification.FlowDocumented ? "Y" : "N")} GLASS {(worker.Specification.RushGlassPriorityDocumented ? "Y" : "N")} TRAY {(worker.Specification.RareTrayHandlingDocumented ? "Y" : "N")}", bounds.X + 12, bounds.Y + 43, 1, Color.LightGray, 38);
        PixelFont.Draw(spriteBatch!, pixel!, $"ACTIONS {worker.ActionsCompleted} P {worker.PlateActions} G {worker.GlassActions} T {worker.TrayActions}", bounds.X + 12, bounds.Y + 67, 1, Color.LightGray, 38);
        PixelFont.Draw(spriteBatch!, pixel!, $"LAST {worker.LastAction?.ToString() ?? "WAITING"} {worker.LastKind?.ToString() ?? ""} REWORK {worker.TrayReworkIncidents}", bounds.X + 12, bounds.Y + 86, 1, worker.OmittedPriorityObserved ? Color.OrangeRed : Color.LightGray, 38);
    }

    private void DrawAutomation(AutomationSnapshot automation)
    {
        var bounds = new RectangleF(315, 330, 245, 120);
        var color = automation.Incidents > 0 && automation.Halted ? new Color(105, 39, 37) : automation.Policy.Enabled ? new Color(42, 75, 91) : new Color(48, 55, 60);
        DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, color);
        PixelFont.Draw(spriteBatch!, pixel!, $"AUTO START {(automation.Policy.Enabled ? "ON" : "OFF")}", bounds.X + 12, bounds.Y + 12, 2, Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, $"REPORTED READY {(automation.ReportedReady ? "YES" : "NO")}  PHYSICAL READY {(automation.PhysicalReady ? "YES" : "NO")}", bounds.X + 12, bounds.Y + 43, 1, automation.ReportedReady != automation.PhysicalReady ? Color.OrangeRed : Color.LightGray, 35);
        PixelFont.Draw(spriteBatch!, pixel!, $"INTERLOCK {(automation.Policy.RequirePhysicalReady ? "ON" : "OFF")} HALTED {(automation.Halted ? "YES" : "NO")}", bounds.X + 12, bounds.Y + 72, 1, Color.LightGray, 35);
        PixelFont.Draw(spriteBatch!, pixel!, $"STARTS {automation.AutomatedStarts} INCIDENTS {automation.Incidents} PREVENTED {automation.PreventedUnsafeStarts}", bounds.X + 12, bounds.Y + 91, 1, Color.LightGray, 35);
    }

    private void DrawActiveLens(DishStationSnapshot snapshot)
    {
        if (activeLens is not (SystemLens.Reality or SystemLens.Process))
            DrawModalScrim();
        switch (activeLens)
        {
            case SystemLens.State: DrawStateLens(snapshot); break;
            case SystemLens.Knowledge: DrawKnowledgeLens(snapshot); break;
            case SystemLens.Automation: DrawAutomationLens(snapshot.Automation); break;
            case SystemLens.Runtime: DrawIncidentTimeline(snapshot.Automation); break;
            case SystemLens.Responsibility: DrawResponsibilityLens(snapshot); break;
        }
    }

    private void DrawSyntheticBenchmark(SyntheticWorkResult result)
    {
        DrawModalScrim();
        DrawLensFrame("10K OF 100K ACTOR PROJECTION", "THE SIMULATION UPDATED 100K ACTORS. THE CLIENT BATCHES A 10K REPRESENTATIVE SUBSET.", Color.MediumTurquoise);
        for (var index = 0; index < result.RepresentativeStates.Length; index++)
        {
            var state = result.RepresentativeStates[index];
            var column = (index + (int)(world.Tick.Value * (state + 1) % 100)) % 100;
            var row = index / 100;
            var color = state switch
            {
                0 => Color.CornflowerBlue,
                1 => Color.MediumTurquoise,
                2 => Color.Goldenrod,
                3 => Color.LightGreen,
                4 => Color.Orange,
                5 => Color.LightSkyBlue,
                _ => Color.White,
            };
            DrawRect(82 + column * 8.5f, 222 + row * 1.95f, 6.5f, 1.35f, color);
        }
        PixelFont.Draw(spriteBatch!, pixel!, $"ACTORS {result.ActorCount}  TICKS {result.Ticks}  TRANSITIONS {result.Transitions}  CHECKSUM {result.Checksum:X16}", 72, 428, 1, Color.White);
    }

    private void DrawLensFrame(string title, string question, Color accent)
    {
        var bounds = new RectangleF(32, 78, 960, 440);
        DrawRect(bounds.X + 8, bounds.Y + 9, bounds.Width, bounds.Height, new Color(2, 7, 10, 190));
        DrawRect(bounds.X, bounds.Y, bounds.Width, bounds.Height, new Color(19, 28, 37, 248));
        DrawBorder(bounds, 4, accent);
        DrawRect(32, 78, 960, 52, new Color(27, 40, 51, 250));
        PixelFont.Draw(spriteBatch!, pixel!, title, 54, 91, 2, accent);
        PixelFont.Draw(spriteBatch!, pixel!, "V NEXT LENS", 865, 98, 1, Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, question, 54, 142, 1, Color.LightGray, 121);
        PixelFont.Draw(spriteBatch!, pixel!, "REALITY   PROCESS   STATE   KNOWLEDGE   AUTOMATION   RUNTIME   RESPONSIBILITY", 54, 177, 1, Color.White, 121);
        DrawRect(54, 195, 916, 2, new Color(accent.R, accent.G, accent.B, 120));
    }

    private void DrawModalScrim() => DrawRect(0, 0, VirtualWidth, VirtualHeight, new Color(3, 8, 12, 205));

    private void DrawStateLens(DishStationSnapshot snapshot)
    {
        DrawLensFrame("STATE LENS", "WHERE IS EACH DISH NOW, WHAT MOVED IT, AND WHICH TRANSITION COMES NEXT?", Color.CornflowerBlue);
        for (var i = 0; i < DishStates.Length; i++)
        {
            var state = DishStates[i];
            var x = 58 + i * 130;
            var count = snapshot.At(state).For(selectedKind);
            var color = count > 0 ? new Color(45, 86, 112) : new Color(42, 48, 55);
            DrawRect(x, 225, 112, 72, color);
            PixelFont.Draw(spriteBatch!, pixel!, StateLabel(state), x + 8, 237, 1, Color.White, 16);
            PixelFont.Draw(spriteBatch!, pixel!, $"{selectedKind.ToString().ToUpperInvariant()} {count}", x + 8, 270, 1, count > 0 ? Color.LightGreen : Color.LightGray);
            if (i < DishStates.Length - 1) DrawRect(x + 112, 257, 18, 6, Color.CornflowerBlue);
        }

        PixelFont.Draw(spriteBatch!, pixel!, $"WASHER REPORTED {(snapshot.Automation.ReportedReady ? "READY" : "NOT READY")}  PHYSICAL {(snapshot.Automation.PhysicalReady ? "READY" : "OCCUPIED")}", 60, 316, 1, snapshot.Automation.ReportedReady != snapshot.Automation.PhysicalReady ? Color.OrangeRed : Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, "RECENT AUTHORITATIVE TRANSITIONS", 60, 342, 1, Color.LightSkyBlue);
        var first = Math.Max(0, snapshot.RecentTransitions.Count - 4);
        var y = 366f;
        for (var i = first; i < snapshot.RecentTransitions.Count; i++)
        {
            var transition = snapshot.RecentTransitions[i];
            PixelFont.Draw(spriteBatch!, pixel!, $"T{transition.Tick.Value,-3} {transition.Kind,-5} {StateLabel(transition.From)} TO {StateLabel(transition.To)}  BY {TransitionCauseLabel(transition.Cause)}", 60, y, 1, Color.White);
            y += 20;
        }
    }

    private void DrawKnowledgeLens(DishStationSnapshot snapshot)
    {
        var appliedProcess = snapshot.ProcessCapture.AppliedArtifactId is { } appliedId
            ? snapshot.ProcessCapture.Artifacts.FirstOrDefault(artifact => artifact.Id == appliedId)
            : null;
        var flowDocumented = snapshot.NewHire.Specification.FlowDocumented || appliedProcess is not null;
        var routingKnowledge = appliedProcess?.Current.RoutingPolicy switch
        {
            ProcessRoutingPolicy.GlassesFirst => "GLASS FIRST",
            ProcessRoutingPolicy.PlatesFirst => "PLATES FIRST",
            ProcessRoutingPolicy.CapturedOrder => "CAPTURED ORDER",
            _ => KnowledgeFlag(snapshot.NewHire.Specification.RushGlassPriorityDocumented),
        };
        DrawLensFrame("KNOWLEDGE LENS", "WHO KNOWS WHICH OPERATING FACTS, AND WHICH ASSUMPTION IS STILL UNSAFE?", new Color(183, 111, 210));
        DrawKnowledgeCard(60, "PLAYER / OBSERVED",
            $"FLOW STATES       KNOWN\nBOTTLENECK        {(snapshot.BottleneckHypothesis is null ? "UNCONFIRMED" : snapshot.BottleneckHypothesis.ToString()!.ToUpperInvariant())}\nSTICKY SIGNAL     {(snapshot.Automation.Incident.Recorded ? "DISCOVERED" : "UNKNOWN")}",
            snapshot.Automation.Incident.Recorded ? Color.LightGreen : Color.Yellow);
        DrawKnowledgeCard(360, $"NEW HIRE A{snapshot.NewHire.Id.Value} / EXPLICIT",
            $"DISH FLOW        {KnowledgeFlag(flowDocumented)}\nROUTING          {routingKnowledge}\nRARE TRAY        {KnowledgeFlag(snapshot.NewHire.Specification.RareTrayHandlingDocumented)}",
            flowDocumented ? Color.LightGreen : Color.Yellow);
        DrawKnowledgeCard(660, "AUTO RULE / ASSUMES",
            $"READY REPORT     TRUSTED\nPHYSICAL STATE   {(snapshot.Automation.Policy.RequirePhysicalReady ? "CORROBORATED" : "OMITTED")}\nMANUAL FALLBACK  RETAINED",
            snapshot.Automation.Policy.RequirePhysicalReady ? Color.LightGreen : Color.OrangeRed);
    }

    private void DrawKnowledgeCard(float x, string title, string content, Color status)
    {
        DrawRect(x, 225, 275, 190, new Color(54, 45, 61));
        PixelFont.Draw(spriteBatch!, pixel!, title, x + 12, 241, 1, Color.White, 38);
        PixelFont.Draw(spriteBatch!, pixel!, content, x + 12, 282, 1, status, 38);
    }

    private void DrawAutomationLens(AutomationSnapshot automation)
    {
        DrawLensFrame("AUTOMATION LENS", "WHICH BOUNDED RESPONSIBILITY DID THE RULE TAKE, AND WHAT REMAINS MANUAL?", Color.DeepSkyBlue);
        DrawAutomationNode(65, "INPUTS", $"RACK PRESENT\nREPORTED {(automation.ReportedReady ? "READY" : "NOT READY")}\nPHYSICAL {(automation.PhysicalReady ? "READY" : "OCCUPIED")}", automation.ReportedReady != automation.PhysicalReady ? Color.OrangeRed : Color.LightGray);
        DrawRect(300, 282, 55, 8, Color.DeepSkyBlue);
        DrawAutomationNode(355, "DECISION RULE", automation.Policy.Enabled
            ? automation.Policy.RequirePhysicalReady ? "REPORT AND\nPHYSICAL READY" : "REPORT READY\nONLY"
            : "AUTOMATION OFF", automation.Policy.RequirePhysicalReady ? Color.LightGreen : Color.Yellow);
        DrawRect(590, 282, 55, 8, Color.DeepSkyBlue);
        DrawAutomationNode(645, "EFFECT", $"START WASHER\nSTARTS {automation.AutomatedStarts}\nHALTED {(automation.Halted ? "YES" : "NO")}", automation.Halted ? Color.OrangeRed : Color.LightGray);
        PixelFont.Draw(spriteBatch!, pixel!, $"BOUNDARY: SOFTWARE OWNS ONLY THE START REQUEST.\nPEOPLE STILL LOAD, RECOVER, MAINTAIN, AND CAN WORK MANUALLY.  INCIDENTS {automation.Incidents}  PREVENTED {automation.PreventedUnsafeStarts}", 65, 391, 1, Color.White, 112);
    }

    private void DrawAutomationNode(float x, string title, string content, Color status)
    {
        DrawRect(x, 235, 235, 125, new Color(37, 64, 78));
        PixelFont.Draw(spriteBatch!, pixel!, title, x + 12, 250, 1, Color.White);
        PixelFont.Draw(spriteBatch!, pixel!, content, x + 12, 282, 1, status, 31);
    }

    private void DrawResponsibilityLens(DishStationSnapshot snapshot)
    {
        DrawLensFrame("RESPONSIBILITY / ARCHITECTURE", "WHICH CAPABILITY OWNS EACH OUTCOME, AND HOW FAR CAN A FAILURE SPREAD?", new Color(231, 184, 72));
        DrawResponsibilityNode(60, "SERVICE", "CONSUMES\nCLEAN DISHES", new Color(46, 82, 55));
        DrawRect(250, 274, 35, 8, Color.Goldenrod);
        DrawResponsibilityNode(285, "DISH STATION", "OWNS FLOW\nAND SUPPLY", new Color(65, 66, 82));
        DrawRect(475, 274, 35, 8, Color.Goldenrod);
        DrawResponsibilityNode(510, "START RULE", "OWNS ONE\nDECISION", new Color(42, 75, 91));
        DrawRect(700, 274, 35, 8, Color.Goldenrod);
        DrawResponsibilityNode(735, "PHYSICAL WASHER", "OWNS CYCLE\nAND OCCUPANCY", new Color(47, 77, 102));
        PixelFont.Draw(spriteBatch!, pixel!, "PEOPLE: YOU DEFINE AND RECOVER THE WORK; JULES FOLLOWS WHAT THE CREW HAS SHARED.\nSTATION: DISHES, DEMAND, AND THE PHYSICAL WASHER DETERMINE THE CONSEQUENCES.", 60, 375, 1, Color.White, 115);
        PixelFont.Draw(spriteBatch!, pixel!, "BLAST RADIUS: STICKY READY CAN MISLEAD AUTO START, BUT CANNOT REWRITE PHYSICAL STATE.\nHALT AND MANUAL FALLBACK CONTAIN THE INCIDENT.", 60, 419, 1, Color.LightGreen, 115);
    }

    private void DrawResponsibilityNode(float x, string title, string content, Color color)
    {
        DrawRect(x, 235, 190, 110, color);
        PixelFont.Draw(spriteBatch!, pixel!, title, x + 10, 250, 1, Color.White, 25);
        PixelFont.Draw(spriteBatch!, pixel!, content, x + 10, 285, 1, Color.LightGray, 25);
    }

    private void SelectNextLens()
    {
        for (var offset = 1; offset <= SystemLenses.Length; offset++)
        {
            var candidate = SystemLenses[((int)activeLens + offset) % SystemLenses.Length];
            if (!IsLensUnlocked(candidate)) continue;
            activeLens = candidate;
            commandFeedback = $"Opened {candidate} lens.";
            UpdateWindowTitle();
            return;
        }
    }

    private bool IsLensUnlocked(SystemLens lens)
    {
        var progression = world.Snapshot().Progression;
        return lens switch
        {
            SystemLens.Reality or SystemLens.Process => true,
            SystemLens.State => progression.IsUnlocked(CareerCapability.StateLens),
            SystemLens.Knowledge => progression.IsUnlocked(CareerCapability.KnowledgeLens),
            SystemLens.Automation => progression.IsUnlocked(CareerCapability.AutomationWorkbench),
            SystemLens.Runtime => world.Snapshot().Automation.Incident.Recorded || progression.IsUnlocked(CareerCapability.RuntimeTrace),
            SystemLens.Responsibility => progression.IsUnlocked(CareerCapability.ResponsibilityMap),
            _ => false,
        };
    }

    private static string KnowledgeFlag(bool known) => known ? "DOCUMENTED" : "MISSING";

    private static string PlacementLabel(DishStationFixture fixture) => fixture switch
    {
        DishStationFixture.DryRestock => "DRY + STOCK",
        _ => fixture.ToString().ToUpperInvariant(),
    };

    private static string LayoutLabel(DishStationLayout layout) => layout switch
    {
        DishStationLayout.UShapedCell => "U-CELL",
        DishStationLayout.Custom => "CUSTOM",
        _ => "LINEAR",
    };

    private static RectangleF WorkstationBounds(int index, DishStationLayout layout) =>
        index == 4 && layout == DishStationLayout.UShapedCell
            ? new RectangleF(755, 330, 175, 120)
            : Workstations[index].Bounds;

    private static string StateLabel(DishState state) => state switch
    {
        DishState.WashedInMachine => "IN MACHINE",
        DishState.CleanWet => "CLEAN WET",
        _ => state.ToString().ToUpperInvariant(),
    };

    private static string TransitionCauseLabel(DishTransitionCause cause) => cause switch
    {
        DishTransitionCause.PlayerWork => "PLAYER",
        DishTransitionCause.NewHireWork => "NEW HIRE",
        DishTransitionCause.Automation => "AUTO RULE",
        DishTransitionCause.WasherCycle => "WASHER",
        DishTransitionCause.ServiceDemand => "SERVICE",
        _ => cause.ToString().ToUpperInvariant(),
    };

    private void DrawIncidentTimeline(AutomationSnapshot automation)
    {
        DrawLensFrame("RUNTIME / INCIDENT TRACE", "FIND THE FIRST DIVERGENCE, REPLAY THE CAPTURED INPUTS, THEN VALIDATE THE GUARD.", Color.Orange);
        PixelFont.Draw(spriteBatch!, pixel!, "D CLOSE   I INSPECT   P REPLAY CAPTURED INPUTS", 60, 203, 1, Color.LightGray);

        if (!automation.Incident.Recorded)
        {
            PixelFont.Draw(spriteBatch!, pixel!, "NO AUTOMATION INCIDENT HAS BEEN CAPTURED.", 60, 235, 1, Color.White);
            return;
        }

        var incident = automation.Incident;
        PixelFont.Draw(spriteBatch!, pixel!, $"CAPTURE T{incident.OccurredAt.Value} {incident.Kind}  REPORTED {(incident.ReportedReady ? "YES" : "NO")}  PHYSICAL {(incident.PhysicalReady ? "YES" : "NO")}", 60, 217, 1, Color.OrangeRed);
        PixelFont.Draw(spriteBatch!, pixel!, "EXPECTED: START ONLY WHEN THE MACHINE CAN ACCEPT A RACK", 535, 217, 1, Color.LightGray, 58);
        PixelFont.Draw(spriteBatch!, pixel!, "OBSERVED: READY REPORT STAYED TRUE\nWHILE THE MACHINE WAS OCCUPIED", 535, 249, 1, Color.OrangeRed, 58);

        var first = Math.Max(0, automation.Trace.Count - 7);
        var y = 249f;
        for (var i = first; i < automation.Trace.Count; i++)
        {
            var entry = automation.Trace[i];
            var policy = entry.Policy.RequirePhysicalReady ? "SAFE" : entry.Policy.Enabled ? "REPORT" : "OFF";
            var lineColor = entry.Outcome is AutomationTraceOutcome.UnsafeStartRequested or AutomationTraceOutcome.ReplayWouldStart
                ? Color.OrangeRed
                : entry.Outcome is AutomationTraceOutcome.UnsafeStartPrevented or AutomationTraceOutcome.ReplayPrevented
                    ? Color.LightGreen
                    : Color.White;
            PixelFont.Draw(spriteBatch!, pixel!, $"T{entry.Tick.Value,-3} {TraceLabel(entry.Outcome),-20} {policy} R{(entry.ReportedReady ? "Y" : "N")} P{(entry.PhysicalReady ? "Y" : "N")}", 60, y, 1, lineColor);
            y += 24;
        }

        PixelFont.Draw(spriteBatch!, pixel!, $"REPLAYS {incident.ReplayCount}  LAST {(incident.HasReplay ? incident.LastReplayWouldStart ? "WOULD START" : "PREVENTED" : "NOT RUN")}  REGRESSION {(incident.RegressionPassed ? "PASS" : "OPEN")}", 535, 313, 1, incident.RegressionPassed ? Color.LightGreen : Color.Yellow, 58);
        PixelFont.Draw(spriteBatch!, pixel!, "THE REPLAY USES THE EXACT RECORDED SIGNAL\nAND PHYSICAL STATE; ONLY THE SELECTED POLICY CHANGES.", 535, 355, 1, Color.LightGray, 58);
    }

    private void DrawDishTokens(float x, float y, DishCounts counts)
    {
        for (var i = 0; i < Math.Min(counts.Plates, 8); i++) DrawRect(x + i * 16, y, 12, 12, Color.CornflowerBlue);
        for (var i = 0; i < Math.Min(counts.Glasses, 8); i++) DrawRect(x + i * 16, y + 20, 12, 12, Color.MediumTurquoise);
        for (var i = 0; i < Math.Min(counts.Trays, 8); i++) DrawRect(x + 135 + i * 8, y + 20, 6, 12, Color.Goldenrod);
    }

    private static string TraceLabel(AutomationTraceOutcome outcome) => outcome switch
    {
        AutomationTraceOutcome.PolicyConfigured => "POLICY CHANGED",
        AutomationTraceOutcome.AutomaticStart => "AUTO START",
        AutomationTraceOutcome.UnsafeStartRequested => "UNSAFE REQUEST",
        AutomationTraceOutcome.UnsafeStartPrevented => "GUARD PREVENTED",
        AutomationTraceOutcome.IncidentInspected => "FIRST DIVERGENCE",
        AutomationTraceOutcome.ReplayWouldStart => "REPLAY REPRODUCED",
        AutomationTraceOutcome.ReplayPrevented => "REPLAY PREVENTED",
        _ => outcome.ToString().ToUpperInvariant(),
    };

    private void DrawFlowArrows(DishStationLayout layout, Color color)
    {
        DrawRect(215, 218, 30, 10, color);
        DrawRect(420, 218, 30, 10, color);
        DrawRect(625, 218, 30, 10, color);
        DrawRect(layout == DishStationLayout.UShapedCell ? 775 : 825, 218, 10, 172, color);
        DrawRect(745, 385, layout == DishStationLayout.UShapedCell ? 10 : 60, 10, color);
    }

    private void DrawBorder(RectangleF bounds, float width, Color color)
    {
        DrawRect(bounds.X, bounds.Y, bounds.Width, width, color);
        DrawRect(bounds.X, bounds.Bottom - width, bounds.Width, width, color);
        DrawRect(bounds.X, bounds.Y, width, bounds.Height, color);
        DrawRect(bounds.Right - width, bounds.Y, width, bounds.Height, color);
    }

    private void SelectWorkstation(int offset)
    {
        selectedWorkstation = (selectedWorkstation + offset + Workstations.Length) % Workstations.Length;
        selectedInteractionFixture = (DishStationFixture)selectedWorkstation;
        commandFeedback = $"Selected {Workstations[selectedWorkstation].Name}. Approach it, then press E to work or F to inspect.";
        UpdateWindowTitle();
    }

    private DishStationFixture CurrentInteractionFixture() => GameplayInteractionResolver.Resolve(
        world.PlayerCell,
        world.Placements,
        selectedInteractionFixture);

    private void PerformContextInteraction()
    {
        var fixture = CurrentInteractionFixture();
        selectedInteractionFixture = fixture;
        if (fixture != DishStationFixture.Service) selectedWorkstation = (int)fixture;
        Execute(new InteractWithDishStationFixtureCommand(world.Tick, fixture, selectedKind));
    }

    private void InspectContextInteraction()
    {
        var fixture = CurrentInteractionFixture();
        selectedInteractionFixture = fixture;
        if (fixture != DishStationFixture.Service) selectedWorkstation = (int)fixture;
        Execute(new InspectDishStationFixtureCommand(world.Tick, fixture, selectedKind));
    }

    private void SelectWorkstationFromMouse()
    {
        var left = Input.IsMouseButtonPressed(MouseButton.Left);
        var right = Input.IsMouseButtonPressed(MouseButton.Right);
        if (!left && !right) return;
        var point = VirtualMousePosition();
        if (placementMode && left)
        {
            var previewTarget = IsometricStationScene.FloorHitTest(point.X, point.Y, camera);
            if (previewTarget is { } target)
            {
                placementPreview = target;
                lastPointerAction = $"PREVIEW:{target.X},{target.Y}";
                ConfirmPlacement();
            }
            return;
        }
        if (right)
        {
            var destination = IsometricStationScene.FloorHitTest(point.X, point.Y, camera);
            if (destination is { } moveTarget)
            {
                RequestClickMovement(moveTarget, "FLOOR");
            }
            return;
        }
        var hit = IsometricStationScene.HitTest(point.X, point.Y, world.Placements, camera,
            presentationCatalog, washerProjection is not null);
        if (hit is not { } fixture)
        {
            var floorTarget = IsometricStationScene.FloorHitTest(point.X, point.Y, camera);
            if (floorTarget is { } target)
            {
                RequestClickMovement(target, "FLOOR");
            }
            return;
        }
        if (fixture == DishStationFixture.Service)
        {
            selectedInteractionFixture = fixture;
            var servicePort = world.Topology.InteractionPort(fixture);
            if (world.PlayerCell != servicePort)
            {
                RequestClickMovement(world.Placements.At(fixture), "Service");
            }
            else
            {
                lastPointerAction = "Service:INSPECT";
                Execute(new InspectDishStationFixtureCommand(world.Tick, fixture, selectedKind));
            }
            return;
        }
        var index = (int)fixture;
        selectedWorkstation = index;
        selectedInteractionFixture = fixture;
        var fixturePort = world.Topology.InteractionPort(fixture);
        if (world.PlayerCell != fixturePort)
        {
            RequestClickMovement(world.Placements.At(fixture), fixture.ToString());
        }
        else
        {
            lastPointerAction = $"{fixture}:WORK";
            Execute(new InteractWithDishStationFixtureCommand(world.Tick, fixture, selectedKind));
        }
    }

    private void RequestClickMovement(FloorCell destination, string label)
    {
        if (clickMovement.Begin(world.PlayerCell, destination, world.Placements))
        {
            lastPointerAction = $"{label}:ROUTE:{clickMovement.PendingSteps}";
            commandFeedback = clickMovement.PendingSteps == 0 ? "Already at that destination." : $"Walking {clickMovement.PendingSteps} steps.";
            clickMovementRepeatRemaining = 0;
        }
        else
        {
            lastPointerAction = $"{label}:BLOCKED";
            commandFeedback = "BLOCKED: No walkable route reaches that destination.";
        }
        UpdateWindowTitle();
    }

    private void HandleMouseCameraInput()
    {
        var changed = false;
        if (Input.IsMouseButtonDown(MouseButton.Middle))
        {
            UpdateCanvasTransform();
            var delta = Input.AbsoluteMouseDelta;
            if (delta.X != 0 || delta.Y != 0)
            {
                camera = GameplayCameraInput.ApplyMiddleDrag(camera, delta.X, delta.Y, canvasScale,
                    clientSettings.CameraSensitivityPercent / 100f);
                changed = true;
            }
        }

        var wheelDelta = Input.MouseWheelDelta;
        if (wheelDelta != 0)
        {
            camera = GameplayCameraInput.ApplyWheel(camera, wheelDelta,
                clientSettings.CameraSensitivityPercent / 100f);
            changed = true;
        }

        if (!changed) return;
        commandFeedback = $"Camera view: pan {camera.OffsetX:0},{camera.OffsetY:0}; zoom {camera.Zoom:0.0}.";
        UpdateWindowTitle();
    }

    private float InteractionPulse() => renderReducedMotion ? 0 : (MathF.Sin(interactionTime * 6f) + 1f) * 2f;

    private void EmitAudio(AudioCueEmission emission)
    {
        audioPresenter?.Play(emission);
        audioCaption = emission.Caption;
        audioCaptionSeconds = emission.Looping ? 2f : 2.75f;
    }

    private void InitializeDialogue(DishStationSnapshot snapshot)
    {
        dialogueRouter.Reset();
        observedNarrativeEvents = snapshot.NarrativeEvents.Count;
        activeCharacterBark = null;
        characterBarkSeconds = 0;
    }

    private void ObserveDialogue(DishStationSnapshot snapshot)
    {
        if (observedNarrativeEvents > snapshot.NarrativeEvents.Count) InitializeDialogue(snapshot);
        while (observedNarrativeEvents < snapshot.NarrativeEvents.Count)
        {
            var narrativeEvent = snapshot.NarrativeEvents[observedNarrativeEvents++];
            if (dialogueRouter.Resolve(narrativeEvent) is not { } bark) continue;
            activeCharacterBark = CharacterDialoguePresenter.Present(bark);
            characterBarkSeconds = bark.Priority == CharacterDialoguePriority.Critical ? 7f : 5f;
        }
    }

    private string InteractionLabel()
    {
        if (placementMode) return IsPlacementPreviewValid() ? "PLACE" : "BLOCKED";
        if (hoveredFixture is { } fixture)
        {
            var interaction = world.InteractionAt(fixture, selectedKind);
            if (!interaction.IsInRange) return "MOVE";
            if (fixture == DishStationFixture.Service) return "INSPECT";
            return interaction.CanWork ? "WORK" : "BLOCKED";
        }
        return IsometricStationScene.FloorHitTest(VirtualMousePosition().X, VirtualMousePosition().Y, camera) is null ? "" : "MOVE";
    }

    private Color InteractionColor() => InteractionLabel() switch
    {
        "WORK" or "PLACE" => Color.LightGreen,
        "BLOCKED" => Color.OrangeRed,
        "INSPECT" => Color.Goldenrod,
        _ => Color.DeepSkyBlue,
    };

    private static Color NotificationColor(HudNotificationPriority priority) => priority switch
    {
        HudNotificationPriority.Ambient => Color.LightGray,
        HudNotificationPriority.Operational => Color.LightSkyBlue,
        HudNotificationPriority.Important => Color.Yellow,
        HudNotificationPriority.Critical => Color.OrangeRed,
        _ => Color.White,
    };

    private void DrawInteractionCursor()
    {
        var point = VirtualMousePosition();
        var label = InteractionLabel();
        if (string.IsNullOrEmpty(label) || point.X < 0 || point.X > VirtualWidth || point.Y < 0 || point.Y > VirtualHeight) return;
        var color = InteractionColor();
        var radius = 9 + InteractionPulse();
        var iconName = label switch
        {
            "WORK" => "hand_point",
            "INSPECT" => "look_a",
            "BLOCKED" => "disabled",
            "PLACE" => "target_round_a",
            _ => "pointer_a",
        };
        if (interactionIcons.TryGetValue(iconName, out var icon))
            spriteBatch!.Draw(icon, new RectangleF(point.X - 10, point.Y - 10, 20, 20), color, Color.Black);
        const float stroke = 2;
        const float arm = 7;
        DrawRect(point.X - radius, point.Y - radius, arm, stroke, color);
        DrawRect(point.X - radius, point.Y - radius, stroke, arm, color);
        DrawRect(point.X + radius - arm, point.Y - radius, arm, stroke, color);
        DrawRect(point.X + radius - stroke, point.Y - radius, stroke, arm, color);
        DrawRect(point.X - radius, point.Y + radius - stroke, arm, stroke, color);
        DrawRect(point.X - radius, point.Y + radius - arm, stroke, arm, color);
        DrawRect(point.X + radius - arm, point.Y + radius - stroke, arm, stroke, color);
        DrawRect(point.X + radius - stroke, point.Y + radius - arm, stroke, arm, color);
        var labelX = Math.Min(point.X + radius + 5, VirtualWidth - 82);
        var labelY = Math.Min(point.Y + radius + 3, VirtualHeight - 22);
        DrawRect(labelX - 3, labelY - 3, 76, 18, new Color(12, 22, 27, 230));
        PixelFont.Draw(spriteBatch!, pixel!, label, labelX, labelY, 1, color, 12);
    }

    private Texture LoadInteractionIcon(string name)
    {
        var assembly = typeof(DishStationGame).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .Single(resource => resource.EndsWith($".{name}.png", StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded cursor asset '{resourceName}' was not found.");
        return Texture.Load(GraphicsDevice, stream, TextureFlags.ShaderResource, GraphicsResourceUsage.Immutable, true);
    }

    private Vector2 VirtualMousePosition()
    {
        UpdateCanvasTransform();
        var mouse = Input.MousePosition;
        var physicalX = mouse.X * GraphicsDevice.Presenter.BackBuffer.Width;
        var physicalY = mouse.Y * GraphicsDevice.Presenter.BackBuffer.Height;
        var point = new ClientCanvasTransform(canvasScale, canvasOffsetX, canvasOffsetY).ToVirtual(physicalX, physicalY);
        return new Vector2(point.X, point.Y);
    }

    private Vector2 UiMousePosition()
    {
        UpdateCanvasTransform();
        var mouse = Input.MousePosition;
        var physicalX = mouse.X * GraphicsDevice.Presenter.BackBuffer.Width;
        var physicalY = mouse.Y * GraphicsDevice.Presenter.BackBuffer.Height;
        var point = new ClientCanvasTransform(uiCanvasScale, uiCanvasOffsetX, uiCanvasOffsetY).ToVirtual(physicalX, physicalY);
        return new Vector2(point.X, point.Y);
    }

    private Matrix UiCanvasTransform() =>
        Matrix.Scaling(uiCanvasScale, uiCanvasScale, 1) * Matrix.Translation(uiCanvasOffsetX, uiCanvasOffsetY, 0);

    private void UpdateCanvasTransform()
    {
        var width = GraphicsDevice.Presenter.BackBuffer.Width;
        var height = GraphicsDevice.Presenter.BackBuffer.Height;
        var worldCanvas = ClientCanvasLayout.Fit(width, height, VirtualWidth, VirtualHeight);
        canvasScale = worldCanvas.Scale;
        canvasOffsetX = worldCanvas.OffsetX;
        canvasOffsetY = worldCanvas.OffsetY;
        var uiCanvas = ClientCanvasLayout.Fit(width, height, VirtualWidth, VirtualHeight, clientSettings.UiScalePercent);
        uiCanvasScale = uiCanvas.Scale;
        uiCanvasOffsetX = uiCanvas.OffsetX;
        uiCanvasOffsetY = uiCanvas.OffsetY;
    }

    private void SelectPlacementFixture(int offset)
    {
        var count = Enum.GetValues<DishStationFixture>().Length;
        placementFixture = (DishStationFixture)(((int)placementFixture + offset + count) % count);
        placementPreview = world.Placements.At(placementFixture);
        commandFeedback = $"Placing {placementFixture}. Move the preview, then press Enter.";
        UpdateWindowTitle();
    }

    private void ShowLockedCapability(CareerCapability capability)
    {
        var quest = DishStationFirstHoursContent.Quests.Single(definition => definition.CapabilityReward == capability);
        commandFeedback = $"LOCKED: complete {quest.Title} to unlock {CapabilityLabel(capability)}.";
        UpdateWindowTitle();
    }

    private void MovePlacementPreview(int x, int y)
    {
        placementPreview = new FloorCell(
            Math.Clamp(placementPreview.X + x, FloorCell.MinimumX, FloorCell.MaximumX),
            Math.Clamp(placementPreview.Y + y, FloorCell.MinimumY, FloorCell.MaximumY));
        commandFeedback = IsPlacementPreviewValid() ? "Placement preview is valid." : "That floor cell is occupied.";
        UpdateWindowTitle();
    }

    private bool IsPlacementPreviewValid() =>
        placementPreview.IsInsideDishStation && !world.Placements.IsOccupied(placementPreview, placementFixture);

    private void ConfirmPlacement()
    {
        if (!placementMode)
        {
            commandFeedback = "Open placement mode with M first.";
            UpdateWindowTitle();
            return;
        }
        if (!IsPlacementPreviewValid())
        {
            commandFeedback = "BLOCKED: another fixture occupies that floor cell.";
            UpdateWindowTitle();
            return;
        }
        var previous = world.Placements.At(placementFixture);
        var result = world.ExecuteNow(new PlaceDishStationFixtureCommand(world.Tick, placementFixture, placementPreview));
        commandFeedback = result.Success ? result.Message : $"BLOCKED: {result.Message}";
        if (result.Success && previous != placementPreview) placementUndo.Push(new(placementFixture, previous));
        UpdateWindowTitle();
    }

    private void UndoPlacement()
    {
        if (!placementUndo.TryPop(out var undo))
        {
            commandFeedback = "Nothing to undo in this placement session.";
            UpdateWindowTitle();
            return;
        }
        Execute(new PlaceDishStationFixtureCommand(world.Tick, undo.Fixture, undo.Cell));
        placementFixture = undo.Fixture;
        placementPreview = undo.Cell;
    }

    private void MoveCamera(float x, float y)
    {
        camera = camera.Pan(x, y);
        commandFeedback = "Camera moved across the sandbox floor.";
        UpdateWindowTitle();
    }

    private void ZoomCamera(float amount)
    {
        camera = camera.ZoomBy(amount);
        commandFeedback = $"Camera zoom {camera.Zoom:0.00}.";
        UpdateWindowTitle();
    }

    private void DrawRect(float x, float y, float width, float height, Color color) =>
        spriteBatch!.Draw(pixel!, new RectangleF(x, y, width, height), color, Color.Black);

    private void UpdateWindowTitle()
    {
        var note = world.Notifications.Count == 0 ? "Clock in — press 1 to scrape" : $"{world.Notifications[^1].Title}: {world.Notifications[^1].Message}";
        var width = GraphicsDevice.Presenter.BackBuffer.Width;
        var height = GraphicsDevice.Presenter.BackBuffer.Height;
        var pointer = hoveredFixture?.ToString() ?? "FLOOR";
        var progression = world.Snapshot().Progression;
        var diagnosticTitle = !string.IsNullOrWhiteSpace(driverControlFile) || developerToolsOptIn || diagnosticTitleOptIn;
        if (!diagnosticTitle)
        {
            Window.Title = FirstShiftNarrativePresenter.WindowTitle(StartMenuVisible, BriefingVisible, progression.ActiveQuest);
            return;
        }
        var menu = StartMenuVisible ? NewCareerConfirmationVisible ? "confirm-new" : startMenuSelection switch
        {
            0 => "continue",
            1 => "new",
            _ => "settings",
        } : "closed";
        var comfort = world.IntroComplete
            ? $"motion={(world.Snapshot().Onboarding.ReducedMotion ? "reduced" : "full")},contrast={(world.Snapshot().Onboarding.HighContrast ? "high" : "standard")}"
            : $"motion={(selectedReducedMotion ? "reduced" : "full")},contrast={(selectedHighContrast ? "high" : "standard")}";
        var trial = world.Snapshot().ShiftTrial;
        var receipt = progressionReceiptSeconds > 0 && progressionReceiptQuest is { } receiptQuest
            ? $"{receiptQuest}:L{progressionReceiptLevel}"
            : "none";
        var routing = twoStationWorld.Snapshot();
        var routingProfile = DishStationTwoStationsContent.Configuration.Stations[selectedRoutingStation];
        var codexKnowledge = patternKnowledge.For(DishStationPatternContent.Strategy.PatternId);
        var vendor = vendorWorld.Snapshot();
        Window.Title = $"The Automation Game — [room={roomPresentationStatus}] [screen={screenRouter.Screen}] [modal={screenRouter.Modal}] [menu={menu}] [save={saveStatus}] [settings={settingsStatus}] [window={clientSettings.WindowMode}] [volume={clientSettings.MasterVolumePercent}] [ui={clientSettings.UiScalePercent}] [cameraSensitivity={clientSettings.CameraSensitivityPercent}] [evidence={playtestEvidenceStatus}] [intro={(world.IntroComplete ? "done" : $"{introPage + 1}/5:{selectedGuidance}")}] [comfort={comfort}] [quest={progression.ActiveQuest?.ToString() ?? "complete"}] [journal={QuestJournalVisible}] [journalQuest={(DishStationQuestId)selectedJournalQuest}] [detail={QuestDetailVisible}] [report={ShiftReportVisible}] [help={HelpVisible}] [level={progression.Level}] [xp={progression.Experience}] [receipt={receipt}] [stage={world.TutorialStage}] [trial={trial.Status}:{trial.SuccessfulDemandChecks}/{trial.TargetDemandChecks}] [lens={activeLens}] [fullscreen={fullscreenPresentation}] [god={godMode}] [tools={(DeveloperToolsAvailable ? "available" : "locked")}] [station={Workstations[selectedWorkstation].Name}] [pointer={pointer}:{InteractionLabel()}] [click={lastPointerAction}] [layout={world.Layout}] [build={placementMode}] [route={world.Placements.EstimatedRouteSteps}] [player={world.PlayerCell.X},{world.PlayerCell.Y}] [zoom={camera.Zoom:0.00}] [cam={camera.OffsetX:0},{camera.OffsetY:0}] [viewport={width}x{height}] [canvas={canvasScale:0.00}] [benchmark={(benchmarkVisible ? "on" : "off")}] [paused={paused}] [tick={world.Tick.Value}] [dirty={world.At(DishState.Dirty).Total}] [routingStation={routingProfile.Id}] [routingPolicy={routing.PolicyFor(routingProfile.Id)}] [routingTrials={routing.Trials.Count}] [routingShortages={routing.LatestTrial?.TotalShortages.ToString() ?? "none"}] [codex={(codexKnowledge.Has(PatternKnowledgeMilestone.Named) ? "named" : codexKnowledge.Has(PatternKnowledgeMilestone.Recognized) ? "recognized" : "locked")}:{codexKnowledge.Evidence.Length}] [vendor={vendor.SelectedProposal}:{vendor.Trials.Count}:{vendor.ComparedProposalCount}] {note}";
    }

    private readonly record struct WorkstationPresentation(
        string Name,
        DishAction Action,
        DishState QueueState,
        RectangleF Bounds,
        Color Color);

    private readonly record struct PlacementUndo(DishStationFixture Fixture, FloorCell Cell);
}

internal enum ClientControl
{
    MenuPrevious,
    MenuNext,
    MenuConfirm,
    MenuBack,
    IntroNext,
    PreviousGuidance,
    NextGuidance,
    ToggleQuestJournal,
    ToggleHelp,
    JournalPrevious,
    JournalNext,
    ToggleQuestDetail,
    JournalBack,
    ToggleShiftReport,
    ToggleSettings,
    SettingsPrevious,
    SettingsNext,
    SettingsDecrease,
    SettingsIncrease,
    SettingsConfirm,
    SettingsReset,
    PreviousWorkstation,
    NextWorkstation,
    ContextWork,
    ContextInteract,
    ContextInspect,
    Scrape,
    Rack,
    StartWasher,
    Unload,
    DryAndRestock,
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
    ToggleIncidentLens,
    NextLens,
    StartShiftTrial,
    ToggleProcessLens,
    ToggleProcessCapture,
    ToggleProcessEditor,
    ProcessEditorPrevious,
    ProcessEditorNext,
    ProcessEditorMoveUp,
    ProcessEditorMoveDown,
    ProcessEditorToggleAssignment,
    ProcessEditorNextRouting,
    ProcessEditorApply,
    ProcessEditorClose,
    ToggleAutomationEditor,
    AutomationEditorPrevious,
    AutomationEditorNext,
    AutomationEditorToggleValue,
    AutomationEditorApply,
    AutomationEditorClose,
    AutomationEditorSaveBaseline,
    AutomationEditorSaveVariant,
    AutomationEditorRunComparison,
    ToggleTwoStationRouting,
    TwoStationRoutingPreviousStation,
    TwoStationRoutingNextStation,
    TwoStationRoutingPreviousPolicy,
    TwoStationRoutingNextPolicy,
    TwoStationRoutingCopy,
    TwoStationRoutingRunTrial,
    TwoStationRoutingClose,
    TogglePatternCodex,
    PatternCodexReflect,
    PatternCodexClose,
    ToggleVendorComparison,
    VendorComparisonPrevious,
    VendorComparisonNext,
    VendorComparisonRunTrial,
    VendorComparisonClose,
    ToggleGodMode,
    GodAddDirty,
    GodSetCleanSupply,
    GodReset,
    GodTogglePause,
    GodStep,
    GodStickyReady,
    GodToggleLayout,
    GodToggleBenchmark,
    GodQuickSave,
    GodQuickLoad,
    CameraPanLeft,
    CameraPanRight,
    CameraPanUp,
    CameraPanDown,
    CameraZoomIn,
    CameraZoomOut,
    CameraReset,
    TogglePlacementMode,
    PreviousPlacementFixture,
    NextPlacementFixture,
    PlacementLeft,
    PlacementRight,
    PlacementUp,
    PlacementDown,
    ConfirmPlacement,
    UndoPlacement,
    ResetSandboxLayout,
    Exit,
}

internal enum SystemLens
{
    Reality,
    Process,
    State,
    Knowledge,
    Automation,
    Runtime,
    Responsibility,
}
