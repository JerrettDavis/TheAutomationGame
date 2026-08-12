using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class FullscreenWindow
{
    private const int GwlStyle = -16;
    private const long WsPopup = 0x80000000L;
    private const long WsVisible = 0x10000000L;
    private const uint SwpNoZOrder = 0x0004;
    private const uint SwpNoActivate = 0x0010;
    private const uint SwpFrameChanged = 0x0020;

    public static DisplayWorkArea LeftmostWorkArea
    {
        get
        {
            DisplayWorkArea? leftmost = null;
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (monitor, _, _, _) =>
            {
                var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
                if (!GetMonitorInfo(monitor, ref info)) return true;
                var candidate = new DisplayWorkArea(
                    info.WorkArea.Left,
                    info.WorkArea.Top,
                    info.WorkArea.Right - info.WorkArea.Left,
                    info.WorkArea.Bottom - info.WorkArea.Top);
                if (leftmost is null || candidate.X < leftmost.Value.X) leftmost = candidate;
                return true;
            }, IntPtr.Zero);
            return leftmost ?? new DisplayWorkArea(0, 0, GetSystemMetrics(0), GetSystemMetrics(1));
        }
    }

    public static Task ApplyBorderlessToLeftmostWhenReadyAsync(DisplayWorkArea workArea) =>
        ApplyToLeftmostWhenReadyAsync(workArea, workArea.Width, workArea.Height, borderless: true);

    public static Task ApplyWindowedToLeftmostWhenReadyAsync(DisplayWorkArea workArea, int width, int height) =>
        ApplyToLeftmostWhenReadyAsync(workArea,
            Math.Min(width, workArea.Width),
            Math.Min(height, workArea.Height),
            borderless: false);

    private static async Task ApplyToLeftmostWhenReadyAsync(
        DisplayWorkArea workArea,
        int width,
        int height,
        bool borderless)
    {
        try
        {
            var process = Process.GetCurrentProcess();
            var deadline = DateTime.UtcNow.AddSeconds(15);
            while (process.MainWindowHandle == IntPtr.Zero && DateTime.UtcNow < deadline)
            {
                await Task.Delay(50).ConfigureAwait(false);
                process.Refresh();
            }

            var window = process.MainWindowHandle;
            if (window == IntPtr.Zero) return;

            if (borderless) SetWindowLongPtr(window, GwlStyle, new IntPtr(WsPopup | WsVisible));
            var x = workArea.X + (workArea.Width - width) / 2;
            var y = workArea.Y + (workArea.Height - height) / 2;
            SetWindowPos(window, IntPtr.Zero,
                x,
                y,
                width,
                height,
                SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
        }
        catch
        {
            // Window bootstrap must never prevent the simulation client from starting.
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public int Size;
        public NativeRect Monitor;
        public NativeRect WorkArea;
        public uint Flags;
    }

    public readonly record struct DisplayWorkArea(int X, int Y, int Width, int Height);

    private delegate bool MonitorEnumProc(IntPtr monitor, IntPtr deviceContext, IntPtr monitorRect, IntPtr data);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(IntPtr deviceContext, IntPtr clipRect, MonitorEnumProc callback, IntPtr data);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int index);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr newValue);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);
}
