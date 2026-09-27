using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace PDFBlender.App;

public sealed record BlendLevelItem(string Name, string Summary, bool IsAvailable = true);

public partial class BlendLevelRail : UserControl
{
    private IReadOnlyList<BlendLevelItem> _levels = [];
    private bool _updating;

    public event EventHandler<BlendLevelChangedEventArgs>? SelectionChanged;

    public IReadOnlyList<BlendLevelItem> Levels => _levels;

    public BlendLevelRail()
    {
        InitializeComponent();
        LevelSlider.PreviewKeyDown += LevelSlider_PreviewKeyDown;
        SetLevels([]);
    }

    public int SelectedIndex
    {
        get => _levels.Count == 0 ? 0 : (int)Math.Round(LevelSlider.Value);
        set
        {
            if (_levels.Count == 0) return;
            _updating = true;
            LevelSlider.Value = Math.Clamp(value, 0, _levels.Count - 1);
            _updating = false;
            UpdateSelectionText();
        }
    }

    public void SetLevels(IReadOnlyList<BlendLevelItem> levels)
    {
        _levels = levels;
        _updating = true;
        LevelSlider.Minimum = 0;
        LevelSlider.Maximum = Math.Max(0, levels.Count - 1);
        LevelSlider.IsEnabled = levels.Count(level => level.IsAvailable) > 1;
        LevelSlider.Value = levels.Count == 0 ? 0 : Math.Clamp(SelectedIndex, 0, levels.Count - 1);
        LabelsPanel.Children.Clear();
        LabelsPanel.Columns = Math.Max(1, levels.Count);
        foreach (var level in levels)
        {
            LabelsPanel.Children.Add(new TextBlock
            {
                Text = level.Name,
                FontSize = 11,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                FontWeight = FontWeights.Medium,
                HorizontalAlignment = HorizontalAlignment.Center,
                TextAlignment = TextAlignment.Center
            });
        }

        _updating = false;
        UpdateSelectionText();
    }

    public void SetCaption(string caption) => CaptionText.Text = caption;

    public void SetAccessibilityName(string name)
    {
        AutomationProperties.SetName(LevelSlider, name);
    }

    private void LevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        UpdateSelectionText();
        if (!_updating && _levels.Count > 0 && !_levels[SelectedIndex].IsAvailable)
        {
            var nearest = Enumerable.Range(0, _levels.Count)
                .Where(index => _levels[index].IsAvailable)
                .OrderBy(index => Math.Abs(index - SelectedIndex))
                .FirstOrDefault(-1);
            if (nearest >= 0)
            {
                _updating = true;
                LevelSlider.Value = nearest;
                _updating = false;
                UpdateSelectionText();
                SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(nearest, _levels[nearest]));
            }
            return;
        }
        if (!_updating && _levels.Count > 0)
        {
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(SelectedIndex, _levels[SelectedIndex]));
        }
    }

    private void LevelSlider_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_levels.Count == 0) return;
        var available = _levels
            .Select((level, index) => (level, index))
            .Where(item => item.level.IsAvailable)
            .Select(item => item.index)
            .ToArray();
        if (available.Length == 0) return;

        var current = SelectedIndex;
        var target = e.Key switch
        {
            System.Windows.Input.Key.Home => available[0],
            System.Windows.Input.Key.End => available[^1],
            System.Windows.Input.Key.Left or System.Windows.Input.Key.Down => available.LastOrDefault(index => index < current, current),
            System.Windows.Input.Key.Right or System.Windows.Input.Key.Up => available.FirstOrDefault(index => index > current, current),
            _ => current
        };
        if (target == current && e.Key is not (System.Windows.Input.Key.Home or System.Windows.Input.Key.End)) return;

        _updating = true;
        LevelSlider.Value = target;
        _updating = false;
        UpdateSelectionText();
        if (target != current)
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(target, _levels[target]));
        e.Handled = true;
    }

    private void UpdateSelectionText()
    {
        if (_levels.Count == 0)
        {
            SelectionText.Text = string.Empty;
            SelectionSummaryText.Text = string.Empty;
            return;
        }

        var item = _levels[SelectedIndex];
        SelectionText.Text = item.IsAvailable ? item.Name : string.Empty;
        SelectionSummaryText.Text = item.IsAvailable ? item.Summary : string.Empty;
        SelectionText.ToolTip = item.Summary;
        for (var index = 0; index < LabelsPanel.Children.Count; index++)
        {
            if (LabelsPanel.Children[index] is not TextBlock label) continue;
            var selected = index == SelectedIndex;
            var available = _levels[index].IsAvailable;
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
