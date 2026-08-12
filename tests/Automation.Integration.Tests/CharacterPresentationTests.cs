using Automation.Client.Stride;
using Automation.Domain;
using Automation.Persistence;
using Automation.Simulation;

namespace Automation.Integration.Tests;

public sealed class CharacterPresentationTests
{
    [Theory]
    [InlineData(-1, -1, CharacterFacing.AwayFromCamera)]
    [InlineData(-1, 1, CharacterFacing.ScreenLeft)]
    [InlineData(1, 1, CharacterFacing.TowardCamera)]
    [InlineData(1, -1, CharacterFacing.ScreenRight)]
    public void IsometricWorldDirectionsResolveToStableScreenFacing(float x, float y, CharacterFacing expected)
    {
        Assert.Equal(expected, DishStationCharacterPresenter.ResolveFacing(
            new Stride.Core.Mathematics.Vector2(x, y), CharacterFacing.TowardCamera));
    }

    [Fact]
    public void PlayerWalkFacesAuthoritativeMovementAndReducedMotionSnapsToTruth()
    {
        var world = IntegrationTestScenario.World();
        var presenter = new DishStationCharacterPresenter();
        var initial = presenter.Update(world.Snapshot(), 0, reducedMotion: false);

        Assert.True(initial.Player.Selected);
        Assert.Equal(CharacterAnimationState.Idle, initial.Player.Animation);
        Assert.True(world.ExecuteNow(new MovePlayerCommand(world.Tick, new FloorCell(2, 3))).Success);

        var walking = presenter.Update(world.Snapshot(), 0.05f, reducedMotion: false);

        Assert.Equal(CharacterAnimationState.Walk, walking.Player.Animation);
        Assert.Equal(CharacterFacing.TowardCamera, walking.Player.Facing);
        Assert.NotEqual(new Stride.Core.Mathematics.Vector2(2, 3), walking.Player.Cell);

        var snapped = presenter.Update(world.Snapshot(), 0.05f, reducedMotion: true);

        Assert.Equal(new Stride.Core.Mathematics.Vector2(2, 3), snapped.Player.Cell);
        Assert.Equal(CharacterAnimationState.Idle, snapped.Player.Animation);
    }

    [Fact]
    public void SuccessfulWorkCueChangesPresentationWithoutChangingSimulationOrSaveIdentity()
    {
        var world = IntegrationTestScenario.World();
        var presenter = new DishStationCharacterPresenter();
        presenter.Update(world.Snapshot(), 0, reducedMotion: false);
        var save = DishStationSaveStore.Serialize(world);
        var snapshot = world.Snapshot();

        presenter.NotifyPlayerWork();
        var frame = presenter.Update(snapshot, 0.01f, reducedMotion: false);

        Assert.Equal(CharacterAnimationState.Work, frame.Player.Animation);
        Assert.True(SharedCharacterRig.Resolve(frame.Player, reducedMotion: false).WorkReach > 0);
        Assert.Equal(save, DishStationSaveStore.Serialize(world));

        var settled = presenter.Update(snapshot, 1f, reducedMotion: false);
        Assert.Equal(CharacterAnimationState.Idle, settled.Player.Animation);
    }

    [Fact]
    public void WorkerAuthoritativeActionChangeProducesWalkThenWorkPresentation()
    {
        var configuration = IntegrationTestScenario.Reference with
        {
            InitialNewHireEnabled = true,
            InitialNewHireSpecification = DishProcessSpecification.HappyPath,
            ArrivalIntervalTicks = 1000,
        };
        var world = new DishStationWorld(42, configuration);
        var presenter = new DishStationCharacterPresenter();
        presenter.Update(world.Snapshot(), 0, reducedMotion: false);

        AdvanceUntilWorkerActs(world, 1);
        var firstWork = presenter.Update(world.Snapshot(), 0.01f, reducedMotion: false);
        Assert.Equal(CharacterAnimationState.Work, firstWork.Worker.Animation);
        Assert.True(firstWork.Worker.Visible);
        Assert.False(firstWork.Worker.Selected);

        AdvanceUntilWorkerActs(world, 2);
        var walking = presenter.Update(world.Snapshot(), 0.05f, reducedMotion: false);
        Assert.Equal(CharacterAnimationState.Walk, walking.Worker.Animation);

        DishStationCharacterFrame arrived = walking;
        for (var index = 0; index < 20 && arrived.Worker.Animation == CharacterAnimationState.Walk; index++)
            arrived = presenter.Update(world.Snapshot(), 0.1f, reducedMotion: false);

        Assert.Equal(CharacterAnimationState.Work, arrived.Worker.Animation);
        Assert.Equal(new Stride.Core.Mathematics.Vector2(
            world.Topology.InteractionPort(DishStationFixture.Rack).X,
            world.Topology.InteractionPort(DishStationFixture.Rack).Y), arrived.Worker.Cell);
    }

    [Fact]
    public void SharedRigHasDistinctDeterministicIdleWalkAndWorkPoses()
    {
        var idle = new CharacterVisualPose(default, CharacterFacing.ScreenRight, CharacterAnimationState.Idle, 0.25f, true, true);
        var walk = idle with { Animation = CharacterAnimationState.Walk };
        var work = idle with { Animation = CharacterAnimationState.Work };

        var idleRig = SharedCharacterRig.Resolve(idle, reducedMotion: false);
        var walkRig = SharedCharacterRig.Resolve(walk, reducedMotion: false);
        var workRig = SharedCharacterRig.Resolve(work, reducedMotion: false);

        Assert.Equal(idleRig, SharedCharacterRig.Resolve(idle, reducedMotion: false));
        Assert.Equal(0, idleRig.LeftStride);
        Assert.NotEqual(0, walkRig.LeftStride);
        Assert.Equal(-walkRig.LeftStride, walkRig.RightStride);
        Assert.Equal(0, walkRig.WorkReach);
        Assert.True(workRig.WorkReach > 0);
        Assert.True(workRig.SelectionVisible);
        Assert.Equal(0, SharedCharacterRig.Resolve(walk, reducedMotion: true).LeftStride);
    }

    private static void AdvanceUntilWorkerActs(DishStationWorld world, int actions)
    {
        for (var tick = 0; tick < 100 && world.Snapshot().NewHire.ActionsCompleted < actions; tick++) world.Advance();
        Assert.Equal(actions, world.Snapshot().NewHire.ActionsCompleted);
    }
}
