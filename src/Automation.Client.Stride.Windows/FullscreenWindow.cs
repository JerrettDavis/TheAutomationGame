using System.Diagnostics;
using System.Runtime.InteropServices;

internal static class FullscreenWindow
{
    private const int GwlStyle = -16;
    private const long WsPopup = 0x80000000L;
    private const long WsVisible = 0x10000000L;
    private const uint MonitorDefaultToNearest = 2;
    private const uint SwpFrameChanged = 0x0020;
    private static readonly IntPtr HwndTop = IntPtr.Zero;

    public static (int Width, int Height) PrimaryDisplaySize => (GetSystemMetrics(0), GetSystemMetrics(1));

    public static async Task ApplyWhenReadyAsync()
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
            var monitor = MonitorFromWindow(window, MonitorDefaultToNearest);
            var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info)) return;

            SetWindowLongPtr(window, GwlStyle, new IntPtr(WsPopup | WsVisible));
            SetWindowPos(window, HwndTop,
                info.Monitor.Left,
                info.Monitor.Top,
                info.Monitor.Right - info.Monitor.Left,
                info.Monitor.Bottom - info.Monitor.Top,
                SwpFrameChanged);
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

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

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
