namespace Automation.Domain;

public enum DishStationNarrativeEventKind
{
    QueuePressure,
    AutomationIncident,
    ShiftSucceeded,
}

public readonly record struct DishStationNarrativeEvent(
    SimulationTick Tick,
    DishStationNarrativeEventKind Kind,
    DishStationQuestId Quest);
