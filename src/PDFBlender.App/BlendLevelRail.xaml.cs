using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PDFBlender.App;

public sealed record BlendLevelItem(string Name, string Summary, bool IsAvailable = true);

public partial class BlendLevelRail : UserControl
{
    private const int SlotCount = 5;
    private const double RailStartRatio = 0.1;
    private const double RailTravelRatio = 0.8;
    private IReadOnlyList<BlendLevelItem> _levels = [];
    private bool _updating;

    public event EventHandler<BlendLevelChangedEventArgs>? SelectionChanged;
    public event RoutedEventHandler? EditRequested;

    public IReadOnlyList<BlendLevelItem> Levels => _levels;

    public BlendLevelRail()
    {
        InitializeComponent();
        LevelSlider.PreviewKeyDown += LevelSlider_PreviewKeyDown;
        SetLevels([]);
    }

    public int SelectedIndex
    {
        get => _levels.Count == 0 ? 0 : Math.Clamp((int)Math.Round(LevelSlider.Value), 0, SlotCount - 1);
        set
        {
            if (_levels.Count == 0) return;
            var target = ResolveAvailableIndex(Math.Clamp(value, 0, SlotCount - 1), value >= SelectedIndex);
            _updating = true;
            LevelSlider.Value = target;
            _updating = false;
            UpdateSelectionText();
        }
    }

    public void SetLevels(IReadOnlyList<BlendLevelItem> levels)
    {
        var previousIndex = _levels.Count == 0 ? 0 : Math.Clamp((int)Math.Round(LevelSlider.Value), 0, SlotCount - 1);
        _levels = levels.Take(SlotCount).ToArray();
        _updating = true;
        LevelSlider.Minimum = 0;
        LevelSlider.Maximum = SlotCount - 1;
        LevelSlider.IsEnabled = _levels.Count(level => level.IsAvailable) > 1;
        var current = _levels.Count == 0 ? 0 : Math.Clamp(previousIndex, 0, SlotCount - 1);
        LevelSlider.Value = _levels.Count == 0 ? 0 : ResolveAvailableIndex(current, true);

        NumbersPanel.Children.Clear();
        UpperTickPanel.Children.Clear();
        LowerTickPanel.Children.Clear();
        LabelsPanel.Children.Clear();
        for (var index = 0; index < SlotCount; index++)
        {
            var level = index < _levels.Count ? _levels[index] : new BlendLevelItem($"{index + 1}", string.Empty, false);
            var availableBrush = (Brush)FindResource(level.IsAvailable ? "MutedTextBrush" : "BorderBrush");
            NumbersPanel.Children.Add(new TextBlock
            {
                Text = $"{index + 1}",
                Width = 22,
                Height = 16,
                FontSize = 11,
                Foreground = availableBrush,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });
            UpperTickPanel.Children.Add(CreateTick(availableBrush));
            LowerTickPanel.Children.Add(CreateTick(availableBrush));
            LabelsPanel.Children.Add(new TextBlock
            {
                Text = level.IsAvailable && !string.IsNullOrWhiteSpace(level.Name) ? level.Name : "—",
                FontSize = 11,
                Height = 18,
                Foreground = availableBrush,
                FontWeight = FontWeights.Medium,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0)
            });
        }

        _updating = false;
        PositionRailMarkers();
        UpdateSelectionText();
    }

    public void SetCaption(string caption) => CaptionText.Text = caption;

    public void SetAccessibilityName(string name) => AutomationProperties.SetName(LevelSlider, name);

    public void SetEditAccessibilityName(string name)
    {
        AutomationProperties.SetName(EditButton, name);
        EditButton.ToolTip = name;
    }

    private void EditButton_Click(object sender, RoutedEventArgs e) => EditRequested?.Invoke(this, e);

    private void LevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_levels.Count == 0) return;
        var candidate = Math.Clamp((int)Math.Round(e.NewValue), 0, SlotCount - 1);
        var wasIncreasing = e.NewValue >= e.OldValue;
        if (_levels.ElementAtOrDefault(candidate)?.IsAvailable != true)
        {
            var available = ResolveAvailableIndex(candidate, wasIncreasing);
            _updating = true;
            LevelSlider.Value = available;
            _updating = false;
            candidate = available;
        }

        UpdateSelectionText();
        if (!_updating && _levels.ElementAtOrDefault(candidate)?.IsAvailable == true)
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(candidate, _levels[candidate]));
    }

    private void LevelSlider_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_levels.Count == 0) return;
        var current = SelectedIndex;
        var target = e.Key switch
        {
            Key.Home => ResolveAvailableIndex(0, true),
            Key.End => ResolveAvailableIndex(SlotCount - 1, false),
            Key.Left or Key.Down => PreviousAvailable(current),
            Key.Right or Key.Up => NextAvailable(current),
            _ => current
        };
        if (target == current && e.Key is not (Key.Home or Key.End)) return;

        _updating = true;
        LevelSlider.Value = target;
        _updating = false;
        UpdateSelectionText();
        if (target != current)
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(target, _levels[target]));
        e.Handled = true;
    }

    private int ResolveAvailableIndex(int candidate, bool forward)
    {
        if (_levels.ElementAtOrDefault(candidate)?.IsAvailable == true) return candidate;
        if (!_levels.Any(level => level.IsAvailable)) return 0;
        return forward ? NextAvailable(candidate - 1) : PreviousAvailable(candidate + 1);
    }

    private int NextAvailable(int start)
    {
        for (var index = Math.Max(0, start + 1); index < SlotCount; index++)
            if (_levels.ElementAtOrDefault(index)?.IsAvailable == true) return index;
        return PreviousAvailable(SlotCount);
    }

    private int PreviousAvailable(int start)
    {
        for (var index = Math.Min(SlotCount - 1, start - 1); index >= 0; index--)
            if (_levels.ElementAtOrDefault(index)?.IsAvailable == true) return index;
        return NextAvailable(-1);
    }

    private Rectangle CreateTick(Brush brush) => new()
    {
        Width = 2,
        Height = 6,
        Fill = brush,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center,
        RadiusX = 1,
        RadiusY = 1
    };

    private void RailPanel_SizeChanged(object sender, SizeChangedEventArgs e) => PositionRailMarkers();

    private void PositionRailMarkers()
    {
        // All marker layers must use one measured rail width. Measuring each Canvas
        // independently causes text and ticks to shift when the page is re-laid out.
        var railWidth = NumbersPanel.ActualWidth;
        if (railWidth <= 0 || NumbersPanel.Children.Count == 0) return;

        PositionMarkers(NumbersPanel, railWidth, markerWidth: 22);
        PositionMarkers(UpperTickPanel, railWidth, markerWidth: 2);
        PositionMarkers(LowerTickPanel, railWidth, markerWidth: 2);
        var labelWidth = Math.Max(24, railWidth * RailTravelRatio / (SlotCount - 1));
        foreach (var label in LabelsPanel.Children.OfType<TextBlock>()) label.Width = labelWidth;
        PositionMarkers(LabelsPanel, railWidth, labelWidth);
    }

    private static void PositionMarkers(Canvas panel, double railWidth, double markerWidth)
    {
        if (railWidth <= 0 || panel.Children.Count == 0) return;
        var interval = railWidth * RailTravelRatio / (SlotCount - 1);
        for (var index = 0; index < panel.Children.Count; index++)
        {
            var marker = panel.Children[index] as FrameworkElement;
            if (marker is null) continue;
            Canvas.SetLeft(marker, railWidth * RailStartRatio + index * interval - markerWidth / 2);
            // Every marker canvas has a fixed row height. Keep its top coordinate
            // stable instead of deriving it from transient layout measurements.
            Canvas.SetTop(marker, 0);
        }
    }

    private void UpdateSelectionText()
    {
        if (_levels.Count == 0)
        {
            IntensityText.Text = string.Empty;
            return;
        }

        var index = SelectedIndex;
        var item = _levels.ElementAtOrDefault(index);
        IntensityText.Text = item?.IsAvailable == true ? item.Summary : string.Empty;
        IntensityText.ToolTip = IntensityText.Text;
        for (var labelIndex = 0; labelIndex < LabelsPanel.Children.Count; labelIndex++)
        {
            if (LabelsPanel.Children[labelIndex] is not TextBlock label) continue;
            var selected = labelIndex == index;
            var available = _levels.ElementAtOrDefault(labelIndex)?.IsAvailable == true;
            label.Foreground = (Brush)FindResource(selected ? "AccentBrush" : available ? "MutedTextBrush" : "BorderBrush");
            label.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Medium;
        }
        UpdateTickBrushes(index);
    }

    private void UpdateTickBrushes(int selectedIndex)
    {
        for (var index = 0; index < SlotCount; index++)
        {
            var available = _levels.ElementAtOrDefault(index)?.IsAvailable == true;
            var brush = (Brush)FindResource(index == selectedIndex && available ? "AccentBrush" : available ? "MutedTextBrush" : "BorderBrush");
            if (index < UpperTickPanel.Children.Count && UpperTickPanel.Children[index] is Rectangle upper) upper.Fill = brush;
            if (index < LowerTickPanel.Children.Count && LowerTickPanel.Children[index] is Rectangle lower) lower.Fill = brush;
            if (index < NumbersPanel.Children.Count && NumbersPanel.Children[index] is TextBlock number) number.Foreground = brush;
        }
    }
}

public sealed class BlendLevelChangedEventArgs(int selectedIndex, BlendLevelItem item) : EventArgs
{
    public int SelectedIndex { get; } = selectedIndex;
    public BlendLevelItem Item { get; } = item;
}
