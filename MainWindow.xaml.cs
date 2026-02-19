using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using league_mastery_overlay.Render;
using league_mastery_overlay.State;
using league_mastery_overlay.Win32;
using league_mastery_overlay.League;
using league_mastery_overlay.Util;
using league_mastery_overlay.Layout;
using System.Diagnostics;

namespace league_mastery_overlay;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly StateStore _stateStore = new();
    private readonly ILeagueClient _league = new LeagueClient();
    private OverlayRenderer? _renderer;
    private DispatcherTimer? _renderTimer;
    private WindowTracker? _windowTracker;
    private GridMapper? _gridMapper;
    private PollingLoop? _gamePhaseLoop;
    private PollingLoop? _champSelectLoop;

    private GamePhase _currentPhase = GamePhase.None;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    public void ToggleDebugGrid()
    {
        _gridMapper?.Toggle();
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
        Debug.WriteLine("[MainWindow] OnLoaded fired");
        _gridMapper = new GridMapper(RootCanvas);
        var layout = new OverlayLayout();
        _renderer = new OverlayRenderer(RootCanvas, _stateStore, layout, _gridMapper);
        _windowTracker = new WindowTracker(this);

        // Render timer - updates UI at ~30 FPS
        _renderTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _renderTimer.Tick += (_, _) =>
        {
            _renderer.UpdateWindowSize(new Size(RootCanvas.ActualWidth, RootCanvas.ActualHeight));
            _renderer.Render();
            _windowTracker.UpdatePosition();
        };
        _renderTimer.Start();

        // polls champion select data while in champion select
        _champSelectLoop = new PollingLoop(async () =>
        {
            var state = await _league.GetChampionSelectStateAsync();
            _stateStore.UpdateChampionSelectState(state);
        }, TimeSpan.FromMilliseconds(500));

        // manages connection lifecycle and drives state machine transitions
        _gamePhaseLoop = new PollingLoop(async () =>
        {
            if (_league.IsConnected && !await _league.IsStillAliveAsync())
            {
                Debug.WriteLine("[MainWindow] League client disconnected");
                _league.Disconnect();
            }

            if (!_league.TryConnect())
            {
                TransitionTo(GamePhase.None);
                return;
            }

            var phase = await _league.GetGamePhaseAsync();
            if (phase != _currentPhase)
            {
                Debug.WriteLine("[MainWindow] Detected game phase change: " + phase);
                TransitionTo(phase);
            }
            

        }, TimeSpan.FromSeconds(2));

        _gamePhaseLoop.Start();
    }

    /// <summary>
    /// Handles state transitions — fires OnExit/OnEnter side effects only when the phase changes.
    /// </summary>
    private void TransitionTo(GamePhase newPhase)
    {
        if (newPhase == _currentPhase)
            return;

        Debug.WriteLine($"[MainWindow] Phase transition: {_currentPhase} → {newPhase}");

        // OnExit current state
        switch (_currentPhase)
        {
            case GamePhase.ChampSelect:
                _champSelectLoop?.Stop();
                _stateStore.UpdateChampionSelectState(null);
                break;
        }

        // OnEnter new state
        switch (newPhase)
        {
            case GamePhase.ChampSelect:
                _champSelectLoop?.Start();
                break;
        }

        _currentPhase = newPhase;
        _stateStore.UpdateGamePhase(newPhase);
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _gamePhaseLoop?.Stop();
        _champSelectLoop?.Stop();
        _league.Dispose();

        var trayIcon = TryFindResource("TrayIcon") as Hardcodet.Wpf.TaskbarNotification.TaskbarIcon;
        trayIcon?.Dispose();
    }
}
