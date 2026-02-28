using System.Windows;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using Hardcodet.Wpf.TaskbarNotification;
using league_mastery_overlay.Util;

namespace league_mastery_overlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private TaskbarIcon _trayIcon;
    private MainWindow M => Current.MainWindow as MainWindow;
    protected override void OnStartup(StartupEventArgs e)
    {
        // Check if another instance is already running
        if (!SingleInstanceManager.AcquireInstance())
        {
            Current.Shutdown();
            return;
        }

        // Catch UI thread exceptions
        DispatcherUnhandledException += (sender, args) =>
        {
            Debug.WriteLine($"[FATAL] UI Exception: {args.Exception}");
            System.Windows.MessageBox.Show($"UI Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true; // Keep app running
        };

        // Catch background thread exceptions
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            Debug.WriteLine($"[FATAL] Background Exception: {ex}");
            MessageBox.Show($"Background Error: {ex?.Message}\n\n{ex?.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        // Catch async/Task exceptions
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            Debug.WriteLine($"[FATAL] Task Exception: {args.Exception}");
            MessageBox.Show($"Task Error: {args.Exception.Message}\n\n{args.Exception.StackTrace}", 
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.SetObserved(); // Prevent app termination
        };
        
        Win32.NativeMethods.SetPerMonitorDpiAwareness();
        base.OnStartup(e);
        
        _trayIcon = (TaskbarIcon)FindResource("TrayIcon");
        _trayIcon.ShowBalloonTip("League Mastery Overlay", "Overlay is running.", BalloonIcon.None);
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        _trayIcon?.Dispose();
        SingleInstanceManager.ReleaseInstance();
        base.OnExit(e);
    }
    
    #region Tray Menu Handlers

    private void ToggleMasteryIconSet_Click(object sender, RoutedEventArgs e)
    {
        M?.ToggleMasteryIconSet();
        Debug.WriteLine("[App] Mastery icon set toggled from tray menu");
    }

    private void ToggleDebugGrid_Click(object sender, RoutedEventArgs e)
    {
        M?.ToggleDebugGrid();
        Debug.WriteLine("[App] Debug grid toggled from tray menu");
    }

    private void ToggleDebugPanel_Click(object sender, RoutedEventArgs e)
    {
        M?.ToggleDebugPanel();
        Debug.WriteLine("[App] Debug panel toggled from tray menu");
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        var owner = Current.MainWindow;
        MessageBox.Show(
            owner,
            "League Mastery Overlay\n\n" +
            "Track your champion mastery progress during ARAM champ select.",
            "About",
            MessageBoxButton.OK,
            MessageBoxImage.None
        );
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Current.Shutdown();
    }

    #endregion
}