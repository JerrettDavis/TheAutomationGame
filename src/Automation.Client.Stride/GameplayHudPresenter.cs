using Automation.Content;
using Automation.Domain;
using Automation.Simulation;
using System.Text;

namespace Automation.Client.Stride;

public enum HudNotificationPriority
{
    Ambient,
    Operational,
    Important,
    Critical,
}

public readonly record struct InteractionHudPresentation(
    string Target,
    string State,
    string ActionPrompt,
    string? DisabledReason);

public readonly record struct HudNotificationPresentation(
    HudNotificationPriority Priority,
    string Text);

public readonly record struct QuestParticipantPresentation(
    ContentId Id,
    string DisplayName,
    string Role);

public readonly record struct EconomyHudPresentation(
    string Summary,
    string Details);

public static class GameplayHudPresenter
{
    public static InteractionHudPresentation Interaction(
        DishStationInteractionState interaction,
        DishKind kind,
        string interactBinding,
        string inspectBinding,
        string movementBindings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(interactBinding);
        ArgumentException.ThrowIfNullOrWhiteSpace(inspectBinding);
        ArgumentException.ThrowIfNullOrWhiteSpace(movementBindings);

        var target = FixtureLabel(interaction.Fixture);
        if (!interaction.IsInRange)
        {
            var steps = interaction.Distance == 1 ? "STEP" : "STEPS";
            return new(target, $"{interaction.Distance} {steps} AWAY", $"{movementBindings}  MOVE CLOSER",
                $"OUT OF RANGE • {interaction.Distance} FLOOR {steps} TO {target}");
        }

        if (interaction.Fixture == DishStationFixture.Service)
        {
            return new(target, $"{kind.ToString().ToUpperInvariant()} AVAILABLE {interaction.SelectedDishCount}",
                $"{inspectBinding}  INSPECT SUPPLY", null);
        }

        var action = ActionLabel(interaction.WorkAction!.Value);
        var required = StateLabel(interaction.RequiredState!.Value);
        var dish = kind.ToString().ToUpperInvariant();
        var state = $"{required} {dish} {interaction.SelectedDishCount}";
        var prompt = $"{interactBinding}  {action}    {inspectBinding}  INSPECT";
        return new(target, state, prompt, DisabledReason(interaction, dish, required));
    }

    public static string GuidedGoalHint(
        DishTutorialStage stage,
        InputBindingProfile bindings,
        DishStationFirstShiftNarrative? narrative = null)
    {
        ArgumentNullException.ThrowIfNull(bindings);
        var step = (narrative ?? DishStationFirstHoursContent.Narrative).Step(Token(stage.ToString()));
        if (step.InputAction is null) return step.Text;
        var action = Enum.GetValues<GameInputAction>().SingleOrDefault(candidate =>
            string.Equals(Token(candidate.ToString()), step.InputAction, StringComparison.Ordinal));
        if (!Enum.IsDefined(action) || !string.Equals(Token(action.ToString()), step.InputAction, StringComparison.Ordinal))
            throw new InvalidDataException($"Unknown first-shift input action '{step.InputAction}' for tutorial step '{step.Id}'.");
        return step.Text.Replace("{binding}", bindings.DisplayName(action), StringComparison.Ordinal);
    }

    public static HudNotificationPresentation Notification(WorldNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var priority = NotificationPriority(notification.Title);
        return new(priority,
            $"{priority.ToString().ToUpperInvariant()} • {notification.Title.ToUpperInvariant()}: {notification.Message}");
    }

    public static IReadOnlyList<QuestParticipantPresentation> QuestParticipants(
        DishStationQuestDefinition quest,
        CompiledContentCatalogV1? catalog = null)
    {
        ArgumentNullException.ThrowIfNull(quest);
        var available = (catalog ?? DishStationFirstHoursContent.Catalog).Characters.ToDictionary(character => character.Id);
        return quest.Participants.Select(id => available.TryGetValue(id, out var character)
                ? new QuestParticipantPresentation(id, character.DisplayName.ToUpperInvariant(),
                    Label(character.Role.Value[(character.Role.Value.LastIndexOf('.') + 1)..]))
                : throw new InvalidDataException($"Quest '{quest.ContentId}' references missing participant '{id}'."))
            .ToArray();
    }

    public static EconomyHudPresentation Economy(DishStationEconomySnapshot economy) => new(
        $"VALUE {economy.ThroughputValue}  COST {economy.TotalCost}  NET {Signed(economy.NetValue)}",
        $"LABOR {economy.LaborTicks} / {economy.LaborCost}\n" +
        $"STAFF {economy.StaffedTicks} / {economy.StaffingCost}\n" +
        $"REWORK {economy.ReworkIncidents} / {economy.WasteCost}\n" +
        $"SHORT {economy.ServiceShortages} / {economy.ShortageDowntimeCost}\n" +
        $"INCIDENT {economy.AutomationIncidents} / {economy.IncidentDowntimeCost}\n" +
        $"FLOW CELL {(economy.FlowCellInvested ? "YES" : "NO")} / {economy.InvestmentCost}\n" +
        $"TOTAL COST {economy.TotalCost}\nNET VALUE {Signed(economy.NetValue)}");

    private static string? DisabledReason(DishStationInteractionState interaction, string dish, string required) =>
        interaction.WorkBlockReason switch
        {
            DishStationInteractionBlockReason.None => null,
            DishStationInteractionBlockReason.NoDishReady => $"UNAVAILABLE • NO {required} {dish} READY",
            DishStationInteractionBlockReason.RackFull => "UNAVAILABLE • RACK FULL",
            DishStationInteractionBlockReason.WasherRunning => "UNAVAILABLE • WASHER RUNNING",
            DishStationInteractionBlockReason.WasherNeedsUnload => "UNAVAILABLE • UNLOAD CLEAN DISH FIRST",
            _ => "UNAVAILABLE",
        };

    private static HudNotificationPriority NotificationPriority(string title) => title switch
    {
        "Washer start stopped" or "Shift handoff interrupted" => HudNotificationPriority.Critical,
        "Work has state" or "Dish available" or "Observation recorded" or "Layout changed" or
            "Layout configured" or "God mode" or "Scenario reset" => HudNotificationPriority.Ambient,
        "Action blocked" or "Cycle complete" or "Washer started" or "Ready for the machine" or
            "Drying area" or "Dinner service" or "Service paused" or "Service is waiting" or
            "Automatic Start" or "Devon's check held" => HudNotificationPriority.Operational,
        _ => HudNotificationPriority.Important,
    };

    private static string FixtureLabel(DishStationFixture fixture) => fixture switch
    {
        DishStationFixture.DryRestock => "DRY + RESTOCK",
        _ => fixture.ToString().ToUpperInvariant(),
    };

    private static string ActionLabel(DishAction action) => action switch
    {
        DishAction.StartWasher => "START WASHER",
        DishAction.DryAndRestock => "DRY + RESTOCK",
        _ => action.ToString().ToUpperInvariant(),
    };

    private static string StateLabel(DishState state) => state switch
    {
        DishState.WashedInMachine => "WASHED IN MACHINE",
        DishState.CleanWet => "CLEAN WET",
        _ => state.ToString().ToUpperInvariant(),
    };

    private static string Label(string value) => value.Replace('.', ' ').Replace('-', ' ').ToUpperInvariant();

    private static string Signed(int value) => value > 0 ? $"+{value}" : value.ToString();

    private static string Token(string value)
    {
        var result = new StringBuilder(value.Length + 8);
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            if (index > 0 && char.IsUpper(character)) result.Append('-');
            result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }
}
