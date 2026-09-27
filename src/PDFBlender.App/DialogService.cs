using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

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
            Content = new System.Windows.Controls.TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
            PrimaryButtonText = primaryText,
            CloseButtonText = closeText,
            PrimaryButtonAppearance = ControlAppearance.Primary,
            CloseButtonAppearance = ControlAppearance.Secondary,
            PrimaryButtonIcon = new SymbolIcon(icon),
            DialogWidth = 460,
            DialogMaxWidth = 560,
            DefaultButton = ContentDialogButton.Primary
        };
        var result = await _service.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary;
    }
}
