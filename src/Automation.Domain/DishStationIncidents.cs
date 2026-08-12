namespace Automation.Domain;

public readonly record struct DishStationIncidentId
{
    public DishStationIncidentId(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Value = value;
    }

    public string Value { get; }
    public override string ToString() => Value;
}

public enum DishStationIncidentKind
{
    ProcessDelay,
    CapacityLoss,
    BadSensor,
    BlockedResource,
    WorkerAbsence,
    DemandSpike,
}

public abstract record DishStationIncidentEffect(DishStationIncidentKind Kind, int DurationTicks)
{
    public abstract DishStationIncidentEffect Validate();

    protected void ValidateDuration()
    {
        if (DurationTicks <= 0) throw new ArgumentOutOfRangeException(nameof(DurationTicks));
    }
}

public sealed record ProcessDelayIncidentEffect(int DurationTicks, int AddedCycleTicks)
    : DishStationIncidentEffect(DishStationIncidentKind.ProcessDelay, DurationTicks)
{
    public override DishStationIncidentEffect Validate()
    {
        ValidateDuration();
        if (AddedCycleTicks <= 0) throw new ArgumentOutOfRangeException(nameof(AddedCycleTicks));
        return this;
    }
}

public sealed record CapacityLossIncidentEffect(int DurationTicks, int LostSlots)
    : DishStationIncidentEffect(DishStationIncidentKind.CapacityLoss, DurationTicks)
{
    public override DishStationIncidentEffect Validate()
    {
        ValidateDuration();
        if (LostSlots <= 0) throw new ArgumentOutOfRangeException(nameof(LostSlots));
        return this;
    }
}

public sealed record BadSensorIncidentEffect(int DurationTicks)
    : DishStationIncidentEffect(DishStationIncidentKind.BadSensor, DurationTicks)
{
    public override DishStationIncidentEffect Validate() { ValidateDuration(); return this; }
}

public sealed record BlockedResourceIncidentEffect(int DurationTicks)
    : DishStationIncidentEffect(DishStationIncidentKind.BlockedResource, DurationTicks)
{
    public override DishStationIncidentEffect Validate() { ValidateDuration(); return this; }
}

public sealed record WorkerAbsenceIncidentEffect(int DurationTicks)
    : DishStationIncidentEffect(DishStationIncidentKind.WorkerAbsence, DurationTicks)
{
    public override DishStationIncidentEffect Validate() { ValidateDuration(); return this; }
}

public sealed record DemandSpikeIncidentEffect(int DurationTicks, DishKind DemandKind, int IntervalTicks)
    : DishStationIncidentEffect(DishStationIncidentKind.DemandSpike, DurationTicks)
{
    public override DishStationIncidentEffect Validate()
    {
        ValidateDuration();
        if (!Enum.IsDefined(DemandKind)) throw new ArgumentOutOfRangeException(nameof(DemandKind));
        if (IntervalTicks <= 0) throw new ArgumentOutOfRangeException(nameof(IntervalTicks));
        return this;
    }
}

public sealed record DishStationIncident(
    DishStationIncidentId Id,
    string Scope,
    string Observable,
    string Evidence,
    string Recovery,
    DishStationIncidentEffect Effect)
{
    public DishStationIncident Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Id.Value);
        ArgumentException.ThrowIfNullOrWhiteSpace(Scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(Observable);
        ArgumentException.ThrowIfNullOrWhiteSpace(Evidence);
        ArgumentException.ThrowIfNullOrWhiteSpace(Recovery);
        ArgumentNullException.ThrowIfNull(Effect);
        Effect.Validate();
        return this;
    }
}

public readonly record struct ScheduledDishStationIncident(SimulationTick TriggerAt, DishStationIncident Incident)
{
    public ScheduledDishStationIncident Validate()
    {
        if (TriggerAt.Value < 0) throw new ArgumentOutOfRangeException(nameof(TriggerAt));
        ArgumentNullException.ThrowIfNull(Incident);
        Incident.Validate();
        return this;
    }
}
