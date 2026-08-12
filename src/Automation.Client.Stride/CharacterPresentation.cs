using Automation.Domain;
using Automation.Simulation;
using Stride.Core.Mathematics;

namespace Automation.Client.Stride;

public enum CharacterAnimationState
{
    Idle,
    Walk,
    Work,
}

public enum CharacterFacing
{
    AwayFromCamera,
    ScreenLeft,
    TowardCamera,
    ScreenRight,
}

public readonly record struct CharacterVisualPose(
    Vector2 Cell,
    CharacterFacing Facing,
    CharacterAnimationState Animation,
    float Phase,
    bool Selected,
    bool Visible);

public readonly record struct DishStationCharacterFrame(CharacterVisualPose Player, CharacterVisualPose Worker);

public readonly record struct CharacterRigPose(
    float BodyBob,
    float LeftStride,
    float RightStride,
    float WorkReach,
    float FacingX,
    float FacingY,
    bool SelectionVisible);

public static class SharedCharacterRig
{
    public static CharacterRigPose Resolve(CharacterVisualPose pose, bool reducedMotion)
    {
        var phase = reducedMotion ? 0 : pose.Phase;
        var wave = MathF.Sin(phase * MathF.PI * 2);
        var facing = pose.Facing switch
        {
            CharacterFacing.AwayFromCamera => (X: 0f, Y: -1f),
            CharacterFacing.ScreenLeft => (X: -1f, Y: 0f),
            CharacterFacing.TowardCamera => (X: 0f, Y: 1f),
            CharacterFacing.ScreenRight => (X: 1f, Y: 0f),
            _ => (X: 0f, Y: 1f),
        };
        return pose.Animation switch
        {
            CharacterAnimationState.Walk => new(MathF.Abs(wave) * 1.5f, wave * 3f, -wave * 3f, 0,
                facing.X, facing.Y, pose.Selected),
            CharacterAnimationState.Work => new(reducedMotion ? 0 : MathF.Abs(wave) * 0.75f, 0, 0,
                reducedMotion ? 0.8f : 0.65f + MathF.Abs(wave) * 0.35f, facing.X, facing.Y, pose.Selected),
            _ => new(reducedMotion ? 0 : wave * 0.45f, 0, 0, 0,
                facing.X, facing.Y, pose.Selected),
        };
    }
}

public sealed class DishStationCharacterPresenter
{
    private const float MovementSpeed = 6f;
    private const float WorkDuration = 0.65f;
    private Vector2 playerVisual;
    private Vector2 playerTarget;
    private Vector2 workerVisual;
    private Vector2 workerTarget;
    private CharacterFacing playerFacing = CharacterFacing.TowardCamera;
    private CharacterFacing workerFacing = CharacterFacing.TowardCamera;
    private int observedWorkerActions;
    private float playerWorkRemaining;
    private float workerWorkRemaining;
    private bool playerWorkPending;
    private bool workerWorkPending;
    private float animationClock;
    private bool initialized;

    public void NotifyPlayerWork() => playerWorkPending = true;

    public void Reset()
    {
        initialized = false;
        playerWorkPending = false;
        workerWorkPending = false;
        playerWorkRemaining = 0;
        workerWorkRemaining = 0;
    }

    public DishStationCharacterFrame Update(DishStationSnapshot snapshot, float elapsedSeconds, bool reducedMotion)
    {
        var topology = new DishStationTopology(snapshot.Layout.Placements);
        var nextPlayerTarget = CellVector(snapshot.Layout.PlayerCell);
        var nextWorkerTarget = CellVector(WorkerCell(snapshot.NewHire, topology));
        if (!initialized)
        {
            playerVisual = playerTarget = nextPlayerTarget;
            workerVisual = workerTarget = nextWorkerTarget;
            observedWorkerActions = snapshot.NewHire.ActionsCompleted;
            initialized = true;
        }

        if (nextPlayerTarget != playerTarget)
        {
            playerFacing = ResolveFacing(nextPlayerTarget - playerTarget, playerFacing);
            playerTarget = nextPlayerTarget;
            playerWorkRemaining = 0;
        }
        if (snapshot.NewHire.ActionsCompleted != observedWorkerActions)
        {
            workerWorkPending = true;
            observedWorkerActions = snapshot.NewHire.ActionsCompleted;
        }
        if (nextWorkerTarget != workerTarget)
        {
            workerFacing = ResolveFacing(nextWorkerTarget - workerTarget, workerFacing);
            workerTarget = nextWorkerTarget;
            workerWorkRemaining = 0;
        }

        animationClock += Math.Max(0, elapsedSeconds);
        playerVisual = Move(playerVisual, playerTarget, elapsedSeconds, reducedMotion);
        workerVisual = Move(workerVisual, workerTarget, elapsedSeconds, reducedMotion);
        var playerMoving = !Near(playerVisual, playerTarget);
        var workerMoving = !Near(workerVisual, workerTarget);

        if (!playerMoving) playerWorkRemaining = Math.Max(0, playerWorkRemaining - Math.Max(0, elapsedSeconds));
        if (!workerMoving) workerWorkRemaining = Math.Max(0, workerWorkRemaining - Math.Max(0, elapsedSeconds));
        if (!playerMoving && playerWorkPending)
        {
            playerWorkPending = false;
            playerWorkRemaining = WorkDuration;
        }
        if (!workerMoving && workerWorkPending)
        {
            workerWorkPending = false;
            workerWorkRemaining = WorkDuration;
        }

        var playerAnimation = playerMoving ? CharacterAnimationState.Walk : playerWorkRemaining > 0 ? CharacterAnimationState.Work : CharacterAnimationState.Idle;
        var workerAnimation = workerMoving ? CharacterAnimationState.Walk : workerWorkRemaining > 0 ? CharacterAnimationState.Work : CharacterAnimationState.Idle;
        var phase = animationClock % 1f;
        return new(
            new(playerVisual, playerFacing, playerAnimation, phase, Selected: true, Visible: true),
            new(workerVisual, workerFacing, workerAnimation, phase, Selected: false, Visible: snapshot.NewHire.Enabled));
    }

    public static CharacterFacing ResolveFacing(Vector2 delta, CharacterFacing fallback)
    {
        var screenX = delta.X - delta.Y;
        var screenY = delta.X + delta.Y;
        if (MathF.Abs(screenX) < 0.001f && MathF.Abs(screenY) < 0.001f) return fallback;
        if (MathF.Abs(screenX) > MathF.Abs(screenY))
            return screenX < 0 ? CharacterFacing.ScreenLeft : CharacterFacing.ScreenRight;
        return screenY < 0 ? CharacterFacing.AwayFromCamera : CharacterFacing.TowardCamera;
    }

    private static FloorCell WorkerCell(NewHireSnapshot worker, DishStationTopology topology)
    {
        var fixture = worker.LastAction switch
        {
            DishAction.Rack => DishStationFixture.Rack,
            DishAction.StartWasher => DishStationFixture.Washer,
            DishAction.Unload => DishStationFixture.Unload,
            DishAction.DryAndRestock => DishStationFixture.DryRestock,
            _ => DishStationFixture.Scrape,
        };
        return topology.InteractionPort(fixture);
    }

    private static Vector2 Move(Vector2 current, Vector2 target, float elapsedSeconds, bool reducedMotion)
    {
        if (reducedMotion) return target;
        var delta = target - current;
        var distance = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);
        var maximum = Math.Max(0, elapsedSeconds) * MovementSpeed;
        if (distance <= maximum || distance < 0.001f) return target;
        return current + delta * (maximum / distance);
    }

    private static bool Near(Vector2 left, Vector2 right) =>
        MathF.Abs(left.X - right.X) < 0.001f && MathF.Abs(left.Y - right.Y) < 0.001f;

    private static Vector2 CellVector(FloorCell cell) => new(cell.X, cell.Y);
}
