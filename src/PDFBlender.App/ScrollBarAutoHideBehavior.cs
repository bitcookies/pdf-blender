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
        public required ScrollViewer Viewer;
        public ScrollBar? Bar;
        public readonly DispatcherTimer Timer = new(DispatcherPriority.Background);
        public EventHandler? TimerHandler;
    }

    public static void SetEnabled(DependencyObject element, bool value) => element.SetValue(EnabledProperty, value);
    public static bool GetEnabled(DependencyObject element) => (bool)element.GetValue(EnabledProperty);

    private static void OnEnabledChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
    {
        if (dependencyObject is not ScrollViewer viewer)
            return;

        if (e.NewValue is true)
        {
            if (viewer.Tag is State)
                return;

            var state = new State { Viewer = viewer };
            state.Timer.Interval = UiMotionPolicy.ScrollBarHideDelay;
            state.TimerHandler = (_, _) => Hide(state.Viewer);
            state.Timer.Tick += state.TimerHandler;
            viewer.Tag = state;
            viewer.Loaded += Viewer_Loaded;
            viewer.Unloaded += Viewer_Unloaded;
            viewer.ScrollChanged += Viewer_ScrollChanged;
            viewer.PreviewMouseWheel += Viewer_PreviewMouseWheel;
            if (viewer.IsLoaded)
                AttachBar(viewer);
        }
        else if (viewer.Tag is State state)
        {
            Detach(viewer, state);
            viewer.Tag = null;
        }
    }

    private static void Viewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer viewer)
            AttachBar(viewer);
    }

    private static void Viewer_Unloaded(object sender, RoutedEventArgs e)
    {
        if (sender is ScrollViewer viewer && viewer.Tag is State state)
        {
            state.Timer.Stop();
            DetachBar(state);
        }
    }

    private static void Viewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (sender is ScrollViewer viewer)
            Show(viewer);
    }

    private static void Viewer_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer viewer)
            Show(viewer);
    }

    private static void AttachBar(ScrollViewer viewer)
    {
        if (viewer.Tag is not State state)
            return;

        viewer.ApplyTemplate();
        if (state.Bar is not null)
            DetachBar(state);

        state.Bar = FindVisualChild<ScrollBar>(viewer);
        if (state.Bar is null)
            return;

        state.Bar.MouseEnter += Bar_MouseEnter;
        state.Bar.MouseLeave += Bar_MouseLeave;
        state.Bar.Opacity = 0;
        if (viewer.ScrollableHeight > 0)
            Schedule(viewer);
    }

    private static void DetachBar(State state)
    {
        if (state.Bar is null)
            return;

        state.Bar.BeginAnimation(UIElement.OpacityProperty, null);
        state.Bar.MouseEnter -= Bar_MouseEnter;
        state.Bar.MouseLeave -= Bar_MouseLeave;
        state.Bar.Opacity = 0;
        state.Bar = null;
    }

    private static void Detach(ScrollViewer viewer, State state)
    {
        viewer.Loaded -= Viewer_Loaded;
        viewer.Unloaded -= Viewer_Unloaded;
        viewer.ScrollChanged -= Viewer_ScrollChanged;
        viewer.PreviewMouseWheel -= Viewer_PreviewMouseWheel;
        state.Timer.Stop();
        if (state.TimerHandler is not null)
            state.Timer.Tick -= state.TimerHandler;
        state.TimerHandler = null;
        DetachBar(state);
    }

    private static void Bar_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is ScrollBar bar && FindScrollViewer(bar) is { } viewer)
            Show(viewer, false);
    }

    private static void Bar_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is ScrollBar bar && FindScrollViewer(bar) is { } viewer)
            Schedule(viewer);
    }

    private static void Show(ScrollViewer? viewer, bool schedule = true)
    {
        if (viewer is null || viewer.Tag is not State state || state.Bar is null)
            return;

        if (viewer.ScrollableHeight <= 0)
        {
            state.Bar.Opacity = 0;
            return;
        }

        state.Bar.BeginAnimation(UIElement.OpacityProperty, null);
        state.Bar.Opacity = 1;
        if (schedule)
            Schedule(viewer);
    }

    private static void Schedule(ScrollViewer viewer)
    {
        if (viewer.Tag is State { Bar: not null } state && viewer.ScrollableHeight > 0)
        {
            state.Timer.Stop();
            state.Timer.Start();
        }
    }

    private static void Hide(ScrollViewer? viewer)
    {
        if (viewer is null || viewer.Tag is not State state || state.Bar is null || state.Bar.IsMouseOver)
            return;

        state.Timer.Stop();
        var fade = new DoubleAnimation(0, UiMotionPolicy.ScrollBarFadeDuration)
        {
            FillBehavior = FillBehavior.HoldEnd
        };
        state.Bar.BeginAnimation(UIElement.OpacityProperty, fade);
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject child)
    {
        var current = child;
        while (current is not null)
        {
            if (current is ScrollViewer viewer)
                return viewer;
            current = VisualTreeHelper.GetParent(current);
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
                return match;
            var nested = FindVisualChild<T>(child);
            if (nested is not null)
                return nested;
        }

        return null;
    }
}
