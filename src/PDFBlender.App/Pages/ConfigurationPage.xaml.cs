using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using PDFBlender.Core.Models;
using UiToggleSwitch = Wpf.Ui.Controls.ToggleSwitch;
using UiButton = Wpf.Ui.Controls.Button;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;
using WpfTextBox = System.Windows.Controls.TextBox;

namespace PDFBlender.App.Pages;

public partial class ConfigurationPage : Page
{
    private readonly AppSession _session;
    private bool _refreshing;
    private const double CardWidth = 245;

    internal ConfigurationPage(AppSession session)
    {
        _session = session;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        _refreshing = true;
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("configuration");
        SubtitleText.Text = UiText.Get("configurationSubtitle");
        SlotsHeader.Text = UiText.Get("blendSlots");
        SlotsHint.Text = UiText.Get("blendSlotsHint");
        SlotsCountText.Text = $"{_session.SlotConfigurations.Count}/5";
        UserHeader.Text = UiText.Get("userConfigurations");
        AddButtonText.Text = UiText.Get("addConfiguration");
        UserEmptyText.Text = _session.UserConfigurations.Count == 0 ? UiText.Get("noUserConfigurations") : string.Empty;
        BuiltInHeader.Text = UiText.Get("builtInPresets");
        BuiltInHint.Text = UiText.Get("builtInPresetsHint");
        ErrorText.Text = string.Empty;
        BuildSlots();
        BuildConfigurationCards(UserCardsPanel, _session.UserConfigurations, true);
        BuildConfigurationCards(BuiltInCardsPanel, _session.BuiltInConfigurations, false);
        BuildEditor(_session.GetConfiguration(_session.SelectedConfigurationId ?? string.Empty));
        _refreshing = false;
    }

    private void BuildSlots()
    {
        SlotsPanel.Children.Clear();
        if (_session.SlotConfigurations.Count == 0)
        {
            SlotsPanel.Children.Add(new TextBlock
            {
                Text = UiText.Get("emptyBlendSlots"),
                Foreground = (Brush)FindResource("MutedTextBrush"),
                Margin = new Thickness(4, 8, 4, 8)
            });
            return;
        }

        foreach (var configuration in _session.SlotConfigurations)
        {
            var card = CreateCard(configuration, isSlot: true);
            SlotsPanel.Children.Add(card);
        }
    }

    private void BuildConfigurationCards(Panel panel, IEnumerable<BlendConfiguration> configurations, bool editable)
    {
        panel.Children.Clear();
        foreach (var configuration in configurations)
        {
            var card = CreateCard(configuration, isSlot: false, editable);
            panel.Children.Add(card);
        }
    }

    private Border CreateCard(BlendConfiguration configuration, bool isSlot, bool editable = false)
    {
        var border = new Border
        {
            Width = isSlot ? 220 : CardWidth,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(12),
            Background = (Brush)FindResource("CardBrush"),
            BorderBrush = IntensityBrush(configuration.Intensity),
            BorderThickness = new Thickness(1.5),
            CornerRadius = new CornerRadius(9),
            Cursor = Cursors.Hand,
            ToolTip = UiText.Get("dragToSlots")
        };
        var root = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var name = new TextBlock { Text = configuration.Name, FontSize = 14, FontWeight = FontWeights.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(name, 0);
        header.Children.Add(name);
        var intensity = new TextBlock { Text = $"{configuration.Intensity}/9", Foreground = IntensityBrush(configuration.Intensity), FontWeight = FontWeights.SemiBold };
        Grid.SetColumn(intensity, 1);
        header.Children.Add(intensity);
        root.Children.Add(header);
        root.Children.Add(new TextBlock
        {
            Text = configuration.IsBuiltIn ? UiText.Get("builtIn") : UiText.Get("userConfiguration"),
            Foreground = (Brush)FindResource("MutedTextBrush"),
            FontSize = 11,
            Margin = new Thickness(0, 2, 0, 7)
        });
        root.Children.Add(CreateIntensityBar(configuration.Intensity));
        root.Children.Add(new TextBlock
        {
            Text = FeatureSummary(configuration),
            Foreground = (Brush)FindResource("MutedTextBrush"),
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 7, 0, 0),
            MaxHeight = 34
        });

        if (isSlot)
        {
            var remove = new UiButton { Content = UiText.Get("remove"), HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(6, 2, 6, 2), Margin = new Thickness(0, 7, 0, 0), Appearance = ControlAppearance.Secondary };
            remove.Click += (_, _) => _session.RemoveFromSlots(configuration.Id);
            root.Children.Add(remove);
        }
        else if (editable)
        {
            var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var edit = MakeAction(UiText.Get("edit"));
            edit.Click += (_, _) => _session.SelectConfiguration(configuration.Id);
            var copy = MakeAction(UiText.Get("copy"));
            copy.Click += (_, _) => _session.DuplicateConfiguration(configuration.Id);
            var delete = MakeAction(UiText.Get("delete"));
            delete.Click += (_, _) =>
            {
                if (MessageBox.Show(UiText.Get("deleteConfigurationConfirm"), UiText.Get("configuration"), MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                    _session.DeleteConfiguration(configuration.Id);
            };
            actions.Children.Add(edit);
            actions.Children.Add(copy);
            actions.Children.Add(delete);
            root.Children.Add(actions);
        }

        border.Child = root;
        border.MouseLeftButtonDown += (_, _) => _session.SelectConfiguration(configuration.Id);
        border.PreviewMouseMove += (_, e) =>
        {
            if (e.LeftButton == MouseButtonState.Pressed && !isSlot)
                DragDrop.DoDragDrop(border, configuration.Id, DragDropEffects.Copy);
        };
        return border;
    }

    private void BuildEditor(BlendConfiguration? configuration)
    {
        EditorPanel.Children.Clear();
        EditorBorder.Visibility = configuration is null ? Visibility.Collapsed : Visibility.Visible;
        if (configuration is null) return;

        var title = new TextBlock { Text = $"{UiText.Get("editConfiguration")}: {configuration.Name}", Style = (Style)FindResource("CardTitleTextStyle") };
        EditorPanel.Children.Add(title);
        var nameRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 10, 0, 10) };
        var nameBox = new WpfTextBox { Text = configuration.Name, Width = 250, IsEnabled = !configuration.IsBuiltIn };
        var rename = MakeAction(UiText.Get("rename"));
        rename.Margin = new Thickness(8, 0, 0, 0);
        rename.IsEnabled = !configuration.IsBuiltIn;
        rename.Click += (_, _) => _session.RenameConfiguration(configuration.Id, nameBox.Text);
        nameRow.Children.Add(nameBox);
        nameRow.Children.Add(rename);
        EditorPanel.Children.Add(nameRow);

        var settings = configuration.Settings;
        AddFeatureEditor(configuration, BlendFeature.NormalizeContentWidth, UiText.Get("normalizeWidth"), UiText.Get("normalizeHint"), settings.NormalizeContentWidth,
            () => new UIElement[] { LabeledTextBox(UiText.Get("targetWidth"), settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture), out var widthBox) },
            controls => ApplySettings(configuration, settings with
            {
                NormalizeContentWidth = controls.toggle.IsChecked == true,
                TargetWidthMillimeters = ReadWidth(controls.detail)
            }));
        AddFeatureEditor(configuration, BlendFeature.Flatten, UiText.Get("flatten"), UiText.Get("protectionHint"), settings.Protection == ProtectionMethod.Flatten,
            () => new UIElement[] { LabeledCombo(UiText.Get("dpi"), new[] { "120", "150", "200", "300" }, Array.IndexOf(new[] { 120, 150, 200, 300 }, settings.Dpi)) },
            controls => ApplySettings(configuration, settings with
            {
                Protection = controls.toggle.IsChecked == true ? ProtectionMethod.Flatten : ProtectionMethod.None,
                Dpi = ReadCombo(controls.detail, settings.Dpi, new[] { 120, 150, 200, 300 })
            }));
        AddFeatureEditor(configuration, BlendFeature.Watermark, UiText.Get("watermark"), UiText.Get("watermarkTypeHint"), settings.Watermark != WatermarkStyle.None,
            () => new UIElement[]
            {
                LabeledCombo(UiText.Get("watermarkType"), new[] { UiText.Get("text"), UiText.Get("fullPage") }, Math.Max(0, (int)settings.Watermark - 1)),
                LabeledTextBox(UiText.Get("watermarkText"), settings.WatermarkText, out _)
            },
            controls => ApplySettings(configuration, settings with
            {
                Watermark = controls.toggle.IsChecked == true ? (WatermarkStyle)(ReadCombo(controls.detail, 1, new[] { 1, 2 })) : WatermarkStyle.None,
                WatermarkText = ReadText(controls.detail)
            }));
        AddFeatureEditor(configuration, BlendFeature.PixelNoise, UiText.Get("noise"), UiText.Get("noiseHint"), settings.Noise != NoiseStrength.Off,
            () => new UIElement[] { LabeledCombo(UiText.Get("noise"), new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") }, Math.Max(0, (int)settings.Noise - 1)) },
            controls => ApplySettings(configuration, settings with
            {
                Noise = controls.toggle.IsChecked == true ? (NoiseStrength)(ReadCombo(controls.detail, 1, new[] { 1, 2, 3 })) : NoiseStrength.Off
            }));
    }

    private void AddFeatureEditor(BlendConfiguration configuration, BlendFeature feature, string title, string hint, bool enabled, Func<UIElement[]> detailsFactory, Action<(UiToggleSwitch toggle, StackPanel detail)> changed)
    {
        var row = new Border { Background = (Brush)FindResource("SettingItemBrush"), CornerRadius = new CornerRadius(7), Padding = new Thickness(10), Margin = new Thickness(0, 0, 0, 8) };
        var root = new StackPanel();
        var header = new Grid();
        header.ColumnDefinitions.Add(new ColumnDefinition());
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var labels = new StackPanel();
        labels.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold });
        labels.Children.Add(new TextBlock { Text = hint, Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 11, TextWrapping = TextWrapping.Wrap });
        header.Children.Add(labels);
        var toggle = new UiToggleSwitch { IsChecked = enabled, IsEnabled = !configuration.IsBuiltIn, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(toggle, 1);
        header.Children.Add(toggle);
        root.Children.Add(header);
        var detail = new StackPanel { Margin = new Thickness(0, 8, 0, 0), Visibility = enabled ? Visibility.Visible : Visibility.Collapsed, IsEnabled = !configuration.IsBuiltIn };
        foreach (var control in detailsFactory()) detail.Children.Add(control);
        root.Children.Add(detail);
        toggle.Checked += (_, _) => { detail.Visibility = Visibility.Visible; changed((toggle, detail)); };
        toggle.Unchecked += (_, _) => { detail.Visibility = Visibility.Collapsed; changed((toggle, detail)); };
        foreach (var textBox in detail.Children.OfType<StackPanel>().SelectMany(item => item.Children.OfType<WpfTextBox>()))
            textBox.TextChanged += (_, _) => changed((toggle, detail));
        foreach (var combo in detail.Children.OfType<StackPanel>().SelectMany(item => item.Children.OfType<ComboBox>()))
            combo.SelectionChanged += (_, _) => changed((toggle, detail));
        row.Child = root;
        EditorPanel.Children.Add(row);
    }

    private void ApplySettings(BlendConfiguration configuration, BlendSettings settings)
    {
        if (_refreshing || configuration.IsBuiltIn) return;
        var errors = SettingsValidator.Validate(settings);
        ErrorText.Text = errors.Count == 0 ? string.Empty : string.Join(" ", errors);
        if (errors.Count == 0) _session.UpdateConfiguration(configuration.Id, settings);
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => _session.AddUserConfiguration();

    private void Slots_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) && _session.SlotIds.Count < 5 ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void Slots_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.StringFormat) is string id) _session.AddToSlots(id);
        e.Handled = true;
    }

    private UiButton MakeAction(string text) => new() { Content = text, Padding = new Thickness(7, 3, 7, 3), Margin = new Thickness(0, 0, 5, 0), Appearance = ControlAppearance.Secondary };

    private UIElement LabeledTextBox(string label, string value, out WpfTextBox box)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
        panel.Children.Add(new TextBlock { Text = label, Width = 150, VerticalAlignment = VerticalAlignment.Center });
        box = new WpfTextBox { Text = value, Width = 180, IsEnabled = true };
        panel.Children.Add(box);
        return panel;
    }

    private UIElement LabeledCombo(string label, IReadOnlyList<string> values, int selected)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
        panel.Children.Add(new TextBlock { Text = label, Width = 150, VerticalAlignment = VerticalAlignment.Center });
        var combo = new ComboBox { ItemsSource = values, SelectedIndex = Math.Clamp(selected, 0, values.Count - 1), Width = 180 };
        panel.Children.Add(combo);
        return panel;
    }

    private static double ReadWidth(Panel panel)
    {
        var text = panel.Children.OfType<StackPanel>().SelectMany(item => item.Children.OfType<WpfTextBox>()).FirstOrDefault()?.Text;
        return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 210;
    }

    private static int ReadCombo(Panel panel, int fallback, int[] values)
    {
        var index = panel.Children.OfType<StackPanel>().SelectMany(item => item.Children.OfType<ComboBox>()).FirstOrDefault()?.SelectedIndex ?? -1;
        return index >= 0 && index < values.Length ? values[index] : fallback;
    }

    private static string ReadText(Panel panel) =>
        panel.Children.OfType<StackPanel>().SelectMany(item => item.Children.OfType<WpfTextBox>()).LastOrDefault()?.Text ?? string.Empty;

    private Border CreateIntensityBar(int intensity)
    {
        var outer = new Border { Height = 5, Background = (Brush)FindResource("BorderBrush"), CornerRadius = new CornerRadius(3) };
        outer.Child = new Border { Width = Math.Max(4, 100 * (intensity + 1) / 10d), HorizontalAlignment = HorizontalAlignment.Left, Background = IntensityBrush(intensity), CornerRadius = new CornerRadius(3) };
        return outer;
    }

    private Brush IntensityBrush(int intensity)
    {
        var stops = new[] { Colors.ForestGreen, Colors.YellowGreen, Colors.Gold, Colors.Orange, Colors.OrangeRed, Colors.Firebrick };
        return new SolidColorBrush(stops[(int)Math.Round(Math.Clamp(intensity, 0, 9) / 9d * (stops.Length - 1))]);
    }

    private string FeatureSummary(BlendConfiguration configuration)
    {
        var names = configuration.EnabledFeatures.Select(feature => feature switch
        {
            BlendFeature.NormalizeContentWidth => UiText.Get("normalizeWidth"),
            BlendFeature.Flatten => UiText.Get("flatten"),
            BlendFeature.Watermark => UiText.Get("watermark"),
            BlendFeature.PixelNoise => UiText.Get("noise"),
            _ => feature.ToString()
        });
        return string.Join(" · ", names.DefaultIfEmpty(UiText.Get("noFeatures")));
    }
}
