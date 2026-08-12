using Automation.Client.Stride;
using Automation.Content;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class GameplayHudPresenterTests
{
    [Fact]
    public void FirstTaskHudShowsTargetStateActionAndGoalFromAuthoritativeWorld()
    {
        var world = IntegrationTestScenario.World();
        var interaction = world.InteractionAt(DishStationFixture.Scrape, DishKind.Plate);

        var hud = GameplayHudPresenter.Interaction(interaction, DishKind.Plate, "E", "F", "W A S D");
        var goal = GameplayHudPresenter.GuidedGoalHint(world.TutorialStage, InputBindingProfile.Default);

        Assert.Equal("SCRAPE", hud.Target);
        Assert.Equal("DIRTY PLATE 6", hud.State);
        Assert.Equal("E  SCRAPE    F  INSPECT", hud.ActionPrompt);
        Assert.Null(hud.DisabledReason);
        Assert.Equal("RESTOCK ONE CLEAN PLATE", goal);
    }

    [Fact]
    public void MissingRequiredDishHasConcreteDisabledReason()
    {
        var world = IntegrationTestScenario.World();
        MoveToInteractionPort(world, DishStationFixture.Rack);

        var hud = GameplayHudPresenter.Interaction(
            world.InteractionAt(DishStationFixture.Rack, DishKind.Plate),
            DishKind.Plate,
            "E",
            "F",
            "W A S D");

        Assert.Equal("SCRAPED PLATE 0", hud.State);
        Assert.Equal("UNAVAILABLE • NO SCRAPED PLATE READY", hud.DisabledReason);
        Assert.Contains("E  RACK", hud.ActionPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void OutOfRangeTargetExplainsDistanceAndMovementInsteadOfOfferingWork()
    {
        var world = IntegrationTestScenario.World();

        var hud = GameplayHudPresenter.Interaction(
            world.InteractionAt(DishStationFixture.Service, DishKind.Glass),
            DishKind.Glass,
            "E",
            "F",
            "W A S D");

        Assert.Equal("SERVICE", hud.Target);
        Assert.EndsWith("STEPS AWAY", hud.State, StringComparison.Ordinal);
        Assert.Equal("W A S D  MOVE CLOSER", hud.ActionPrompt);
        Assert.StartsWith("OUT OF RANGE", hud.DisabledReason, StringComparison.Ordinal);
        Assert.DoesNotContain("E  ", hud.ActionPrompt, StringComparison.Ordinal);
    }

    [Fact]
    public void ServiceInRangeOffersInspectionOnlyWithSupplyState()
    {
        var world = IntegrationTestScenario.World();
        MoveToInteractionPort(world, DishStationFixture.Service);

        var hud = GameplayHudPresenter.Interaction(
            world.InteractionAt(DishStationFixture.Service, DishKind.Glass),
            DishKind.Glass,
            "E",
            "F",
            "W A S D");

        Assert.Equal("GLASS AVAILABLE 0", hud.State);
        Assert.Equal("F  INSPECT SUPPLY", hud.ActionPrompt);
        Assert.Null(hud.DisabledReason);
    }

    [Fact]
    public void GuidedGoalUsesRemappedLogicalBinding()
    {
        var bindings = InputBindingProfile.Default.WithBinding(GameInputAction.ToggleRush, KeyboardKey.Digit5);

        var goal = GameplayHudPresenter.GuidedGoalHint(DishTutorialStage.EnableDinnerRush, bindings);

        Assert.Equal("LET TESSA OPEN DINNER SERVICE WITH 5", goal);
    }

    [Theory]
    [InlineData("Work has state", HudNotificationPriority.Ambient)]
    [InlineData("Washer started", HudNotificationPriority.Operational)]
    [InlineData("Clock In", HudNotificationPriority.Important)]
    [InlineData("Washer start stopped", HudNotificationPriority.Critical)]
    public void NotificationHierarchyIsVisibleAndDeterministic(string title, HudNotificationPriority expected)
    {
        var notification = GameplayHudPresenter.Notification(
            new WorldNotification(new SimulationTick(12), title, "Evidence changed."));

        Assert.Equal(expected, notification.Priority);
        Assert.StartsWith(expected.ToString().ToUpperInvariant(), notification.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void QuestParticipantsResolveToNamedPeopleAndRoles()
    {
        var quest = DishStationFirstHoursContent.Quest(DishStationQuestId.TransferTheWork);

        var participants = GameplayHudPresenter.QuestParticipants(quest);

        Assert.Equal(
            ["AVERY CHEN", "JULES MARTIN", "RAY MORALES"],
            participants.Select(participant => participant.DisplayName));
        Assert.Equal("SHIFT MANAGER", participants[0].Role);
        Assert.Equal("NEW HIRE", participants[1].Role);
        Assert.Equal("VETERAN BOH WORKER", participants[2].Role);
    }

    [Fact]
    public void EconomyProjectionExplainsLiveValueAndEveryScorecardCostCause()
    {
        var economy = new DishStationEconomySnapshot(
            4, 6, 10, 25, 1, 2, 3, true,
            480, 30, 25, 35, 160, 360, 520, 180, 790, -310);

        var presentation = GameplayHudPresenter.Economy(economy);

        Assert.Equal("VALUE 480  COST 790  NET -310", presentation.Summary);
        Assert.Contains("LABOR 10 / 30", presentation.Details, StringComparison.Ordinal);
        Assert.Contains("STAFF 25 / 25", presentation.Details, StringComparison.Ordinal);
        Assert.Contains("REWORK 1 / 35", presentation.Details, StringComparison.Ordinal);
        Assert.Contains("SHORT 2 / 160", presentation.Details, StringComparison.Ordinal);
        Assert.Contains("INCIDENT 3 / 360", presentation.Details, StringComparison.Ordinal);
        Assert.Contains("FLOW CELL YES / 180", presentation.Details, StringComparison.Ordinal);
        Assert.Contains("NET VALUE -310", presentation.Details, StringComparison.Ordinal);
    }

    private static void MoveToInteractionPort(DishStationWorld world, DishStationFixture fixture)
    {
        var path = world.Topology.FindPath(world.PlayerCell, world.Topology.InteractionPort(fixture));
        Assert.NotEmpty(path);
        foreach (var cell in path.Skip(1))
            Assert.True(world.ExecuteNow(new MovePlayerCommand(world.Tick, cell)).Success);
    }
}
