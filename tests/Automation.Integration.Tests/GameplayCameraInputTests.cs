using Automation.Client.Stride;

namespace Automation.Integration.Tests;

public sealed class GameplayCameraInputTests
{
    [Fact]
    public void MiddleDragMovesTheProjectedViewByCanvasRelativeDelta()
    {
        var camera = GameplayCameraInput.ApplyMiddleDrag(
            IsometricCamera.Default,
            physicalDeltaX: 30,
            physicalDeltaY: -18,
            canvasScale: 1.5f);

        Assert.Equal(20, camera.OffsetX);
        Assert.Equal(-12, camera.OffsetY);
        Assert.Equal(1, camera.Zoom);
    }

    [Fact]
    public void MiddleDragClampsBothPanAxes()
    {
        var camera = GameplayCameraInput.ApplyMiddleDrag(
            IsometricCamera.Default,
            physicalDeltaX: 10_000,
            physicalDeltaY: -10_000,
            canvasScale: 1);

        Assert.Equal(IsometricCamera.MaximumOffsetX, camera.OffsetX);
        Assert.Equal(IsometricCamera.MinimumOffsetY, camera.OffsetY);
    }

    [Fact]
    public void WheelDirectionZoomsAndClampsAtBothBounds()
    {
        var zoomedIn = IsometricCamera.Default;
        var zoomedOut = IsometricCamera.Default;
        for (var index = 0; index < 20; index++)
        {
            zoomedIn = GameplayCameraInput.ApplyWheel(zoomedIn, 1);
            zoomedOut = GameplayCameraInput.ApplyWheel(zoomedOut, -1);
        }

        Assert.Equal(IsometricCamera.MaximumZoom, zoomedIn.Zoom);
        Assert.Equal(IsometricCamera.MinimumZoom, zoomedOut.Zoom);
        Assert.Equal(IsometricCamera.Default, GameplayCameraInput.ApplyWheel(IsometricCamera.Default, 0));
    }

    [Fact]
    public void RecenterRestoresCanonicalCameraWithoutSimulationOutput()
    {
        var moved = new IsometricCamera(100, -50, 1.3f);

        var reset = GameplayCameraInput.Recenter();

        Assert.Equal(IsometricCamera.Default, reset);
        Assert.NotEqual(moved, reset);
        Assert.DoesNotContain(typeof(GameplayCameraInput).GetMethods(), method =>
            typeof(Automation.Simulation.ISimulationCommand).IsAssignableFrom(method.ReturnType));
    }

    [Fact]
    public void InvalidCanvasScaleIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            GameplayCameraInput.ApplyMiddleDrag(IsometricCamera.Default, 1, 1, 0));
    }

    [Fact]
    public void CameraSensitivityScalesDragAndWheelResponse()
    {
        var drag = GameplayCameraInput.ApplyMiddleDrag(IsometricCamera.Default, 20, -10, 1, sensitivity: 1.5f);
        var wheel = GameplayCameraInput.ApplyWheel(IsometricCamera.Default, 1, sensitivity: 1.5f);

        Assert.Equal(30, drag.OffsetX);
        Assert.Equal(-15, drag.OffsetY);
        Assert.Equal(1.15f, wheel.Zoom, 3);
    }
}
