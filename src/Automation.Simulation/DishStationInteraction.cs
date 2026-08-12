using Automation.Domain;

namespace Automation.Simulation;

public enum DishStationInteractionBlockReason
{
    None,
    MoveCloser,
    InspectionOnly,
    NoDishReady,
    RackFull,
    WasherRunning,
    WasherNeedsUnload,
}

public readonly record struct DishStationInteractionState(
    DishStationFixture Fixture,
    FloorCell Cell,
    int Distance,
    DishAction? WorkAction,
    DishState? RequiredState,
    int SelectedDishCount,
    DishStationInteractionBlockReason WorkBlockReason)
{
    public bool IsInRange => Distance == 0;
    public bool CanWork => WorkAction is not null && WorkBlockReason == DishStationInteractionBlockReason.None;
    public bool CanInspect => IsInRange;
}
