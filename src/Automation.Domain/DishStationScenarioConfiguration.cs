namespace Automation.Domain;

public sealed record DishStationScenarioConfiguration
{
    public required DishCounts InitialDirty { get; init; }
    public required DishCounts InitialAvailable { get; init; }
    public required int ArrivalIntervalTicks { get; init; }
    public required int GlassEveryArrivals { get; init; }
    public required int RackCapacity { get; init; }
    public required int WasherCycleTicks { get; init; }
    public required int WorkerActionIntervalTicks { get; init; }
    public required int FlowCellWorkerActionIntervalTicks { get; init; }
    public required int StickyReadyFaultAfterAutomatedStarts { get; init; }
    public required int StickyReadyFaultPermillePerStart { get; init; }
    public required DishKind DemandKind { get; init; }
    public required int DemandIntervalTicks { get; init; }
    public required bool InitialRushEnabled { get; init; }
    public required bool InitialNewHireEnabled { get; init; }
    public required DishProcessSpecification InitialNewHireSpecification { get; init; }
    public required WasherAutomationPolicy InitialAutomationPolicy { get; init; }
    public required DishStationLayout InitialLayout { get; init; }
    public DishStationEconomyConfiguration Economy { get; init; } = DishStationEconomyConfiguration.Default;

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
        Economy.Validate();
        return this;
    }
}

public sealed record DishStationEconomyConfiguration(
    int CompletedDishValue,
    int LaborTicksPerWorkAction,
    int LaborCostPerTick,
    int StaffingCostPerEnabledTick,
    int TrayReworkCost,
    int ServiceShortageDowntimeCost,
    int AutomationIncidentDowntimeCost,
    int FlowCellInvestmentCost)
{
    public static DishStationEconomyConfiguration Default { get; } = new(
        CompletedDishValue: 120,
        LaborTicksPerWorkAction: 1,
        LaborCostPerTick: 3,
        StaffingCostPerEnabledTick: 1,
        TrayReworkCost: 35,
        ServiceShortageDowntimeCost: 80,
        AutomationIncidentDowntimeCost: 120,
        FlowCellInvestmentCost: 180);

    public DishStationEconomyConfiguration Validate()
    {
        if (CompletedDishValue <= 0) throw new ArgumentOutOfRangeException(nameof(CompletedDishValue));
        if (LaborTicksPerWorkAction <= 0) throw new ArgumentOutOfRangeException(nameof(LaborTicksPerWorkAction));
        if (LaborCostPerTick < 0) throw new ArgumentOutOfRangeException(nameof(LaborCostPerTick));
        if (StaffingCostPerEnabledTick < 0) throw new ArgumentOutOfRangeException(nameof(StaffingCostPerEnabledTick));
        if (TrayReworkCost < 0) throw new ArgumentOutOfRangeException(nameof(TrayReworkCost));
        if (ServiceShortageDowntimeCost < 0) throw new ArgumentOutOfRangeException(nameof(ServiceShortageDowntimeCost));
        if (AutomationIncidentDowntimeCost < 0) throw new ArgumentOutOfRangeException(nameof(AutomationIncidentDowntimeCost));
        if (FlowCellInvestmentCost < 0) throw new ArgumentOutOfRangeException(nameof(FlowCellInvestmentCost));
        return this;
    }
}
