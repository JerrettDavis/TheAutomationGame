using Automation.Domain;
using Automation.Simulation;

namespace Automation.Client.Stride;

[Flags]
public enum DirectMovementInput
{
    None = 0,
    Away = 1 << 0,
    Left = 1 << 1,
    Toward = 1 << 2,
    Right = 1 << 3,
}

public enum DirectPlayerMovementIntent
{
    None,
    AwayFromCamera,
    TowardCamera,
    ScreenLeft,
    ScreenRight,
    AwayLeft,
    AwayRight,
    TowardLeft,
    TowardRight,
}

public static class GameplayMovementInput
{
    public static DirectPlayerMovementIntent Resolve(DirectMovementInput input)
    {
        var horizontal = ((input & DirectMovementInput.Right) != 0 ? 1 : 0) -
            ((input & DirectMovementInput.Left) != 0 ? 1 : 0);
        var vertical = ((input & DirectMovementInput.Toward) != 0 ? 1 : 0) -
            ((input & DirectMovementInput.Away) != 0 ? 1 : 0);

        return (horizontal, vertical) switch
        {
            (0, -1) => DirectPlayerMovementIntent.AwayFromCamera,
            (0, 1) => DirectPlayerMovementIntent.TowardCamera,
            (-1, 0) => DirectPlayerMovementIntent.ScreenLeft,
            (1, 0) => DirectPlayerMovementIntent.ScreenRight,
            (-1, -1) => DirectPlayerMovementIntent.AwayLeft,
            (1, -1) => DirectPlayerMovementIntent.AwayRight,
            (-1, 1) => DirectPlayerMovementIntent.TowardLeft,
            (1, 1) => DirectPlayerMovementIntent.TowardRight,
            _ => DirectPlayerMovementIntent.None,
        };
    }

    public static MovePlayerCommand? CreateCommand(
        DirectMovementInput input,
        FloorCell current,
        SimulationTick tick)
    {
        var intent = Resolve(input);
        if (intent == DirectPlayerMovementIntent.None) return null;

        var (screenX, screenY) = intent switch
        {
            DirectPlayerMovementIntent.AwayFromCamera => (0, -1),
            DirectPlayerMovementIntent.TowardCamera => (0, 1),
            DirectPlayerMovementIntent.ScreenLeft => (-1, 0),
            DirectPlayerMovementIntent.ScreenRight => (1, 0),
            DirectPlayerMovementIntent.AwayLeft => (-1, -1),
            DirectPlayerMovementIntent.AwayRight => (1, -1),
            DirectPlayerMovementIntent.TowardLeft => (-1, 1),
            DirectPlayerMovementIntent.TowardRight => (1, 1),
            _ => (0, 0),
        };
        var worldX = Math.Sign(screenX + screenY);
        var worldY = Math.Sign(screenY - screenX);
        return new MovePlayerCommand(tick, new FloorCell(current.X + worldX, current.Y + worldY));
    }
}
