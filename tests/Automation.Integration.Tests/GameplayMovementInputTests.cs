using Automation.Client.Stride;
using Automation.Domain;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class GameplayMovementInputTests
{
    [Theory]
    [InlineData(DirectMovementInput.Away, DirectPlayerMovementIntent.AwayFromCamera, 5, 3)]
    [InlineData(DirectMovementInput.Left, DirectPlayerMovementIntent.ScreenLeft, 5, 5)]
    [InlineData(DirectMovementInput.Toward, DirectPlayerMovementIntent.TowardCamera, 7, 5)]
    [InlineData(DirectMovementInput.Right, DirectPlayerMovementIntent.ScreenRight, 7, 3)]
    public void WasdMapsToNaturalIsometricMovement(
        DirectMovementInput input,
        DirectPlayerMovementIntent expectedIntent,
        int expectedX,
        int expectedY)
    {
        var command = GameplayMovementInput.CreateCommand(input, new FloorCell(6, 4), new SimulationTick(12));

        Assert.Equal(expectedIntent, GameplayMovementInput.Resolve(input));
        Assert.NotNull(command);
        Assert.Equal(new FloorCell(expectedX, expectedY), command.Destination);
        Assert.Equal(new SimulationTick(12), command.ExecuteAtTick);
    }

    [Fact]
    public void WInGameplayEmitsOnlyAnAuthoritativeMovementCommand()
    {
        var command = GameplayMovementInput.CreateCommand(
            DirectMovementInput.Away,
            new FloorCell(6, 4),
            new SimulationTick(20));

        Assert.IsType<MovePlayerCommand>(command);
        Assert.IsNotType<StartShiftTrialCommand>(command);
    }

    [Theory]
    [InlineData(DirectMovementInput.Away | DirectMovementInput.Toward)]
    [InlineData(DirectMovementInput.Left | DirectMovementInput.Right)]
    [InlineData(DirectMovementInput.Away | DirectMovementInput.Left | DirectMovementInput.Toward | DirectMovementInput.Right)]
    public void OpposingKeysCancelDeterministically(DirectMovementInput input)
    {
        Assert.Equal(DirectPlayerMovementIntent.None, GameplayMovementInput.Resolve(input));
        Assert.Null(GameplayMovementInput.CreateCommand(input, new FloorCell(6, 4), new SimulationTick(1)));
    }

    [Fact]
    public void SimultaneousNonOpposingKeysResolveToOneDeterministicNeighbor()
    {
        var command = GameplayMovementInput.CreateCommand(
            DirectMovementInput.Away | DirectMovementInput.Right,
            new FloorCell(6, 4),
            new SimulationTick(1));

        Assert.Equal(DirectPlayerMovementIntent.AwayRight, GameplayMovementInput.Resolve(DirectMovementInput.Away | DirectMovementInput.Right));
        Assert.Equal(new FloorCell(6, 3), command?.Destination);
    }

    [Fact]
    public void DirectMovementUsesWorldAuthorityAndRejectsAnOutOfBoundsDestination()
    {
        var world = IntegrationTestScenario.World();
        foreach (var cell in world.Topology.FindPath(world.PlayerCell, new FloorCell(0, 0)).Skip(1))
            Assert.True(world.ExecuteNow(new MovePlayerCommand(world.Tick, cell)).Success);
        var command = GameplayMovementInput.CreateCommand(DirectMovementInput.Away, world.PlayerCell, world.Tick);

        var result = world.ExecuteNow(command!);

        Assert.False(result.Success);
        Assert.Equal(new FloorCell(0, 0), world.PlayerCell);
    }
}
