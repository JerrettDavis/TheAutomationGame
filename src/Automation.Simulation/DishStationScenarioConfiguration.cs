using Automation.Domain;

namespace Automation.Simulation;

public sealed record DishStationScenarioConfiguration
{
    public DishCounts InitialDirty { get; init; } = new(6, 2);
    public DishCounts InitialAvailable { get; init; }
    public int ArrivalIntervalTicks { get; init; } = 30;
    public int GlassEveryArrivals { get; init; } = 3;
    public int RackCapacity { get; init; } = 12;
    public int WasherCycleTicks { get; init; } = 20;
    public int WorkerActionIntervalTicks { get; init; } = 5;
    public int FlowCellWorkerActionIntervalTicks { get; init; } = 4;
    public int StickyReadyFaultAfterAutomatedStarts { get; init; } = 2;
    public int StickyReadyFaultPermillePerStart { get; init; }
    public DishKind DemandKind { get; init; } = DishKind.Glass;
    public int DemandIntervalTicks { get; init; } = 15;
    public bool InitialRushEnabled { get; init; }
    public bool InitialNewHireEnabled { get; init; }
    public DishProcessSpecification InitialNewHireSpecification { get; init; }
    public WasherAutomationPolicy InitialAutomationPolicy { get; init; } = WasherAutomationPolicy.Off;
    public DishStationLayout InitialLayout { get; init; } = DishStationLayout.Linear;

    public DishStationScenarioConfiguration Validate()
    {
        if (InitialDirty.Plates < 0 || InitialDirty.Glasses < 0 || InitialDirty.Trays < 0 ||
            InitialAvailable.Plates < 0 || InitialAvailable.Glasses < 0 || InitialAvailable.Trays < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(InitialDirty), "Initial dish counts cannot be negative.");
        }
        if (ArrivalIntervalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(ArrivalIntervalTicks));
        if (GlassEveryArrivals <= 0) throw new ArgumentOutOfRangeException(nameof(GlassEveryArrivals));
        if (RackCapacity <= 0) throw new ArgumentOutOfRangeException(nameof(RackCapacity));
        if (WasherCycleTicks <= 0) throw new ArgumentOutOfRangeException(nameof(WasherCycleTicks));
        if (WorkerActionIntervalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(WorkerActionIntervalTicks));
        if (FlowCellWorkerActionIntervalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(FlowCellWorkerActionIntervalTicks));
        if (StickyReadyFaultAfterAutomatedStarts < 0) throw new ArgumentOutOfRangeException(nameof(StickyReadyFaultAfterAutomatedStarts));
        if (StickyReadyFaultPermillePerStart is < 0 or > 1000) throw new ArgumentOutOfRangeException(nameof(StickyReadyFaultPermillePerStart));
        if (DemandIntervalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(DemandIntervalTicks));
        return this;
    }
}
