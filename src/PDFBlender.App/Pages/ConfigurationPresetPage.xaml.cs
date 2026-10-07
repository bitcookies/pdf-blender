using System.Windows;
using System.Windows.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Media;
using System.Windows.Threading;
using PDFBlender.Core.Models;
using UiButton = Wpf.Ui.Controls.Button;
using UiCard = Wpf.Ui.Controls.Card;
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

        for (var index = 0; index < _session.BuiltInConfigurations.Count; index++)
        {
            var preset = _session.BuiltInConfigurations[index];
            var color = ConfigurationPalette.GetBrush(ConfigurationPalette.ForPresetIndex(index));
            var card = new UiCard
            {
                Padding = new Thickness(12),
                Margin = new Thickness(4, 0, 4, 10),
                Height = 112,
                MinHeight = 112,
                MaxHeight = 112,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                Background = (Brush)FindResource("CardBrush"),
                BorderBrush = (Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1),
            };

            var grid = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                ClipToBounds = true
            };
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(80) });

            var presetIcon = ConfigurationIconCatalog.GetDescriptor(ConfigurationIconCatalog.ForPresetIndex(index));
            var iconBackground = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(21),
                Background = color,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Child = new SymbolIcon
                {
                    Symbol = presetIcon.Symbol,
                    Filled = presetIcon.Filled,
                    FontSize = 22,
                    Foreground = Brushes.White,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetRow(iconBackground, 0);
            Grid.SetColumn(iconBackground, 0);
            grid.Children.Add(iconBackground);

            var info = new Grid
            {
                Margin = new Thickness(10, 1, 8, 1),
                Width = 160,
                MinWidth = 160,
                MaxWidth = 160,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            info.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            info.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var name = new TextBlock
            {
                Text = LocalizedPresetName(preset),
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Foreground = (Brush)FindResource("TextBrush"),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(name, 0);
            info.Children.Add(name);

            var description = new TextBlock
            {
                Text = PresetDescription(preset),
                FontSize = 10.5,
                Foreground = (Brush)FindResource("MutedTextBrush"),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.WordEllipsis,
                MaxHeight = 36,
                Margin = new Thickness(0, 3, 0, 0),
                VerticalAlignment = VerticalAlignment.Top
            };
            Grid.SetRow(description, 1);
            info.Children.Add(description);

            Grid.SetRow(info, 0);
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);
            var intensity = new Border
            {
                Width = 64,
                MaxWidth = 64,
                Padding = new Thickness(3, 2, 3, 2),
                Margin = new Thickness(0, 0, 4, 0),
                CornerRadius = new CornerRadius(8),
                BorderBrush = color,
                BorderThickness = new Thickness(1),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = UiText.Get(ConfigurationIntensity.LabelKey(preset.Intensity)),
                    FontSize = 10,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    Foreground = color,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    TextAlignment = TextAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            Grid.SetRow(intensity, 0);
            Grid.SetColumn(intensity, 2);
            grid.Children.Add(intensity);
            var add = new UiButton
            {
                Content = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Children =
                    {
                        new SymbolIcon
                        {
                            Symbol = SymbolRegular.Add16,
                            FontSize = 14,
                            Margin = new Thickness(0, 0, 5, 0)
                        },
                        new TextBlock
                        {
                            Text = UiText.Get("add"),
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                },
                Appearance = ControlAppearance.Secondary,
                Height = 28,
                MinHeight = 28,
                Padding = new Thickness(8, 2, 8, 2),
                Margin = new Thickness(0, 8, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Tag = preset.Id,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                ToolTip = UiText.Get("add")
            };
            add.Click += AddPreset_Click;
            Grid.SetRow(add, 1);
            Grid.SetColumn(add, 0);
            Grid.SetColumnSpan(add, 3);
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
        "format-only" => UiText.Get("presetFormatOnly"),
        "light-mark" => UiText.Get("presetLightMark"),
        "reading-first" => UiText.Get("presetReadingFirst"),
        "balanced-protection" => UiText.Get("presetBalancedProtection"),
        "copy-layer-shield" => UiText.Get("presetCopyLayerShield"),
        "visual-obstruction" => UiText.Get("presetVisualObstruction"),
        "ai-processing-resistance" => UiText.Get("presetAiResistance"),
        "custom" => UiText.Get("customConfiguration"),
        _ => preset.Name
    };
    private string PresetDescription(BlendConfiguration preset) => preset.Id switch
    {
        "format-only" => UiText.Get("presetFormatOnlyDescription"),
        "light-mark" => UiText.Get("presetLightMarkDescription"),
        "reading-first" => UiText.Get("presetReadingFirstDescription"),
        "balanced-protection" => UiText.Get("presetBalancedProtectionDescription"),
        "copy-layer-shield" => UiText.Get("presetCopyLayerShieldDescription"),
        _ => UiText.Get("presetFormatOnlyDescription")
    };
}
