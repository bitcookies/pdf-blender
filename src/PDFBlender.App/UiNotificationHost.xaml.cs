using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PDFBlender.App;

public enum UiNotificationKind { Info, Success, Warning, Error, Progress }

public sealed class UiNotificationService
{
    private readonly UiNotificationHost _host;

    public UiNotificationService(UiNotificationHost host) => _host = host;

    public Guid Show(UiNotificationKind kind, string title, string message, bool autoDismiss = true)
    {
        var id = Guid.NewGuid();
        _host.Show(new UiNotification(id, kind, title, message, autoDismiss));
        return id;
    }

    public void UpdateProgress(Guid id, string message, double fraction) => _host.Update(id, message, fraction);
    public void Dismiss(Guid id) => _host.Dismiss(id);
}

public sealed record UiNotification(Guid Id, UiNotificationKind Kind, string Title, string Message, bool AutoDismiss);

public partial class UiNotificationHost : UserControl
{
    private readonly Dictionary<Guid, Border> _items = [];

    public UiNotificationHost() => InitializeComponent();

    public void Show(UiNotification notification)
    {
        if (!Dispatcher.CheckAccess())
        {
            Dispatcher.Invoke(() => Show(notification));
            return;
        }

        var accent = notification.Kind switch
        {
            UiNotificationKind.Success => "#2E9D68",
            UiNotificationKind.Warning => "#D28A27",
            UiNotificationKind.Error => "#D84F58",
            UiNotificationKind.Progress => "#D84F58",
            _ => "#5286C6"
        };
        var panel = new StackPanel();
        panel.Children.Add(new TextBlock { Text = notification.Title, FontWeight = FontWeights.SemiBold, FontSize = 13 });
        panel.Children.Add(new TextBlock { Text = notification.Message, TextWrapping = TextWrapping.Wrap, FontSize = 12, Foreground = (Brush)FindResource("MutedTextBrush"), Margin = new Thickness(0, 4, 22, 0) });
        if (notification.Kind == UiNotificationKind.Progress)
            panel.Children.Add(new ProgressBar { Height = 4, Minimum = 0, Maximum = 1, Value = 0, Margin = new Thickness(0, 8, 0, 0), Foreground = (Brush)FindResource("AccentBrush") });

        var close = new Button { Content = "×", Width = 22, Height = 22, Background = Brushes.Transparent, BorderThickness = new Thickness(0), Foreground = (Brush)FindResource("MutedTextBrush"), HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(0, -4, -2, 0) };
        close.Click += (_, _) => Dismiss(notification.Id);
        var layout = new Grid();
        layout.Children.Add(panel);
        layout.Children.Add(close);
        var card = new Border { Child = layout, Background = (Brush)FindResource("SurfaceBrush"), BorderBrush = (Brush)FindResource("BorderBrush"), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8), Opacity = 0 };
        card.Tag = notification;
        _items[notification.Id] = card;
        ItemsPanel.Children.Insert(0, card);
        card.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160)));
        while (_items.Count > 3)
        {
            var oldest = ItemsPanel.Children.OfType<Border>().LastOrDefault();
            if (oldest?.Tag is UiNotification old) Dismiss(old.Id); else break;
        }
        if (notification.AutoDismiss && notification.Kind is not UiNotificationKind.Error and not UiNotificationKind.Progress)
            _ = AutoDismissAsync(notification.Id);
    }

    public void Update(Guid id, string message, double fraction)
    {
        if (!_items.TryGetValue(id, out var card) || card.Child is not Grid grid || grid.Children[0] is not StackPanel panel) return;
        if (panel.Children.OfType<TextBlock>().Skip(1).FirstOrDefault() is { } text) text.Text = message;
        if (panel.Children.OfType<ProgressBar>().FirstOrDefault() is { } progress) progress.Value = Math.Clamp(fraction, 0, 1);
    }

    public void Dismiss(Guid id)
    {
        if (!_items.Remove(id, out var card)) return;
        var animation = new DoubleAnimation(card.Opacity, 0, TimeSpan.FromMilliseconds(140));
        animation.Completed += (_, _) => ItemsPanel.Children.Remove(card);
        card.BeginAnimation(OpacityProperty, animation);
    }

    private async Task AutoDismissAsync(Guid id)
    {
        await Task.Delay(TimeSpan.FromSeconds(4));
        while (IsMouseOver)
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        Dispatcher.Invoke(() => Dismiss(id));
    }
}
