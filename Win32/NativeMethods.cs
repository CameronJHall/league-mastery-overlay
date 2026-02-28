using System;
using System.Runtime.InteropServices;

namespace league_mastery_overlay.Win32;

internal static class NativeMethods
{
    public static void SetPerMonitorDpiAwareness()
    {
        SetProcessDpiAwarenessContext(
            DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        );
    }

    public static void MakeWindowClickThrough(IntPtr hwnd)
    {
        int style = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(
            hwnd,
            GWL_EXSTYLE,
            style | WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW
        );
    }

    /// <summary>
    /// Returns the HWND of the window that currently has keyboard focus.
    /// </summary>
    public static IntPtr GetForegroundWindow() => _GetForegroundWindow();

    #region Win32
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_TOOLWINDOW = 0x80;

    private static readonly IntPtr DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2 =
        new(-4);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

     [DllImport("user32.dll")]
    private static extern int SetWindowLong(
        IntPtr hWnd,
        int nIndex,
        int dwNewLong
    );

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(
        IntPtr value
    );

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern IntPtr _GetForegroundWindow();

    #endregion
}