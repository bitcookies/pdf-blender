using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Wpf.Ui;
using Wpf.Ui.Controls;
using UiTextBox = Wpf.Ui.Controls.TextBox;

namespace PDFBlender.App;

internal sealed class DialogService
{
    private readonly ContentDialogService _service = new();

    public DialogService(ContentDialogHost host) => _service.SetDialogHost(host);

    public async Task<bool> ShowConfirmationAsync(string title, string message, string primaryText, string closeText, SymbolRegular icon)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            PrimaryButtonAppearance = ControlAppearance.Danger,
            CloseButtonAppearance = ControlAppearance.Secondary,
            PrimaryButtonIcon = new SymbolIcon(icon),
            DialogWidth = 330,
            DialogMaxWidth = 620,
            DefaultButton = ContentDialogButton.Primary
        };
        dialog.Loaded += (_, _) =>
        {
            dialog.Dispatcher.BeginInvoke(
                System.Windows.Threading.DispatcherPriority.Loaded,
                new Action(() =>
                {
                    var primary = FindVisualChild<Wpf.Ui.Controls.Button>(dialog);
                    if (primary is not null)
                    {
                        var textBrush = (Brush)Application.Current.FindResource("AccentTextBrush");
                        primary.Foreground = textBrush;
                        primary.Cursor = Cursors.Hand;
                        SetForegroundRecursive(primary, textBrush);
                    }
                }));
        };
        var result = await _service.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary;
    }

    public Task<string?> ShowIconPickerAsync(string? currentKey, FrameworkElement placementTarget)
    {
        var resultSource = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var query = new UiTextBox
        {
            PlaceholderText = UiText.Get("searchIcons"),
            Margin = new Thickness(12, 12, 12, 8),
            Height = 36
        };
        var grid = new WrapPanel { Orientation = Orientation.Horizontal };
        var scroll = new ScrollViewer
        {
            Width = 300,
            Height = 165,
            Padding = new Thickness(8, 0, 8, 8),
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = grid
        };
        var border = new Border
        {
            Background = Brushes.Transparent,
            Padding = new Thickness(6),
            Child = new Border
            {
                Width = 328,
                Background = (Brush)Application.Current.FindResource("SurfaceBrush"),
                BorderBrush = (Brush)Application.Current.FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                SnapsToDevicePixels = true,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 16,
                    ShadowDepth = 4,
                    Direction = 270,
                    Opacity = 0.24,
                    Color = Color.FromRgb(32, 32, 32)
                },
                Child = new StackPanel { Children = { query, scroll } }
            }
        };
        var popup = new Popup
        {
            PlacementTarget = placementTarget,
            Placement = PlacementMode.Bottom,
            StaysOpen = false,
            AllowsTransparency = true,
            Child = border
        };

        var completed = false;
        void Close(string? value)
        {
            if (completed) return;
            completed = true;
            resultSource.TrySetResult(value);
            if (popup.IsOpen) popup.IsOpen = false;
        }

        void Refresh()
        {
            grid.Children.Clear();
            var matches = ConfigurationIconCatalog.Search(query.Text, false)
                .Concat(ConfigurationIconCatalog.Search(query.Text, true))
                .GroupBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .Take(240);
            foreach (var descriptor in matches)
            {
                var selected = string.Equals(descriptor.Key, currentKey, StringComparison.OrdinalIgnoreCase);
                var button = new Wpf.Ui.Controls.Button
                {
                    Width = 28,
                    Height = 28,
                    Margin = new Thickness(1),
                    Padding = new Thickness(0),
                    CornerRadius = new CornerRadius(6),
                    Appearance = ControlAppearance.Secondary,
                    BorderBrush = selected ? (Brush)Application.Current.FindResource("AccentBrush") : (Brush)Application.Current.FindResource("BorderBrush"),
                    BorderThickness = new Thickness(selected ? 2 : 1),
                    ToolTip = descriptor.Name,
                    Cursor = Cursors.Hand,
                    Content = new SymbolIcon { Symbol = descriptor.Symbol, Filled = descriptor.Filled, FontSize = 13 }
                };
                button.Click += (_, _) => Close(descriptor.Key);
                grid.Children.Add(button);
            }
        }

        query.TextChanged += (_, _) => Refresh();
        popup.Closed += (_, _) => { if (!completed) { completed = true; resultSource.TrySetResult(null); } };
        query.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; Close(null); } };
        Refresh();
        popup.IsOpen = true;
        query.Focus();
        return resultSource.Task;
    }

    private static void SetForegroundRecursive(DependencyObject parent, Brush brush)
    {
        if (parent is Control control)
        {
            control.Foreground = brush;
        }

        if (parent is TextBlock textBlock)
        {
            textBlock.Foreground = brush;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            SetForegroundRecursive(VisualTreeHelper.GetChild(parent, i), brush);
        }
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
