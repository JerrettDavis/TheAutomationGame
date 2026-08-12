using Automation.Domain;

namespace Automation.Client.Stride;

[Flags]
public enum GameplayInteractionActions
{
    None = 0,
    Interact = 1 << 0,
    Inspect = 1 << 1,
}

[Flags]
public enum GameplayInteractionIntent
{
    None = 0,
    Interact = 1 << 0,
    Inspect = 1 << 1,
}

public static class GameplayInteractionInput
{
    public static GameplayInteractionIntent Resolve(GameplayInteractionActions actions)
    {
        var intent = GameplayInteractionIntent.None;
        if ((actions & GameplayInteractionActions.Interact) != 0) intent |= GameplayInteractionIntent.Interact;
        if ((actions & GameplayInteractionActions.Inspect) != 0) intent |= GameplayInteractionIntent.Inspect;
        return intent;
    }
}

public static class GameplayInteractionResolver
{
    public static DishStationFixture Resolve(
        FloorCell playerCell,
        DishStationPlacements placements,
        DishStationFixture? selectedFixture)
    {
        var topology = new DishStationTopology(placements);
        if (selectedFixture is { } selected && playerCell == topology.InteractionPort(selected)) return selected;

        var nearest = DishStationFixture.Scrape;
        var nearestDistance = int.MaxValue;
        for (var value = (int)DishStationFixture.Scrape; value <= (int)DishStationFixture.Service; value++)
        {
            var fixture = (DishStationFixture)value;
            var distance = playerCell.DistanceTo(topology.InteractionPort(fixture));
            if (distance >= nearestDistance) continue;
            nearest = fixture;
            nearestDistance = distance;
        }
        return nearest;
    }
}
