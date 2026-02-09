using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using league_mastery_overlay.Render;
using league_mastery_overlay.State;
using league_mastery_overlay.Win32;
using league_mastery_overlay.League;
using league_mastery_overlay.Util;
using System.Diagnostics;

namespace league_mastery_overlay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StateStore _stateStore = new();
    private OverlayRenderer? _renderer;
    private DispatcherTimer? _renderTimer;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        NativeMethods.MakeWindowClickThrough(
            new WindowInteropHelper(this).Handle
        );
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _renderer = new OverlayRenderer(RootCanvas, _stateStore);

        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _renderTimer.Tick += (_, _) => _renderer.Render();
        _renderTimer.Start();

        var authProvider = new LcuAuthProvider();

        var lcuLoop = new PollingLoop(async () =>
        {
            Debug.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Polling tick started");
            
            if (!authProvider.TryGetAuth(out var auth))
            {
                Debug.WriteLine("  → No auth available");
                Dispatcher.Invoke(() => DebugText.Text = "Waiting for League...");
                return;
            }
            
            Debug.WriteLine($"  → Auth found: {auth.Port}");
            
            var client = new LcuClient(auth);
            var service = new ChampionSelectService(client);
            
            var champSelect = await service.PollAsync();
            Debug.WriteLine($"  → ChampSelect result: {champSelect != null}");
            
            _stateStore.Update(
                new LeagueState(
                    champSelect != null ? GamePhase.ChampSelect : GamePhase.None,
                    champSelect
                )
            );

        }, TimeSpan.FromMilliseconds(500));

        lcuLoop.Start();

        
        // TODO: start League window tracking
    }
}