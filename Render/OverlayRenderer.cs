using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using league_mastery_overlay.State;
using league_mastery_overlay.Util;

namespace league_mastery_overlay.Render;

public sealed class OverlayRenderer
{
    private readonly Canvas _root;
    private readonly StateStore _stateStore;
    private readonly Dictionary<int, Border> _championCards = new();
    private readonly GridMapper? _gridMapper;

    public OverlayRenderer(Canvas root, StateStore stateStore, GridMapper? gridMapper = null)
    {
        _root = root;
        _stateStore = stateStore;
        _gridMapper = gridMapper;
    }

    public void Render()
    {
        LeagueState state = _stateStore.Get();

        // Hide overlay if not in champ select
        if (state.Phase != GamePhase.ChampSelect)
        {
            _root.Visibility = Visibility.Collapsed;
            return;
        }

        _root.Visibility = Visibility.Visible;

        // Clear previous render (but preserve debug grid elements)
        var toRemove = _root.Children
            .OfType<FrameworkElement>()
            .Where(e => e.Tag?.ToString() != "DebugGrid")
            .ToList();
        
        foreach (var element in toRemove)
        {
            _root.Children.Remove(element);
        }
        
        _championCards.Clear();

        // Re-render debug grid if active
        _gridMapper?.Render();

        // Show debug info at the top
        var debugPanel = CreateDebugPanel(state);
        Canvas.SetLeft(debugPanel, 10);
        Canvas.SetTop(debugPanel, 10);
        Canvas.SetZIndex(debugPanel, 100);
        _root.Children.Add(debugPanel);

        // Show the selected champion (if any)
        if (state.ChampionSelect?.MyChampion != null)
        {
            var selectedCard = CreateChampionCard(
                state.ChampionSelect.MyChampion, 
                isSelected: true
            );
            Canvas.SetLeft(selectedCard, 10);
            Canvas.SetTop(selectedCard, 80);
            Canvas.SetZIndex(selectedCard, 100);
            _root.Children.Add(selectedCard);
        }

        // Show bench champions
        var benchChampions = state.ChampionSelect?.BenchChampions ?? Array.Empty<ChampionData>();
        for (int i = 0; i < benchChampions.Length && i < 10; i++)
        {
            var card = CreateChampionCard(benchChampions[i], isSelected: false);
        
            // Arrange in 2 columns
            int col = i % 2;
            int row = i / 2;
            Canvas.SetLeft(card, 10 + (col * 220));
            Canvas.SetTop(card, 180 + (row * 80));
            Canvas.SetZIndex(card, 100);
        
            _root.Children.Add(card);
        }
    }

    private Border CreateDebugPanel(LeagueState state)
    {
        var benchCount = state.ChampionSelect?.BenchChampions?.Length ?? 0;
        var hasSelection = state.ChampionSelect?.MyChampion != null;

        var text = new TextBlock
        {
            Text = $"Phase: {state.Phase}\n" +
                   $"Selected: {(hasSelection ? "Yes" : "No")}\n" +
                   $"Bench Champions: {benchCount}",
            Foreground = Brushes.Cyan,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(8)
        };

        return new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
            CornerRadius = new CornerRadius(4),
            Child = text
        };
    }

    private Border CreateChampionCard(ChampionData champion, bool isSelected)
    {
        // Color code by mastery level
        Brush backgroundColor;
        Brush textColor = Brushes.White;
        
        if (champion.Level >= 5) // Mastery 5+
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(200, 0, 200, 0)); // Green - Goal achieved!
        }
        else if (champion.Level >= 3)
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(200, 200, 200, 0)); // Yellow - Getting there
        }
        else if (champion.Level > 0)
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(200, 200, 100, 0)); // Orange - Started
        }
        else
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(200, 150, 0, 0)); // Dark red - Never played
        }

        if (isSelected)
        {
            backgroundColor = new SolidColorBrush(Color.FromArgb(220, 0, 120, 255)); // Blue for selected
        }

        var stack = new StackPanel
        {
            Orientation = Orientation.Vertical,
            Width = 200
        };

        // Header with champion ID and mastery level
        var headerGrid = new Grid
        {
            Margin = new Thickness(8, 4, 8, 2)
        };
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var championName = new TextBlock
        {
            Text = isSelected ? $"★ Champion {champion.Id}" : $"Champion {champion.Id}",
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = textColor,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(championName, 0);
        headerGrid.Children.Add(championName);

        // Mastery level badge
        var masteryBadge = CreateMasteryBadge(champion.Level);
        Grid.SetColumn(masteryBadge, 1);
        headerGrid.Children.Add(masteryBadge);

        stack.Children.Add(headerGrid);

        // Mastery progress bar
        var progressBarBg = new Border
        {
            Height = 8,
            Background = new SolidColorBrush(Color.FromArgb(100, 50, 50, 50)),
            CornerRadius = new CornerRadius(4),
            Margin = new Thickness(8, 2, 8, 2)
        };

        var progressBarFill = new Rectangle
        {
            Height = 8,
            Fill = Brushes.LimeGreen,
            HorizontalAlignment = HorizontalAlignment.Left,
            Width = 184 * champion.MasteryProgress, // 184 = 200 - 16 (padding)
            RadiusX = 4,
            RadiusY = 4
        };

        var progressGrid = new Grid();
        progressGrid.Children.Add(progressBarBg);
        progressGrid.Children.Add(progressBarFill);
        stack.Children.Add(progressGrid);

        // Mastery progress text
        var masteryText = new TextBlock
        {
            Text = $"{champion.MasteryProgress:P0} to level {champion.Level + 1}",
            FontSize = 11,
            Foreground = textColor,
            Padding = new Thickness(8, 2, 8, 4),
            Opacity = 0.8
        };
        stack.Children.Add(masteryText);
        
        
        return new Border
        {
            Background = backgroundColor,
            CornerRadius = new CornerRadius(6),
            BorderBrush = isSelected ? Brushes.Gold : (champion.Level >= 5 ? Brushes.LimeGreen : Brushes.Transparent),
            BorderThickness = new Thickness(isSelected ? 3 : (champion.Level >= 5 ? 2 : 1)),
            Child = stack,
            Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                BlurRadius = 10,
                ShadowDepth = 3,
                Opacity = 0.5
            }
        };
    }

    private Border CreateMasteryBadge(int level)
    {
        // Color the badge based on mastery level
        Brush badgeColor = level switch
        {
            >= 5 => Brushes.Gold,
            >= 3 => new SolidColorBrush(Color.FromRgb(192, 192, 192)), // Silver
            >= 1 => new SolidColorBrush(Color.FromRgb(205, 127, 50)), // Bronze
            _ => Brushes.Gray
        };

        var badge = new Border
        {
            Background = badgeColor,
            CornerRadius = new CornerRadius(3),
            Padding = new Thickness(6, 2, 6, 2),
            Child = new TextBlock
            {
                Text = $"M{level}",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            }
        };

        return badge;
    }
}