namespace Automation.Domain;

public enum DishStationFixture
{
    Scrape,
    Rack,
    Washer,
    Unload,
    DryRestock,
    Service,
}

public readonly record struct FloorCell(int X, int Y)
{
    public const int MinimumX = 0;
    public const int MaximumX = 12;
    public const int MinimumY = 0;
    public const int MaximumY = 7;

    public bool IsInsideDishStation =>
        X is >= MinimumX and <= MaximumX && Y is >= MinimumY and <= MaximumY;

    public int DistanceTo(FloorCell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}

public readonly record struct DishStationPlacements(
    FloorCell Scrape,
    FloorCell Rack,
    FloorCell Washer,
    FloorCell Unload,
    FloorCell DryRestock,
    FloorCell Service)
{
    public static DishStationPlacements Linear => new(
        new(1, 1), new(4, 1), new(7, 1), new(10, 1), new(12, 3), new(11, 6));

    public static DishStationPlacements UShapedCell => new(
        new(1, 1), new(4, 1), new(7, 1), new(10, 1), new(10, 3), new(11, 6));

    public FloorCell At(DishStationFixture fixture) => fixture switch
    {
        DishStationFixture.Scrape => Scrape,
        DishStationFixture.Rack => Rack,
        DishStationFixture.Washer => Washer,
        DishStationFixture.Unload => Unload,
        DishStationFixture.DryRestock => DryRestock,
        DishStationFixture.Service => Service,
        _ => throw new ArgumentOutOfRangeException(nameof(fixture)),
    };

    public DishStationPlacements With(DishStationFixture fixture, FloorCell cell) => fixture switch
    {
        DishStationFixture.Scrape => this with { Scrape = cell },
        DishStationFixture.Rack => this with { Rack = cell },
        DishStationFixture.Washer => this with { Washer = cell },
        DishStationFixture.Unload => this with { Unload = cell },
        DishStationFixture.DryRestock => this with { DryRestock = cell },
        DishStationFixture.Service => this with { Service = cell },
        _ => throw new ArgumentOutOfRangeException(nameof(fixture)),
    };

    public bool IsOccupied(FloorCell cell, DishStationFixture except)
    {
        foreach (var fixture in Enum.GetValues<DishStationFixture>())
            if (fixture != except && At(fixture) == cell) return true;
        return false;
    }

    public bool IsValid()
    {
        foreach (var fixture in Enum.GetValues<DishStationFixture>())
        {
            var cell = At(fixture);
            if (!cell.IsInsideDishStation || IsOccupied(cell, fixture)) return false;
        }
        return true;
    }

    public int EstimatedRouteSteps =>
        Scrape.DistanceTo(Rack) +
        Rack.DistanceTo(Washer) +
        Washer.DistanceTo(Unload) +
        Unload.DistanceTo(DryRestock) +
        DryRestock.DistanceTo(Service);
}
