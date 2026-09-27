using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace PDFBlender.App;

public enum UiNotificationKind { Info, Success, Warning, Error, Progress }

public sealed class UiNotificationService
{
    private readonly SnackbarPresenter _presenter;
    private readonly Dictionary<Guid, Snackbar> _items = [];

    public UiNotificationService(SnackbarPresenter presenter) => _presenter = presenter;

    public Guid Show(UiNotificationKind kind, string title, string message, bool autoDismiss = true)
    {
        var id = Guid.NewGuid();
        var snackbar = new Snackbar(_presenter)
        {
            Title = title,
            Content = message,
            Appearance = kind switch
            {
                UiNotificationKind.Success => ControlAppearance.Success,
                UiNotificationKind.Warning => ControlAppearance.Caution,
                UiNotificationKind.Error => ControlAppearance.Danger,
                UiNotificationKind.Info => ControlAppearance.Info,
                _ => ControlAppearance.Primary
            },
            IsCloseButtonEnabled = true,
            Timeout = autoDismiss ? TimeSpan.FromSeconds(4) : TimeSpan.Zero
        };
        _items[id] = snackbar;
        snackbar.Show();
        return id;
    }

    public void UpdateProgress(Guid id, string message, double fraction)
    {
        if (!_items.TryGetValue(id, out var snackbar)) return;
        snackbar.Content = new StackPanel
        {
            Children =
            {
                new System.Windows.Controls.TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
                new ProgressBar { Minimum = 0, Maximum = 1, Value = Math.Clamp(fraction, 0, 1), Height = 4, Margin = new Thickness(0, 8, 0, 0) }
            }
        };
    }

    public void Dismiss(Guid id)
    {
        if (!_items.Remove(id, out var snackbar)) return;
        snackbar.IsShown = false;
    }
}
