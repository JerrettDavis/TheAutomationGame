namespace Automation.Domain;

public enum GuidanceMode
{
    Guided,
    Contextual,
    Minimal,
}

public enum DishStationQuestId
{
    ClockIn,
    FindTheConstraint,
    ImproveTheFlow,
    TransferTheWork,
    CaptureTheException,
    InvestigateTheSignal,
    ProveTheFix,
    OwnTheShift,
}

public enum CareerCapability
{
    StateLens,
    LayoutEditor,
    KnowledgeLens,
    ExceptionNotebook,
    AutomationWorkbench,
    RuntimeTrace,
    ResponsibilityMap,
    ShiftScorecard,
}

public static class DishStationProgressionRules
{
    public const int MaximumLevel = 7;

    public static int ExperienceReward(DishStationQuestId quest) => quest switch
    {
        DishStationQuestId.ClockIn => 100,
        DishStationQuestId.FindTheConstraint => 200,
        DishStationQuestId.ImproveTheFlow => 300,
        DishStationQuestId.TransferTheWork => 300,
        DishStationQuestId.CaptureTheException => 400,
        DishStationQuestId.InvestigateTheSignal => 500,
        DishStationQuestId.ProveTheFix => 700,
        DishStationQuestId.OwnTheShift => 900,
        _ => 0,
    };

    public static CareerCapability CapabilityReward(DishStationQuestId quest) => quest switch
    {
        DishStationQuestId.ClockIn => CareerCapability.StateLens,
        DishStationQuestId.FindTheConstraint => CareerCapability.LayoutEditor,
        DishStationQuestId.ImproveTheFlow => CareerCapability.KnowledgeLens,
        DishStationQuestId.TransferTheWork => CareerCapability.ExceptionNotebook,
        DishStationQuestId.CaptureTheException => CareerCapability.AutomationWorkbench,
        DishStationQuestId.InvestigateTheSignal => CareerCapability.RuntimeTrace,
        DishStationQuestId.ProveTheFix => CareerCapability.ResponsibilityMap,
        DishStationQuestId.OwnTheShift => CareerCapability.ShiftScorecard,
        _ => throw new ArgumentOutOfRangeException(nameof(quest)),
    };

    public static int LevelForExperience(int experience) => experience switch
    {
        < 100 => 1,
        < 300 => 2,
        < 600 => 3,
        < 1_000 => 4,
        < 1_500 => 5,
        < 3_000 => 6,
        _ => 7,
    };

    public static int ExperienceForLevel(int level) => level switch
    {
        <= 1 => 0,
        2 => 100,
        3 => 300,
        4 => 600,
        5 => 1_000,
        6 => 1_500,
        _ => 3_000,
    };
}
