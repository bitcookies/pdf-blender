using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace PDFBlender.App;

public sealed record BlendLevelItem(string Name, string Summary, bool IsAvailable = true, string? IconKey = null, string? ColorKey = null);

public partial class BlendLevelRail : UserControl
{
    private const int SlotCount = 5;
    private const double RailStartRatio = 0.1;
    private const double RailTravelRatio = 0.8;
    private IReadOnlyList<BlendLevelItem> _levels = [];
    private bool _updating;
    private bool _isDragging;
    private int _dragStartIndex;
    private int _dragTarget;
    private int _animationVersion;
    private static readonly Duration SelectionTransition = new(TimeSpan.FromMilliseconds(120));

    public event EventHandler<BlendLevelChangedEventArgs>? SelectionChanged;
    public event RoutedEventHandler? EditRequested;

    public IReadOnlyList<BlendLevelItem> Levels => _levels;

    public BlendLevelRail()
    {
        InitializeComponent();
        LevelSlider.PreviewKeyDown += LevelSlider_PreviewKeyDown;
        LevelSlider.PreviewMouseLeftButtonDown += LevelSlider_PreviewMouseLeftButtonDown;
        LevelSlider.MouseMove += LevelSlider_MouseMove;
        LevelSlider.PreviewMouseLeftButtonUp += LevelSlider_PreviewMouseLeftButtonUp;
        LevelSlider.LostMouseCapture += LevelSlider_LostMouseCapture;
        SetLevels([]);
    }

    public int SelectedIndex
    {
        get => _levels.Count == 0 ? 0 : Math.Clamp((int)Math.Round(LevelSlider.Value), 0, SlotCount - 1);
        set
        {
            if (_levels.Count == 0) return;
            var target = ResolveAvailableIndex(Math.Clamp(value, 0, SlotCount - 1), value >= SelectedIndex);
            MoveToIndex(target, animate: IsLoaded);
        }
    }

    public void SetLevels(IReadOnlyList<BlendLevelItem> levels)
    {
        var previousIndex = _levels.Count == 0 ? 0 : Math.Clamp((int)Math.Round(LevelSlider.Value), 0, SlotCount - 1);
        _levels = levels.Take(SlotCount).ToArray();
        _animationVersion++;
        _updating = true;
        LevelSlider.BeginAnimation(Slider.ValueProperty, null);
        LevelSlider.Minimum = 0;
        LevelSlider.Maximum = SlotCount - 1;
        LevelSlider.IsEnabled = _levels.Count(level => level.IsAvailable) > 1;
        var current = _levels.Count == 0 ? 0 : Math.Clamp(previousIndex, 0, SlotCount - 1);
        var selectedIndex = _levels.Count == 0 ? -1 : ResolveAvailableIndex(current, true);
        LevelSlider.Value = selectedIndex < 0 ? 0 : selectedIndex;

        NumbersPanel.Children.Clear();
        UpperTickPanel.Children.Clear();
        LowerTickPanel.Children.Clear();
        LabelsPanel.Children.Clear();
        for (var index = 0; index < SlotCount; index++)
        {
            var level = index < _levels.Count ? _levels[index] : new BlendLevelItem($"{index + 1}", string.Empty, false);
            var availableBrush = level.IsAvailable ? GetUnselectedAvailableBrush() : (Brush)FindResource("BorderBrush");
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
            var isSelected = level.IsAvailable && index == selectedIndex;
            var iconBrush = !level.IsAvailable
                ? (Brush)FindResource("BorderBrush")
                : isSelected
                    ? ConfigurationPalette.GetBrush(level.ColorKey)
                    : GetUnselectedAvailableBrush();
            var iconBorder = new Border
            {
                Width = 18,
                Height = 18,
                CornerRadius = new CornerRadius(9),
                BorderThickness = new Thickness(0),
                Background = iconBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                Opacity = level.IsAvailable ? 1 : 0
            };
            var levelIcon = new Wpf.Ui.Controls.SymbolIcon
            {
                Symbol = ConfigurationIconCatalog.GetSymbol(level.IconKey),
                FontSize = 12,
                Foreground = Brushes.White,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 18,
                Height = 18,
            };
            levelIcon.Foreground = new SolidColorBrush(Colors.White);
            iconBorder.Child = levelIcon;
            var labelPanel = new StackPanel
            {
                Width = 0,
                Height = 38,
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false
            };
            labelPanel.Children.Add(iconBorder);
            labelPanel.Children.Add(new TextBlock
            {
                Text = level.IsAvailable && !string.IsNullOrWhiteSpace(level.Name) ? level.Name : "—",
                FontSize = 11,
                Height = 16,
                Foreground = level.IsAvailable ? iconBrush : availableBrush,
                FontWeight = FontWeights.Medium,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 1, 0, 0)
            });
            LabelsPanel.Children.Add(labelPanel);
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

    private void LevelSlider_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (IsThumbSource(e.OriginalSource as DependencyObject))
        {
            if (LevelSlider.IsEnabled && _levels.Count > 0)
            {
                _animationVersion++;
                _updating = true;
                LevelSlider.BeginAnimation(Slider.ValueProperty, null);
                _updating = false;
                _isDragging = true;
                _dragStartIndex = SelectedIndex;
                _dragTarget = _dragStartIndex;
                LevelSlider.IsSnapToTickEnabled = false;
                UpdateSelectionText(_dragTarget);
                // The native Slider/Thumb drag continuously writes Value from
                // the pointer. Capture the rail ourselves so only discrete
                // animated transitions can move the thumb.
                LevelSlider.CaptureMouse();
            }
            e.Handled = true;
            return;
        }

        if (!LevelSlider.IsEnabled || _levels.Count == 0)
            return;

        var point = e.GetPosition(LevelSlider);
        // The custom template reserves 10% of the control on either side of
        // the track. Map the click into that same 80% travel range so clicks
        // and drag positions resolve to the same five stops.
        var ratio = Math.Clamp(point.X / Math.Max(1, LevelSlider.ActualWidth), 0, 1);
        ratio = Math.Clamp((ratio - RailStartRatio) / RailTravelRatio, 0, 1);
        var candidate = (int)Math.Round(LevelSlider.Minimum + ratio * (LevelSlider.Maximum - LevelSlider.Minimum));
        var target = ResolveAvailableIndex(candidate, candidate >= SelectedIndex);
        if (target == SelectedIndex)
        {
            e.Handled = true;
            return;
        }

        MoveToIndex(target, animate: true);
        SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(target, _levels[target]));
        e.Handled = true;
    }


    private void LevelSlider_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed || _levels.Count == 0)
            return;

        // Read the pointer position directly. Value remains on a discrete stop,
        // so it must never be used as the next pointer-position input.
        var point = e.GetPosition(LevelSlider);
        var ratio = Math.Clamp(point.X / Math.Max(1, LevelSlider.ActualWidth), 0, 1);
        ratio = Math.Clamp((ratio - RailStartRatio) / RailTravelRatio, 0, 1);
        var continuous = LevelSlider.Minimum + ratio * (LevelSlider.Maximum - LevelSlider.Minimum);
        var target = _dragTarget;
        const double hysteresis = 0.02;

        while (true)
        {
            var next = NextAvailable(target);
            if (next == target || continuous < (target + next) / 2d + hysteresis)
                break;
            target = next;
        }

        while (true)
        {
            var previous = PreviousAvailable(target);
            if (previous == target || continuous > (target + previous) / 2d - hysteresis)
                break;
            target = previous;
        }

        if (target != _dragTarget)
        {
            // The Thumb is allowed to move only through this discrete,
            // non-linear transition; it never follows the pointer directly.
            MoveToIndex(target, animate: true);
            _dragTarget = target;
            UpdateSelectionText(_dragTarget);
        }
    }

    private void LevelSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e) => EndThumbDrag();

    private void LevelSlider_LostMouseCapture(object sender, MouseEventArgs e) => EndThumbDrag();

    private void EndThumbDrag()
    {
        if (!_isDragging) return;

        var previous = _dragStartIndex;
        var target = _dragTarget;
        var from = Math.Clamp(LevelSlider.Value, LevelSlider.Minimum, LevelSlider.Maximum);
        _isDragging = false;
        if (LevelSlider.IsMouseCaptured)
            LevelSlider.ReleaseMouseCapture();
        MoveToIndex(target, animate: true, fromValue: from, restoreSnap: true);
        if (target != previous)
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(target, _levels[target]));
    }
    private static bool IsThumbSource(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is Thumb)
                return true;
            source = VisualTreeHelper.GetParent(source);
        }
        return false;
    }
    private void LevelSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_levels.Count == 0) return;
        if (_isDragging)
        {
            UpdateSelectionText(_dragTarget);
            return;
        }

        if (_updating)
            return;

        // Animated snapping may pass through empty slots; the animation owns Value.
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

        MoveToIndex(target, animate: IsLoaded);
        UpdateSelectionText();
        if (target != current)
            SelectionChanged?.Invoke(this, new BlendLevelChangedEventArgs(target, _levels[target]));
        e.Handled = true;
    }

    private void MoveToIndex(int target, bool animate, double? fromValue = null, bool restoreSnap = false)
    {
        target = Math.Clamp(target, 0, SlotCount - 1);
        var current = Math.Clamp(fromValue ?? LevelSlider.Value, LevelSlider.Minimum, LevelSlider.Maximum);
        var animationVersion = ++_animationVersion;
        _updating = true;
        LevelSlider.BeginAnimation(Slider.ValueProperty, null);

        if (!animate || Math.Abs(current - target) < 0.001)
        {
            _updating = true;
            LevelSlider.Value = target;
            _updating = false;
            LevelSlider.IsSnapToTickEnabled = true;
            UpdateSelectionText();
            return;
        }

        _updating = true;
        var transition = new DoubleAnimation
        {
            From = current,
            To = target,
            Duration = SelectionTransition,
            // Ease-out exponential motion gives the Thumb a quick acceleration and a soft settle.
            EasingFunction = new ExponentialEase { Exponent = 6, EasingMode = EasingMode.EaseOut }
        };
        transition.Completed += (_, _) =>
        {
            if (animationVersion != _animationVersion)
                return;
            LevelSlider.BeginAnimation(Slider.ValueProperty, null);
            LevelSlider.Value = target;
            _updating = false;
            if (restoreSnap)
                LevelSlider.IsSnapToTickEnabled = true;
            UpdateSelectionText();
        };
        LevelSlider.BeginAnimation(Slider.ValueProperty, transition, HandoffBehavior.SnapshotAndReplace);
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
        foreach (var label in LabelsPanel.Children.OfType<FrameworkElement>()) label.Width = labelWidth;
        PositionMarkers(LabelsPanel, railWidth, labelWidth, 4);
    }

    private static void PositionMarkers(Canvas panel, double railWidth, double markerWidth, double top = 0)
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
            Canvas.SetTop(marker, top);
        }
    }

    private void UpdateSelectionText(int? selectedIndexOverride = null)
    {
        if (_levels.Count == 0)
        {
            IntensityText.Text = string.Empty;
            return;
        }

        var index = selectedIndexOverride ?? SelectedIndex;
        var item = _levels.ElementAtOrDefault(index);
        IntensityText.Text = item?.IsAvailable == true ? item.Summary : string.Empty;
        IntensityText.ToolTip = IntensityText.Text;
        for (var labelIndex = 0; labelIndex < LabelsPanel.Children.Count; labelIndex++)
        {
            if (LabelsPanel.Children[labelIndex] is not StackPanel labelPanel) continue;
            var label = labelPanel.Children.OfType<TextBlock>().FirstOrDefault();
            var iconBorder = labelPanel.Children.OfType<Border>().FirstOrDefault();
            var icon = iconBorder?.Child as Wpf.Ui.Controls.SymbolIcon;
            var selected = labelIndex == index;
            var available = _levels.ElementAtOrDefault(labelIndex)?.IsAvailable == true;
            var highlighted = selected && available;
            var configurationBrush = available
                ? ConfigurationPalette.GetBrush(_levels.ElementAtOrDefault(labelIndex)?.ColorKey)
                : (Brush)FindResource("BorderBrush");
            var stateBrush = available
                ? highlighted
                    ? configurationBrush
                    : GetUnselectedAvailableBrush()
                : (Brush)FindResource("BorderBrush");
            if (label is not null)
            {
                label.Foreground = stateBrush;
                label.FontWeight = highlighted ? FontWeights.SemiBold : FontWeights.Medium;
            }
            if (iconBorder is not null)
            {
                iconBorder.Background = stateBrush;
                iconBorder.Opacity = available ? 1 : 0;
            }
            if (icon is not null)
                icon.Foreground = Brushes.White;
        }
        UpdateTickBrushes(index);
    }

    private Brush GetUnselectedAvailableBrush() => ThemeManager.IsDark
        ? new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66))
        : new SolidColorBrush(Color.FromRgb(0xB3, 0xB3, 0xB3));

    private void UpdateTickBrushes(int selectedIndex)
    {
        for (var index = 0; index < SlotCount; index++)
        {
            var level = _levels.ElementAtOrDefault(index);
            var available = level?.IsAvailable == true;
            var brush = available
                ? index == selectedIndex
                    ? ConfigurationPalette.GetBrush(level?.ColorKey)
                    : GetUnselectedAvailableBrush()
                : (Brush)FindResource("BorderBrush");
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
