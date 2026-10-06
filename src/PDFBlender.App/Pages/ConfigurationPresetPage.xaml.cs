using System.Windows;
using System.Windows.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Media;
using System.Windows.Threading;
using PDFBlender.Core.Models;
using UiButton = Wpf.Ui.Controls.Button;
using UiCard = Wpf.Ui.Controls.CardControl;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;
using Wpf.Ui.Controls;

namespace PDFBlender.App.Pages;

public partial class ConfigurationPresetPage : Page
{
    private readonly AppSession _session;
    private readonly Action<string> _add;
    private readonly Action _createBack;
    private readonly Action _listBack;

    internal ConfigurationPresetPage(AppSession session, Action<string> add, Action createBack, Action listBack)
    {
        _session = session;
        _add = add;
        _createBack = createBack;
        _listBack = listBack;
        InitializeComponent();
        BreadcrumbBarStyling.Attach(BreadcrumbBar);
        Refresh();
    }

    private void Refresh()
    {
        BreadcrumbBar.Items.Clear();
        BreadcrumbBar.Items.Add(UiText.Get("configuration"));
        BreadcrumbBar.Items.Add(UiText.Get("newPresetConfiguration"));
        TitleText.Text = UiText.Get("choosePreset");
        SubtitleText.Text = UiText.Get("presetConfigurationDescription");
        PresetPanel.Children.Clear();
        foreach (var preset in _session.BuiltInConfigurations)
        {
            var card = new UiCard { Padding = new Thickness(16, 12, 16, 12), Margin = new Thickness(0, 0, 0, 10), Height = 88, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch, Background = (Brush)FindResource("CardBrush"), BorderBrush = ConfigurationPalette.GetBrush(ConfigurationPalette.ForIndex(PresetPanel.Children.Count)), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8) };
            var grid = new Grid { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var presetIcon = ConfigurationIconCatalog.GetDescriptor(ConfigurationIconCatalog.ForIndex(PresetPanel.Children.Count));
            grid.Children.Add(new SymbolIcon { Symbol = presetIcon.Symbol, Filled = presetIcon.Filled, FontSize = 26, Foreground = ConfigurationPalette.GetBrush(ConfigurationPalette.ForIndex(PresetPanel.Children.Count)), VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Left });
            var text = new StackPanel { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(14, 0, 14, 0) };
            text.Children.Add(new TextBlock { Text = LocalizedPresetName(preset), FontSize = 15, FontWeight = FontWeights.SemiBold, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left });
            text.Children.Add(new TextBlock { Text = $"{FeatureSummary(preset)} · {UiText.Get(ConfigurationIntensity.LabelKey(preset.Intensity))}", FontSize = 11, Foreground = (Brush)FindResource("MutedTextBrush"), TextTrimming = TextTrimming.CharacterEllipsis, HorizontalAlignment = HorizontalAlignment.Left, TextAlignment = TextAlignment.Left });
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);
            var add = new UiButton { Content = UiText.Get("add"), Appearance = ControlAppearance.Primary, Foreground = (Brush)FindResource("AccentTextBrush"), Width = 76, Height = 34, Padding = new Thickness(8, 4, 8, 4), Cursor = System.Windows.Input.Cursors.Hand, Tag = preset.Id };
            add.Click += AddPreset_Click;
            Grid.SetColumn(add, 2);
            grid.Children.Add(add);
            card.Content = grid;
            PresetPanel.Children.Add(card);
        }
    }

    private void AddPreset_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string id }) _add(id);
    }

    private void BreadcrumbBar_ItemClicked(object sender, BreadcrumbBarItemClickedEventArgs e)
    {
        if (e.Index == 0)
            Dispatcher.BeginInvoke(new Action(_listBack), DispatcherPriority.ContextIdle);
    }

    private string LocalizedPresetName(BlendConfiguration preset) => preset.Id switch
    {
        "builtin-light" => $"{UiText.Get("presetPrefix")} {UiText.Get("light")}",
        "builtin-medium" => $"{UiText.Get("presetPrefix")} {UiText.Get("medium")}",
        "builtin-heavy" => $"{UiText.Get("presetPrefix")} {UiText.Get("heavy")}",
        _ => preset.Name
    };

    private string FeatureSummary(BlendConfiguration configuration) => string.Join(" · ", configuration.EnabledFeatures.Select(feature => feature switch
    {
        BlendFeature.NormalizeContentWidth => UiText.Get("normalizeWidth"),
        BlendFeature.Flatten => UiText.Get("flatten"),
        BlendFeature.Watermark => UiText.Get("watermark"),
        BlendFeature.PixelNoise => UiText.Get("noise"),
        _ => feature.ToString()
    }).DefaultIfEmpty(UiText.Get("noFeatures")));
}


