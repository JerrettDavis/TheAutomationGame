using Automation.Domain;
using Stride.Core.Mathematics;

namespace Automation.Client.Stride;

public readonly record struct DishRoomCameraFrame(Vector3 Position, Vector3 Target, float OrthographicSize)
{
    public Matrix ViewMatrix => Matrix.LookAtLH(Position, Target, Vector3.UnitY);
}

public static class DishRoomCameraProjection
{
    public const float VirtualWidth = 1024;
    public const float VirtualHeight = 600;
    public const float BaseOrthographicSize = 11.785f;
    private static readonly Vector3 CameraOffset = new(6.52f, 3.89f, 6.52f);

    public static DishRoomCameraFrame Resolve(IsometricCamera state)
    {
        var difference = (72 - state.OffsetX) / (36 * state.Zoom);
        var sum = (154 - state.OffsetY) / (14 * state.Zoom);
        var target = ToNative(new Vector3((sum + difference) * 0.5f, 0, (sum - difference) * 0.5f));
        return new(target + CameraOffset, target, BaseOrthographicSize / state.Zoom);
    }

    public static Vector2 ProjectFloor(FloorCell cell, IsometricCamera state)
    {
        var frame = Resolve(state);
        var projection = Projection(state, VirtualWidth, VirtualHeight);
        var projected = Vector3.Project(ToNative(new Vector3(cell.X, 0, cell.Y)), 0, 0, VirtualWidth, VirtualHeight, 0, 1,
            frame.ViewMatrix * projection);
        return new Vector2(projected.X, projected.Y);
    }

    public static Matrix Projection(IsometricCamera state, float viewportWidth, float viewportHeight)
    {
        var canvasScale = MathF.Min(viewportWidth / VirtualWidth, viewportHeight / VirtualHeight);
        var pixelsPerWorldUnit = canvasScale * 50.91f * state.Zoom;
        return Matrix.OrthoLH(viewportWidth / pixelsPerWorldUnit, viewportHeight / pixelsPerWorldUnit, 0.1f, 100);
    }

    public static Vector3 ToNative(Vector3 authored) => new(authored.Z, authored.Y, authored.X);
}
