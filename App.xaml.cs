using System.Configuration;
using System.Data;
using System.Windows;

namespace league_mastery_overlay;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        Win32.NativeMethods.SetPerMonitorDpiAwareness();
        base.OnStartup(e);
    }
}