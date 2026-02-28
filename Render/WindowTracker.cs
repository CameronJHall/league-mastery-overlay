using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using league_mastery_overlay.Win32;

namespace league_mastery_overlay.Util;

public sealed class WindowTracker
{
    /// <summary>
    /// Snapshot of the last <see cref="UpdatePosition"/> call.
    /// Read by the renderer each frame to display a debug panel.
    /// </summary>
    public record DebugSnapshot(
        string ForegroundTitle,
        uint   ForegroundPid,
        uint   LeaguePid,
        bool   PidMatch,
        string OverlayVisibility
    );

    private readonly Window _overlayWindow;
    private Process? _targetProcess;

    public DebugSnapshot? LastDebug { get; private set; }

    /// <summary>
    /// True when the League client owns the foreground window.
    /// The renderer reads this each tick to show or collapse canvas content
    /// without touching Window.Visibility (which causes flicker).
    /// </summary>
    public bool IsLeagueForegrounded { get; private set; }

    public WindowTracker(Window overlayWindow)
    {
        _overlayWindow = overlayWindow;
    }

    public void UpdatePosition()
    {
        // Find LeagueClientUx process if we don't have it or it exited
        if (_targetProcess == null || _targetProcess.HasExited)
        {
            var processes = Process.GetProcessesByName("LeagueClientUx");
            _targetProcess = processes.FirstOrDefault();

            if (_targetProcess == null)
            {
                Debug.WriteLine("[WindowTracker] LeagueClientUx not found");
                IsLeagueForegrounded = false;
                ParkOffScreen();
                LastDebug = new DebugSnapshot("n/a", 0, 0, false, "Hidden – no League process");
                return;
            }

            Debug.WriteLine("[WindowTracker] Found LeagueClientUx");
        }

        // Hide the overlay when League does not own the foreground window.
        //
        // We compare process IDs rather than HWNDs because:
        //   1. LeagueClientUx is a multi-window Electron app; the window the
        //      user sees may not be _targetProcess.MainWindowHandle.
        //   2. Process.MainWindowHandle is cached by .NET and can go stale.
        //
        // GetWindowThreadProcessId resolves the PID of whatever window is
        // currently in the foreground, which is always fresh.
        IntPtr foregroundHwnd = NativeMethods.GetForegroundWindow();
        GetWindowThreadProcessId(foregroundHwnd, out uint foregroundPid);

        // Resolve the title of the foreground window for display in the debug panel
        var titleSb = new System.Text.StringBuilder(256);
        GetWindowText(foregroundHwnd, titleSb, titleSb.Capacity);
        string foregroundTitle = titleSb.Length > 0 ? titleSb.ToString() : $"hwnd=0x{foregroundHwnd:X}";

        uint leaguePid = (uint)_targetProcess.Id;
        bool pidMatch  = foregroundPid == leaguePid;

        if (!pidMatch)
        {
            IsLeagueForegrounded = false;
            ParkOffScreen();
            LastDebug = new DebugSnapshot(foregroundTitle, foregroundPid, leaguePid, false,
                "Hidden – foreground is not League");
            return;
        }

        // League owns the foreground — get its window rect and show the overlay.
        // Use the actual foreground HWND (not MainWindowHandle) for the rect so
        // the overlay snaps to whichever League window is on top.
        if (!GetWindowRect(foregroundHwnd, out RECT rect))
        {
            Debug.WriteLine("[WindowTracker] Failed to get window rect");
            IsLeagueForegrounded = false;
            ParkOffScreen();
            LastDebug = new DebugSnapshot(foregroundTitle, foregroundPid, leaguePid, true,
                "Hidden – GetWindowRect failed");
            return;
        }

        _overlayWindow.Left   = rect.Left;
        _overlayWindow.Top    = rect.Top;
        _overlayWindow.Width  = rect.Right  - rect.Left;
        _overlayWindow.Height = rect.Bottom - rect.Top;
        IsLeagueForegrounded = true;

        LastDebug = new DebugSnapshot(foregroundTitle, foregroundPid, leaguePid, true, "Visible");
    }

    /// <summary>
    /// Moves the overlay window to a 1x1 area far off-screen so it stops
    /// covering the user's desktop while staying open (avoids the repaint
    /// flicker that toggling Window.Visibility causes).
    /// </summary>
    private void ParkOffScreen()
    {
        _overlayWindow.Left   = -32000;
        _overlayWindow.Top    = -32000;
        _overlayWindow.Width  = 1;
        _overlayWindow.Height = 1;
    }

    #region Win32 Interop

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint lpdwProcessId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hwnd, System.Text.StringBuilder lpString, int nMaxCount);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion
}
