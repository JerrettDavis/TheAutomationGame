using Automation.Domain;
using Automation.Simulation;

namespace Automation.Client.Stride;

public sealed class GameplayClickRoute
{
    private readonly Queue<FloorCell> remaining = new();

    public int PendingSteps => remaining.Count;

    public bool Begin(FloorCell current, FloorCell requestedDestination, DishStationPlacements placements)
    {
        remaining.Clear();
        var topology = new DishStationTopology(placements);
        var destination = topology.TryFixtureAt(requestedDestination, out var fixture)
            ? topology.InteractionPort(fixture)
            : requestedDestination;
        var path = topology.FindPath(current, destination);
        if (path.Length == 0) return false;
        for (var index = 1; index < path.Length; index++) remaining.Enqueue(path[index]);
        return true;
    }

    public MovePlayerCommand? TakeNext(FloorCell current, DishStationPlacements placements, SimulationTick tick)
    {
        if (remaining.Count == 0) return null;
        var destination = remaining.Peek();
        if (!new DishStationTopology(placements).CanStep(current, destination))
        {
            remaining.Clear();
            return null;
        }
        remaining.Dequeue();
        return new MovePlayerCommand(tick, destination);
    }

    public void Cancel() => remaining.Clear();
}
