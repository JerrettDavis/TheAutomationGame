namespace Automation.Domain;

public readonly record struct SimulationTick(long Value)
{
    public static SimulationTick operator +(SimulationTick tick, long amount) => new(tick.Value + amount);
}

public readonly record struct ActorId(int Value);
public readonly record struct WorkItemId(long Value);

public enum DishKind
{
    Plate,
    Glass,
    Tray,
}

public enum DishState
{
    Dirty,
    Scraped,
    Racked,
    Washing,
    WashedInMachine,
    CleanWet,
    Available,
}

public enum DishAction
{
    Scrape,
    Rack,
    StartWasher,
    Unload,
    DryAndRestock,
}

public readonly record struct DishCounts(int Plates, int Glasses, int Trays = 0)
{
    public int Total => Plates + Glasses + Trays;

    public DishCounts Add(DishKind kind, int amount = 1) => kind switch
    {
        DishKind.Plate => this with { Plates = Plates + amount },
        DishKind.Glass => this with { Glasses = Glasses + amount },
        DishKind.Tray => this with { Trays = Trays + amount },
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };

    public DishCounts Remove(DishKind kind, int amount = 1) => Add(kind, -amount);

    public int For(DishKind kind) => kind switch
    {
        DishKind.Plate => Plates,
        DishKind.Glass => Glasses,
        DishKind.Tray => Trays,
        _ => throw new ArgumentOutOfRangeException(nameof(kind)),
    };
}
