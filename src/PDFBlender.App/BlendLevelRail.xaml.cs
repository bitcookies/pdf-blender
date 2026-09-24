using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;

namespace PDFBlender.App;

public sealed record BlendLevelItem(string Name, string Summary);

public partial class BlendLevelRail : UserControl
{
    private IReadOnlyList<BlendLevelItem> _levels = [];
    private bool _updating;

    public event EventHandler<BlendLevelChangedEventArgs>? SelectionChanged;

    public IReadOnlyList<BlendLevelItem> Levels => _levels;

    public BlendLevelRail()
    {
        InitializeComponent();
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
        LevelSlider.IsEnabled = levels.Count > 1;
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
        if (!_updating && _levels.Count > 0)
        {
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(SelectedIndex, _levels[SelectedIndex]));
        }
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
        SelectionText.Text = item.Name;
        SelectionSummaryText.Text = item.Summary;
        SelectionText.ToolTip = item.Summary;
    }
}

public sealed class BlendLevelChangedEventArgs(int selectedIndex, BlendLevelItem item) : EventArgs
{
    public int SelectedIndex { get; } = selectedIndex;
    public BlendLevelItem Item { get; } = item;
}
