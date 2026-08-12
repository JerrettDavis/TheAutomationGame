namespace Automation.Domain;

public readonly record struct DishStationTopology(DishStationPlacements Placements)
{
    private const int Width = FloorCell.MaximumX - FloorCell.MinimumX + 1;
    private const int Height = FloorCell.MaximumY - FloorCell.MinimumY + 1;
    private const int CellCount = Width * Height;

    public bool IsWalkable(FloorCell cell) => cell.IsInsideDishStation && !IsFixtureFootprint(cell);

    public bool IsFixtureFootprint(FloorCell cell) =>
        cell == Placements.Scrape || cell == Placements.Rack || cell == Placements.Washer ||
        cell == Placements.Unload || cell == Placements.DryRestock || cell == Placements.Service;

    public bool TryFixtureAt(FloorCell cell, out DishStationFixture fixture)
    {
        for (var value = (int)DishStationFixture.Scrape; value <= (int)DishStationFixture.Service; value++)
        {
            fixture = (DishStationFixture)value;
            if (Placements.At(fixture) == cell) return true;
        }
        fixture = default;
        return false;
    }

    public FloorCell InteractionPort(DishStationFixture fixture)
    {
        if (TryInteractionPort(fixture, out var port)) return port;
        throw new InvalidOperationException($"{fixture} has no walkable interaction port.");
    }

    public bool TryInteractionPort(DishStationFixture fixture, out FloorCell port)
    {
        var footprint = Placements.At(fixture);
        Span<FloorCell> candidates =
        [
            new(footprint.X, footprint.Y + 1),
            new(footprint.X - 1, footprint.Y),
            new(footprint.X + 1, footprint.Y),
            new(footprint.X, footprint.Y - 1),
        ];
        foreach (var candidate in candidates)
            if (IsWalkable(candidate))
            {
                port = candidate;
                return true;
            }
        port = default;
        return false;
    }

    public bool CanStep(FloorCell from, FloorCell to)
    {
        if (!IsWalkable(from) || !IsWalkable(to)) return false;
        var deltaX = to.X - from.X;
        var deltaY = to.Y - from.Y;
        if (deltaX == 0 && deltaY == 0 || Math.Abs(deltaX) > 1 || Math.Abs(deltaY) > 1) return false;
        if (deltaX == 0 || deltaY == 0) return true;
        return IsWalkable(new FloorCell(from.X + deltaX, from.Y)) &&
               IsWalkable(new FloorCell(from.X, from.Y + deltaY));
    }

    public FloorCell[] FindPath(FloorCell start, FloorCell destination)
    {
        if (!IsWalkable(start) || !IsWalkable(destination)) return [];
        if (start == destination) return [start];

        var visited = new bool[CellCount];
        var previous = new int[CellCount];
        Array.Fill(previous, -1);
        var queue = new int[CellCount];
        var head = 0;
        var tail = 0;
        var startIndex = Index(start);
        var destinationIndex = Index(destination);
        visited[startIndex] = true;
        queue[tail++] = startIndex;

        while (head < tail && !visited[destinationIndex])
        {
            var current = Cell(queue[head++]);
            for (var direction = 0; direction < 8; direction++)
            {
                var next = Neighbor(current, direction);
                if (!next.IsInsideDishStation || !CanStep(current, next)) continue;
                var nextIndex = Index(next);
                if (visited[nextIndex]) continue;
                visited[nextIndex] = true;
                previous[nextIndex] = Index(current);
                queue[tail++] = nextIndex;
            }
        }

        if (!visited[destinationIndex]) return [];
        var length = 1;
        for (var index = destinationIndex; index != startIndex; index = previous[index]) length++;
        var path = new FloorCell[length];
        var cursor = destinationIndex;
        for (var index = length - 1; index >= 0; index--)
        {
            path[index] = Cell(cursor);
            if (cursor != startIndex) cursor = previous[cursor];
        }
        return path;
    }

    public FloorCell ResolveWalkable(FloorCell preferred)
    {
        if (IsWalkable(preferred)) return preferred;
        FloorCell? best = null;
        var bestDistance = int.MaxValue;
        for (var y = FloorCell.MinimumY; y <= FloorCell.MaximumY; y++)
            for (var x = FloorCell.MinimumX; x <= FloorCell.MaximumX; x++)
            {
                var candidate = new FloorCell(x, y);
                if (!IsWalkable(candidate)) continue;
                var distance = preferred.DistanceTo(candidate);
                if (distance >= bestDistance) continue;
                best = candidate;
                bestDistance = distance;
            }
        return best ?? throw new InvalidOperationException("Dish-station topology has no walkable floor cell.");
    }

    public bool AllInteractionPortsConnected()
    {
        if (!TryInteractionPort(DishStationFixture.Scrape, out var origin)) return false;
        for (var value = (int)DishStationFixture.Rack; value <= (int)DishStationFixture.Service; value++)
            if (!TryInteractionPort((DishStationFixture)value, out var port) || FindPath(origin, port).Length == 0) return false;
        return true;
    }

    private static int Index(FloorCell cell) => (cell.Y - FloorCell.MinimumY) * Width + cell.X - FloorCell.MinimumX;
    private static FloorCell Cell(int index) => new(index % Width + FloorCell.MinimumX, index / Width + FloorCell.MinimumY);

    private static FloorCell Neighbor(FloorCell cell, int direction) => direction switch
    {
        0 => new(cell.X, cell.Y - 1),
        1 => new(cell.X - 1, cell.Y),
        2 => new(cell.X + 1, cell.Y),
        3 => new(cell.X, cell.Y + 1),
        4 => new(cell.X - 1, cell.Y - 1),
        5 => new(cell.X + 1, cell.Y - 1),
        6 => new(cell.X - 1, cell.Y + 1),
        7 => new(cell.X + 1, cell.Y + 1),
        _ => throw new ArgumentOutOfRangeException(nameof(direction)),
    };
}
