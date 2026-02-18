using System.Diagnostics;
using System.Windows;

namespace league_mastery_overlay.Util;

/// <summary>
/// Manages single instance enforcement using a named mutex.
/// Prevents multiple instances of the application from running simultaneously.
/// </summary>
public static class SingleInstanceManager
{
    private static readonly string MutexName = "league-mastery-overlay-instance";
    private static System.Threading.Mutex? _instanceMutex;

    /// <summary>
    /// Checks if another instance is already running.
    /// If so, brings that instance to the foreground and exits the current instance.
    /// </summary>
    /// <returns>True if this is the first instance, false if another instance is already running.</returns>
    public static bool AcquireInstance()
    {
        bool isNewInstance = false;
        _instanceMutex = new System.Threading.Mutex(true, MutexName, out isNewInstance);

        if (!isNewInstance)
        {
            Debug.WriteLine("[SingleInstanceManager] Another instance is already running. Exiting.");
            
            // Try to bring the existing instance to the foreground
            try
            {
                var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
                var existingProcess = System.Diagnostics.Process.GetProcessesByName(currentProcess.ProcessName)
                    .FirstOrDefault(p => p.Id != currentProcess.Id);

                if (existingProcess != null && existingProcess.MainWindowHandle != IntPtr.Zero)
                {
                    Win32.NativeMethods.SetForegroundWindow(existingProcess.MainWindowHandle);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SingleInstanceManager] Failed to bring existing instance to foreground: {ex.Message}");
            }

            MessageBox.Show(
                "League Mastery Overlay is already running.",
                "Instance Already Running",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );

            return false;
        }

        Debug.WriteLine("[SingleInstanceManager] First instance acquired successfully.");
        return true;
    }

    /// <summary>
    /// Releases the mutex when the application exits.
    /// </summary>
    public static void ReleaseInstance()
    {
        _instanceMutex?.Dispose();
        Debug.WriteLine("[SingleInstanceManager] Instance mutex released.");
    }
}
