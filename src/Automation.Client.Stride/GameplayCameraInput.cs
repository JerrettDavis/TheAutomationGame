namespace Automation.Client.Stride;

public static class GameplayCameraInput
{
    public const float ZoomPerWheelStep = 0.1f;

    public static IsometricCamera ApplyMiddleDrag(
        IsometricCamera camera,
        float physicalDeltaX,
        float physicalDeltaY,
        float canvasScale,
        float sensitivity = 1)
    {
        if (canvasScale <= 0) throw new ArgumentOutOfRangeException(nameof(canvasScale));
        if (sensitivity <= 0) throw new ArgumentOutOfRangeException(nameof(sensitivity));
        return camera.Pan(physicalDeltaX / canvasScale * sensitivity, physicalDeltaY / canvasScale * sensitivity);
    }

    public static IsometricCamera ApplyWheel(IsometricCamera camera, float wheelDelta, float sensitivity = 1)
    {
        if (sensitivity <= 0) throw new ArgumentOutOfRangeException(nameof(sensitivity));
        return camera.ZoomBy(Math.Sign(wheelDelta) * ZoomPerWheelStep * sensitivity);
    }

    public static IsometricCamera Recenter() => IsometricCamera.Default;
}
