using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace PDFBlender.App;

internal static class ScrollBarAutoHideBehavior
{
    public static readonly DependencyProperty EnabledProperty = DependencyProperty.RegisterAttached(
        "Enabled", typeof(bool), typeof(ScrollBarAutoHideBehavior), new PropertyMetadata(false, OnEnabledChanged));

    private sealed class State
    {
        public ScrollBar? Bar;
        public DispatcherTimer Timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(700) };
    }

    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ScrollViewer viewer || e.NewValue is not true) return;
        var state = new State();
        viewer.Tag = state;
        viewer.Loaded += (_, _) =>
        {
            viewer.ApplyTemplate();
            state.Bar = FindVisualChild<ScrollBar>(viewer);
            if (state.Bar is null) return;
            state.Bar.Opacity = 0;
            state.Bar.MouseEnter += (_, _) => Show(viewer, false);
            state.Bar.MouseLeave += (_, _) => Schedule(viewer);
            state.Timer.Tick += (_, _) => Hide(viewer);
        };
        viewer.ScrollChanged += (_, _) => Show(viewer);
        viewer.PreviewMouseWheel += (_, _) => Show(viewer);
    }

    private static void Show(ScrollViewer viewer, bool schedule = true)
    {
        if (viewer.Tag is not State state || state.Bar is null) return;
        if (viewer.ScrollableHeight <= 0) { state.Bar.Opacity = 0; return; }
        state.Bar.BeginAnimation(UIElement.OpacityProperty, null);
        state.Bar.Opacity = 1;
        if (schedule) Schedule(viewer);
    }

    private static void Schedule(ScrollViewer viewer)
    {
        if (viewer.Tag is State state && state.Bar is not null && viewer.ScrollableHeight > 0)
        {
            state.Timer.Stop();
            state.Timer.Start();
        }
    }

    private static void Hide(ScrollViewer viewer)
    {
        if (viewer.Tag is not State state || state.Bar is null || state.Bar.IsMouseOver) return;
        state.Timer.Stop();
        state.Bar.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)) { FillBehavior = FillBehavior.HoldEnd });
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match) return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null) return nested;
        }
        return null;
    }
}
