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
        get => _levels.Count == 0 ? 0 : Math.Clamp((int)Math.Round(LevelSlider.Value), 0, _levels.Count - 1);
        set
        {
            if (_levels.Count == 0) return;
            var target = ResolveAvailableIndex(Math.Clamp(value, 0, _levels.Count - 1), value >= SelectedIndex);
            _updating = true;
            LevelSlider.Value = target;
            _updating = false;
            UpdateSelectionText();
        }
    }

    public void SetLevels(IReadOnlyList<BlendLevelItem> levels)
    {
        _levels = levels.Take(SlotCount).ToArray();
        _updating = true;
        LevelSlider.Minimum = 0;
        LevelSlider.Maximum = SlotCount - 1;
        LevelSlider.IsEnabled = _levels.Count(level => level.IsAvailable) > 1;
        var current = _levels.Count == 0 ? 0 : Math.Clamp(SelectedIndex, 0, _levels.Count - 1);
        LevelSlider.Value = _levels.Count == 0 ? 0 : ResolveAvailableIndex(current, true);

        LabelsPanel.Children.Clear();
        TickPanel.Children.Clear();
        for (var index = 0; index < SlotCount; index++)
        {
            var level = index < _levels.Count ? _levels[index] : new BlendLevelItem($"{index + 1}", string.Empty, false);
            LabelsPanel.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(level.Name) ? $"{index + 1}" : level.Name,
                FontSize = 11,
                Foreground = (Brush)FindResource(level.IsAvailable ? "MutedTextBrush" : "BorderBrush"),
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0)
            });
            TickPanel.Children.Add(new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = (Brush)FindResource(level.IsAvailable ? "MutedTextBrush" : "BorderBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        _updating = false;
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

    private void UpdateSelectionText()
    {
        if (_levels.Count == 0)
        {
            SelectionText.Text = string.Empty;
            SelectionSummaryText.Text = string.Empty;
            return;
        }

        var index = SelectedIndex;
        var item = _levels.ElementAtOrDefault(index);
        SelectionText.Text = item?.IsAvailable == true ? item.Name : string.Empty;
        SelectionSummaryText.Text = item?.IsAvailable == true ? item.Summary : string.Empty;
        SelectionText.ToolTip = SelectionSummaryText.Text;
        for (var labelIndex = 0; labelIndex < LabelsPanel.Children.Count; labelIndex++)
        {
            if (LabelsPanel.Children[labelIndex] is not TextBlock label) continue;
            var selected = labelIndex == index;
            var available = _levels.ElementAtOrDefault(labelIndex)?.IsAvailable == true;
            label.Foreground = (Brush)FindResource(selected ? "AccentBrush" : available ? "MutedTextBrush" : "BorderBrush");
            label.FontWeight = selected ? FontWeights.SemiBold : FontWeights.Medium;
        }
    }
}

public sealed class BlendLevelChangedEventArgs(int selectedIndex, BlendLevelItem item) : EventArgs
{
    public int SelectedIndex { get; } = selectedIndex;
    public BlendLevelItem Item { get; } = item;
}
