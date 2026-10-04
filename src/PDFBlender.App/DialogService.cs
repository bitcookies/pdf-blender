using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PDFBlender.Core.Models;
using TextBlock = System.Windows.Controls.TextBlock;
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

    public async Task<ConfigurationCreationMode?> ShowConfigurationCreationAsync()
    {
        ConfigurationCreationMode? selection = null;
        var dialog = new ContentDialog
        {
            Title = UiText.Get("addConfiguration"),
            DialogWidth = 560,
            DialogMaxWidth = 620,
            DialogHeight = 390,
            DialogMaxHeight = 460,
            IsFooterVisible = false
        };
        var content = new StackPanel { Margin = new Thickness(4, 8, 4, 0) };
        content.Children.Add(new TextBlock
        {
            Text = UiText.Get("newConfigurationHint"),
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.FindResource("MutedTextBrush"),
            FontSize = 13,
            Margin = new Thickness(0, 0, 0, 14)
        });
        var choices = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch };
        choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        choices.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var customButton = CreateCreationChoice("BookAdd24", UiText.Get("newCustomConfiguration"), UiText.Get("customConfigurationDescription"));
        customButton.Click += (_, _) =>
        {
            selection = ConfigurationCreationMode.Custom;
            dialog.Hide(ContentDialogResult.Primary);
        };
        Grid.SetColumn(customButton, 0);
        choices.Children.Add(customButton);

        var presetButton = CreateCreationChoice("BoxMultiple20", UiText.Get("newPresetConfiguration"), UiText.Get("presetConfigurationDescription"));
        presetButton.Click += (_, _) =>
        {
            selection = ConfigurationCreationMode.Preset;
            dialog.Hide(ContentDialogResult.Primary);
        };
        Grid.SetColumn(presetButton, 1);
        choices.Children.Add(presetButton);

        content.Children.Add(choices);
        dialog.Content = content;
        var result = await _service.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary ? selection : null;
    }

    private static Wpf.Ui.Controls.Button CreateCreationChoice(string symbol, string title, string description)
    {
        var button = new Wpf.Ui.Controls.Button
        {
            Appearance = ControlAppearance.Secondary,
            Margin = new Thickness(5),
            Padding = new Thickness(12),
            MinHeight = 210,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            ToolTip = title
        };
        System.Windows.Automation.AutomationProperties.SetName(button, title);
        button.Content = new StackPanel
        {
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Children =
            {
                new SymbolIcon { Symbol = Enum.Parse<SymbolRegular>(symbol), FontSize = 56, Foreground = (Brush)Application.Current.FindResource("AccentBrush"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 4, 0, 14) },
                new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.FindResource("TextBrush") },
                new TextBlock { Text = description, FontSize = 11, TextAlignment = TextAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = (Brush)Application.Current.FindResource("MutedTextBrush"), Margin = new Thickness(4, 8, 4, 0) }
            }
        };
        return button;
    }
    public async Task<string?> ShowPresetPickerAsync(IReadOnlyList<BlendConfiguration> presets)
    {
        string? selected = presets.FirstOrDefault()?.Id;
        var content = new StackPanel { Margin = new Thickness(0, 6, 0, 0) };
        foreach (var item in presets)
        {
            var radio = new RadioButton { Tag = item.Id, GroupName = "Preset", Margin = new Thickness(0, 0, 0, 8), Padding = new Thickness(10), IsChecked = selected == item.Id };
            radio.Content = new StackPanel
            {
                Children =
                {
                    new TextBlock { Text = LocalizedPresetName(item), FontWeight = FontWeights.SemiBold },
                    new TextBlock { Text = string.Format("{0} {1} · {2}", UiText.Get("intensity"), item.Intensity, FeatureSummary(item)), Foreground = Brushes.Gray, FontSize = 12, TextWrapping = TextWrapping.Wrap }
                }
            };
            radio.Checked += (_, _) => selected = Convert.ToString(radio.Tag);
            content.Children.Add(radio);
        }
        var dialog = new ContentDialog
        {
            Title = UiText.Get("choosePreset"), Content = content,
            PrimaryButtonText = UiText.Get("add"), CloseButtonText = UiText.Get("cancel"),
            PrimaryButtonAppearance = ControlAppearance.Primary, CloseButtonAppearance = ControlAppearance.Secondary,
            DialogWidth = 500, DialogMaxWidth = 560, DefaultButton = ContentDialogButton.Primary
        };
        var result = await _service.ShowAsync(dialog, CancellationToken.None);
        return result == ContentDialogResult.Primary ? selected : null;
    }

    private static string LocalizedPresetName(BlendConfiguration preset) => preset.Id switch
    {
        "builtin-light" => string.Format("{0} {1}", UiText.Get("presetPrefix"), UiText.Get("light")),
        "builtin-medium" => string.Format("{0} {1}", UiText.Get("presetPrefix"), UiText.Get("medium")),
        "builtin-heavy" => string.Format("{0} {1}", UiText.Get("presetPrefix"), UiText.Get("heavy")),
        _ => preset.Name
    };

    private static string FeatureSummary(BlendConfiguration configuration) => string.Join(" · ", configuration.EnabledFeatures.Select(feature => feature switch
    {
        BlendFeature.NormalizeContentWidth => UiText.Get("normalizeWidth"),
        BlendFeature.Flatten => UiText.Get("flatten"),
        BlendFeature.Watermark => UiText.Get("watermark"),
        BlendFeature.PixelNoise => UiText.Get("noise"),
        _ => feature.ToString()
    }).DefaultIfEmpty(UiText.Get("noFeatures")));
}

internal enum ConfigurationCreationMode
{
    Custom,
    Preset
}
