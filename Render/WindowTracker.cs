using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace league_mastery_overlay.Util;

public sealed class WindowTracker
{
    private readonly Window _overlayWindow;
    private Process? _targetProcess;

    public WindowTracker(Window overlayWindow)
    {
        _overlayWindow = overlayWindow;
    }

    public void UpdatePosition()
    {
        // Find LeagueClientUx process if we don't have it
        if (_targetProcess == null || _targetProcess.HasExited)
        {
            var processes = Process.GetProcessesByName("LeagueClientUx");
            _targetProcess = processes.FirstOrDefault();
            
            if (_targetProcess == null)
            {
                Debug.WriteLine("[WindowTracker] LeagueClientUx not found");
                return;
            }
            
            Debug.WriteLine("[WindowTracker] Found LeagueClientUx");
        }

        // Get the main window handle
        IntPtr hwnd = _targetProcess.MainWindowHandle;
        if (hwnd == IntPtr.Zero)
        {
            Debug.WriteLine("[WindowTracker] No main window handle");
            return;
        }

        // Get window rectangle
        if (!GetWindowRect(hwnd, out RECT rect))
        {
            Debug.WriteLine("[WindowTracker] Failed to get window rect");
            return;
        }

        // Update overlay position and size to match League client
        _overlayWindow.Left = rect.Left;
        _overlayWindow.Top = rect.Top;
        _overlayWindow.Width = rect.Right - rect.Left;
        _overlayWindow.Height = rect.Bottom - rect.Top;
    }

    #region Win32 Interop

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

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