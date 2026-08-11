using Automation.Domain;

namespace Automation.Content;

public sealed record ScenarioOutcomeCondition(
    string Id,
    string PlayerFacingOutcome);

public sealed record ScenarioDiscoveryCondition(
    string Id,
    string PlayerFacingClue,
    string CausalEvidence);

public sealed record DishStationEpisodeDefinition(
    string Id,
    string DisplayName,
    string StartingSituation,
    IReadOnlyList<ScenarioOutcomeCondition> Outcomes,
    IReadOnlyList<ScenarioDiscoveryCondition> Discoveries)
{
    public static DishStationEpisodeDefinition FirstPlayable { get; } = new(
        "dish-station-first-playable",
        "THE AUTOMATION GAME / DISH STATION",
        "A dinner rush is approaching. Dirty dishes, a constrained washer, and incomplete operating knowledge share one small station.",
        [
            new("service-restored", "Service receives a clean dish produced through the observed process."),
            new("route-improved", "The same dish outcome requires less handling travel after the layout change."),
            new("knowledge-transferred", "Delegated work handles rush priority and the rare tray without repeating discovered rework."),
            new("incident-explained", "The first divergence between reported and physical readiness is causally explained."),
            new("regression-proved", "The corrected policy rejects the exact inputs that caused the unsafe request."),
        ],
        [
            new("glass-shortage", "Service waits even while total dish work continues.", "Queue age and pressure reveal where glasses are waiting."),
            new("priority-gap", "The new hire chooses valid work that does not relieve the urgent shortage.", "The transferred process contains flow but no rush priority."),
            new("rare-tray-gap", "An uncommon tray returns to dirty work after ordinary handling.", "Its orientation fact is absent from the explicit process."),
            new("sticky-ready", "Ready remains visible while a clean rack still occupies the washer.", "The runtime trace preserves reported and physical readiness at the unsafe request."),
        ]);
}

public sealed record DishStationQuestDefinition(
    DishStationQuestId Id,
    string Title,
    string Situation,
    string ObservableOutcome,
    string Discovery,
    string UnlockRationale,
    int ExperienceReward,
    CareerCapability CapabilityReward);

public static class DishStationFirstHoursContent
{
    public static IReadOnlyList<DishStationQuestDefinition> Quests { get; } =
    [
        Quest(DishStationQuestId.ClockIn, "CLOCK IN",
            "Service needs one clean plate from an unfamiliar station.",
            "Return a plate to service through the complete physical flow.",
            "Work accumulates in stages even before anyone names the process.",
            "You have seen work change state."),
        Quest(DishStationQuestId.FindTheConstraint, "WHERE DID THE GLASSES GO?",
            "Dinner demand drains clean glasses while unfinished work remains elsewhere.",
            "Use queue evidence to identify the state constraining glass supply.",
            "Total activity can rise while the resource customers need remains stuck.",
            "Queue evidence exposed handoff cost."),
        Quest(DishStationQuestId.ImproveTheFlow, "DINNER RUSH",
            "The handoff route consumes time every time work changes stations.",
            "Shorten the route and prove that service receives the scarce dish.",
            "A local change matters only when the system outcome improves.",
            "Better flow exposed unwritten work."),
        Quest(DishStationQuestId.TransferTheWork, "THE NEW HIRE",
            "Another worker must keep service supplied without sharing your unstated priorities.",
            "Transfer enough knowledge for delegated work to restore a rush glass.",
            "Doing work and specifying work are different capabilities.",
            "Delegation revealed an omission."),
        Quest(DishStationQuestId.CaptureTheException, "THE RARE TRAY",
            "An uncommon tray returns to dirty work after ordinary handling.",
            "Explain the rework and update the shared process so the tray completes.",
            "Rare conditions are still part of the real contract.",
            "The real exception now bounds a rule."),
        Quest(DishStationQuestId.InvestigateTheSignal, "IT SAID IT WAS READY",
            "Automatic starts trust a readiness report that can disagree with the machine.",
            "Find the first divergence and reproduce the unsafe decision from captured evidence.",
            "Reported state is knowledge about reality, not reality itself.",
            "A failed signal created a trace."),
        Quest(DishStationQuestId.ProveTheFix, "PROVE THE FIX",
            "The corrected start policy must survive the exact condition that caused the incident.",
            "Replay the captured inputs and prove the unsafe request is rejected.",
            "A repeatable test is stronger evidence than another happy-path run.",
            "The fix needs an owned live proof."),
        Quest(DishStationQuestId.OwnTheShift, "OWN THE SHIFT",
            "The individual fixes must now hold together while real service demand continues.",
            "Complete three demand checks without a new shortage or unsafe automation incident.",
            "A system is ready when outcomes survive operation, not when its parts work alone.",
            "The whole shift produced evidence."),
    ];

    public static DishStationQuestDefinition Quest(DishStationQuestId id) => Quests.Single(quest => quest.Id == id);

    private static DishStationQuestDefinition Quest(DishStationQuestId id, string title, string situation,
        string outcome, string discovery, string unlockRationale) => new(id, title, situation, outcome, discovery, unlockRationale,
        DishStationProgressionRules.ExperienceReward(id), DishStationProgressionRules.CapabilityReward(id));
}
