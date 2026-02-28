using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using league_mastery_overlay.Layout;
using league_mastery_overlay.Render;

namespace league_mastery_overlay.DebugTools;

/// <summary>
/// Interactive tuner window for live-editing anchor values without restarting the app.
/// Only included in Debug builds — the entire DevTools/ folder is excluded from Release.
/// </summary>
public partial class AnchorTunerWindow : Window
{
    private readonly OverlayLayout _layout;
    private readonly OverlayRenderer _renderer;
    private readonly AnchorOverrides _overrides;

    // All registered rows, iterated when copying to clipboard.
    private readonly List<TunerRow> _rows = new();

    public AnchorTunerWindow(OverlayLayout layout, OverlayRenderer renderer)
    {
        InitializeComponent();
        _layout    = layout;
        _renderer  = renderer;
        _overrides = new AnchorOverrides();
        _layout.SetOverrides(_overrides);

        BuildRows();
    }

    // ── Row construction ──────────────────────────────────────────────────────

    private void BuildRows()
    {
        // Champ Select
        AddRow(Row_BenchBase_X,    "BenchBase X",        Anchors.BenchBase.X,       0.0001, v => _overrides.BenchBaseX      = v);
        AddRow(Row_BenchBase_Y,    "BenchBase Y",        Anchors.BenchBase.Y,       0.0001, v => _overrides.BenchBaseY      = v);
        AddRow(Row_BenchXSpacing,  "BenchXSpacing",      Anchors.BenchXSpacing,     0.0001, v => _overrides.BenchXSpacing   = v);
        AddRow(Row_PlayerChamp_X,  "PlayerChampion X",   Anchors.PlayerChampion.X,  0.0001, v => _overrides.PlayerChampionX = v);
        AddRow(Row_PlayerChamp_Y,  "PlayerChampion Y",   Anchors.PlayerChampion.Y,  0.0001, v => _overrides.PlayerChampionY = v);

        // Lobby
        AddRow(Row_LobbyCard_X,      "LobbyCard X",         Anchors.LobbyCardFirst.X,  0.001, v => _overrides.LobbyCardFirstX   = v);
        AddRow(Row_LobbyCard_Y,      "LobbyCard Y",         Anchors.LobbyCardFirst.Y,  0.001, v => _overrides.LobbyCardFirstY   = v);
        AddRow(Row_LobbyCardSpacing, "LobbyCard X Spacing", Anchors.LobbyCardXSpacing, 0.001, v => _overrides.LobbyCardXSpacing = v);
        AddRow(Row_LobbyCardSize_W,  "LobbyCard Width",     Anchors.LobbyCardSize.W,   0.001, v => _overrides.LobbyCardSizeW    = v);
        AddRow(Row_LobbyCardSize_H,  "LobbyCard Height",    Anchors.LobbyCardSize.H,   0.001, v => _overrides.LobbyCardSizeH    = v);
    }

    private void AddRow(StackPanel container, string label, double defaultValue,
                        double step, Action<double> applyOverride)
    {
        var row = new TunerRow(label, defaultValue, step, v =>
        {
            applyOverride(v);
            _layout.ForceRecalculate();
        });

        row.BuildInto(container, GetLabelStyle(), GetNumBoxStyle(), GetSpinStyle());
        _rows.Add(row);
    }

    // ── Button handlers ───────────────────────────────────────────────────────

    private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("// ── Paste into Layout/Anchors.cs ──");
        foreach (var row in _rows)
            sb.AppendLine(row.ToAnchorLine());
        Clipboard.SetText(sb.ToString());
    }

    private void ResetToDefaults_Click(object sender, RoutedEventArgs e)
    {
        foreach (var row in _rows)
            row.ResetToDefault();
        _overrides.ResetAll();
        _layout.ForceRecalculate();
    }

    private void ForceRender_Changed(object sender, RoutedEventArgs e)
    {
        bool on = ((System.Windows.Controls.Primitives.ToggleButton)sender).IsChecked == true;
        _renderer.ForceRender = on;
        ForceRenderLabel.Text = on ? "Force render: ON " : "Force render: OFF";
    }

    private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Detach overrides so the main overlay goes back to Anchors defaults.
        _layout.SetOverrides(null);
        _layout.ForceRecalculate();
        // Always restore normal foreground-gating on close.
        _renderer.ForceRender = false;
    }

    // ── Style helpers (defined in code so the window stays purely code-driven) ─

    private Style GetLabelStyle()
    {
        var s = new Style(typeof(TextBlock));
        s.Setters.Add(new Setter(TextBlock.ForegroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 170, 170, 204))));
        s.Setters.Add(new Setter(FrameworkElement.WidthProperty, 180.0));
        s.Setters.Add(new Setter(FrameworkElement.VerticalAlignmentProperty, VerticalAlignment.Center));
        return s;
    }

    private Style GetNumBoxStyle()
    {
        var s = new Style(typeof(TextBox));
        s.Setters.Add(new Setter(Control.BackgroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 13, 13, 26))));
        s.Setters.Add(new Setter(Control.ForegroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 224, 224, 255))));
        s.Setters.Add(new Setter(Control.BorderBrushProperty,
            new SolidColorBrush(Color.FromArgb(255, 68, 68, 119))));
        s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(3, 1, 3, 1)));
        s.Setters.Add(new Setter(FrameworkElement.WidthProperty, 80.0));
        s.Setters.Add(new Setter(TextBox.TextAlignmentProperty, TextAlignment.Right));
        s.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
        return s;
    }

    private Style GetSpinStyle()
    {
        var s = new Style(typeof(RepeatButton));
        s.Setters.Add(new Setter(Control.BackgroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 34, 34, 68))));
        s.Setters.Add(new Setter(Control.ForegroundProperty,
            new SolidColorBrush(Color.FromArgb(255, 176, 176, 255))));
        s.Setters.Add(new Setter(Control.BorderBrushProperty,
            new SolidColorBrush(Color.FromArgb(255, 68, 68, 119))));
        s.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(1)));
        s.Setters.Add(new Setter(FrameworkElement.WidthProperty, 20.0));
        s.Setters.Add(new Setter(FrameworkElement.HeightProperty, 16.0));
        s.Setters.Add(new Setter(Control.FontSizeProperty, 9.0));
        s.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        s.Setters.Add(new Setter(RepeatButton.DelayProperty, 400));
        s.Setters.Add(new Setter(RepeatButton.IntervalProperty, 60));
        return s;
    }

    // ── TunerRow ──────────────────────────────────────────────────────────────

    /// <summary>
    /// One label + text box + up/down spinner pair for a single anchor value.
    /// </summary>
    private sealed class TunerRow
    {
        private readonly string  _label;
        private readonly double  _default;
        private readonly double  _step;
        private readonly Action<double> _apply;

        private double   _value;
        private TextBox? _box;

        public TunerRow(string label, double defaultValue, double step, Action<double> apply)
        {
            _label   = label;
            _default = defaultValue;
            _step    = step;
            _apply   = apply;
            _value   = defaultValue;
        }

        public void BuildInto(StackPanel container, Style labelStyle, Style numBoxStyle, Style spinStyle)
        {
            var label = new TextBlock { Text = _label, Style = labelStyle };

            _box = new TextBox
            {
                Text  = FormatValue(_value),
                Style = numBoxStyle,
            };
            _box.LostFocus  += OnBoxLostFocus;
            _box.KeyDown    += OnBoxKeyDown;

            var up = new RepeatButton { Content = "▲", Style = spinStyle };
            up.Click += (_, _) => Nudge(+_step);

            var down = new RepeatButton { Content = "▼", Style = spinStyle, Margin = new Thickness(0, 1, 0, 0) };
            down.Click += (_, _) => Nudge(-_step);

            var spinStack = new StackPanel { Orientation = Orientation.Vertical, Margin = new Thickness(2, 0, 0, 0) };
            spinStack.Children.Add(up);
            spinStack.Children.Add(down);

            container.Children.Add(label);
            container.Children.Add(_box);
            container.Children.Add(spinStack);
        }

        public void ResetToDefault()
        {
            _value = _default;
            if (_box != null) _box.Text = FormatValue(_value);
            _apply(_value);
        }

        /// <summary>Returns a ready-to-paste Anchors.cs-style line for this row.</summary>
        public string ToAnchorLine() =>
            $"    // {_label} = {FormatValue(_value)};";

        private void Nudge(double delta)
        {
            _value = Math.Round(_value + delta, 6);
            if (_box != null) _box.Text = FormatValue(_value);
            _apply(_value);
        }

        private void Commit(string text)
        {
            if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out double v))
            {
                _value = v;
                _apply(_value);
            }
            // Revert box to last valid value on bad input
            if (_box != null) _box.Text = FormatValue(_value);
        }

        private void OnBoxLostFocus(object sender, RoutedEventArgs e) =>
            Commit(((TextBox)sender).Text);

        private void OnBoxKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
                Commit(((TextBox)sender).Text);
        }

        private static string FormatValue(double v) =>
            v.ToString("G6", CultureInfo.InvariantCulture);
    }
}
