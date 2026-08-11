using Automation.Domain;

namespace Automation.Simulation;

public enum DishTransitionCause
{
    PlayerWork,
    NewHireWork,
    Automation,
    WasherCycle,
    ServiceDemand,
}

public readonly record struct DishTransitionEntry(
    SimulationTick Tick,
    DishKind Kind,
    DishState From,
    DishState To,
    DishTransitionCause Cause);
