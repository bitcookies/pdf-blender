using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using PDFBlender.Core.Models;
using UiCard = Wpf.Ui.Controls.Card;
using UiButton = Wpf.Ui.Controls.Button;
using UiTextBox = Wpf.Ui.Controls.TextBox;
using UiToggleSwitch = Wpf.Ui.Controls.ToggleSwitch;
using Wpf.Ui.Controls;

namespace PDFBlender.App.Pages;

public partial class ConfigurationEditorPage : Page
{
    private readonly AppSession _session;
    private readonly BlendConfiguration _source;
    private readonly bool _isNew;
    private readonly Action _parentBack;
    private readonly Action _listBack;
    private readonly Action<BlendConfiguration, bool, string, BlendSettings, string, string> _save;
    private readonly UiToggleSwitch _normalize = new();
    private readonly UiToggleSwitch _flatten = new();
    private readonly UiToggleSwitch _watermark = new();
    private readonly UiToggleSwitch _noise = new();
    private readonly UiTextBox _width = new();
    private readonly ComboBox _dpi = new();
    private readonly ComboBox _watermarkType = new();
    private readonly UiTextBox _watermarkText = new();
    private readonly ComboBox _noiseLevel = new();
    private string _selectedColorKey;
    private string _selectedIconKey;
    private BlendSettings _settings;

    internal ConfigurationEditorPage(AppSession session, BlendConfiguration? source, bool isNew, Action back, Action<BlendConfiguration, bool, string, BlendSettings, string, string> save, Action listBack)
    {
        _session = session;
        _source = source ?? new BlendConfiguration("new", UiText.Get("configurationName"), false, new BlendSettings { NormalizeContentWidth = true }, false, null, ConfigurationPalette.ForIndex(session.AllConfigurations.Count), ConfigurationIconCatalog.ForIndex(session.AllConfigurations.Count));
        _isNew = isNew;
        _parentBack = back;
        _listBack = listBack;
        _save = save;
        _selectedColorKey = ConfigurationPalette.Normalize(_source.ColorKey);
        _selectedIconKey = ConfigurationIconCatalog.Normalize(_source.IconKey);
        _settings = _source.Settings ?? BlendSettings.Default;
        InitializeComponent();
        ConfigureBreadcrumb(listBack);
        RefreshText();
        BuildFeatureRows();
        BuildColorChoices();
        BuildIconChoices();
    }

    private void ConfigureBreadcrumb(Action listBack)
    {
        BreadcrumbBar.Items.Clear();
        BreadcrumbBar.Items.Add(UiText.Get("configuration"));
        BreadcrumbBar.Items.Add(UiText.Get(_isNew ? "newCustomConfiguration" : "editConfiguration"));
        BreadcrumbBar.Tag = listBack;
    }

    private void RefreshText()
    {
        TitleText.Text = UiText.Get(_isNew ? "newCustomConfiguration" : "editConfiguration");
        NameBox.Text = _source.Name;
        ColorText.Text = UiText.Get("selectColor");
        IconText.Text = UiText.Get("selectIcon");
        CancelButton.Content = UiText.Get("cancel");
        SaveButton.Content = UiText.Get("save");
    }

    private void BuildColorChoices()
    {
        ColorPanel.Children.Clear();
        foreach (var key in ConfigurationPalette.Keys)
        {
            var selected = string.Equals(key, _selectedColorKey, StringComparison.OrdinalIgnoreCase);
            var button = new UiButton { Width = 26, Height = 26, Padding = new Thickness(0), Margin = new Thickness(0, 0, 7, 0), Background = ConfigurationPalette.GetBrush(key), BorderBrush = selected ? (Brush)FindResource("TextBrush") : null, BorderThickness = new Thickness(selected ? 2 : 0), CornerRadius = new CornerRadius(13), Cursor = Cursors.Hand, ToolTip = key, Tag = key };
            button.Click += (_, _) => { _selectedColorKey = key; BuildColorChoices(); };
            ColorPanel.Children.Add(button);
        }
    }

    private void BuildIconChoices()
    {
        IconPanel.Children.Clear();
        foreach (var key in ConfigurationIconCatalog.Keys)
        {
            var selected = string.Equals(key, _selectedIconKey, StringComparison.OrdinalIgnoreCase);
            var button = new UiButton { Width = 30, Height = 30, Padding = new Thickness(3), Margin = new Thickness(0, 0, 6, 0), Appearance = selected ? ControlAppearance.Secondary : ControlAppearance.Transparent, BorderBrush = selected ? (Brush)FindResource("AccentBrush") : null, BorderThickness = new Thickness(selected ? 1 : 0), Cursor = Cursors.Hand, ToolTip = key, Tag = key, Content = new SymbolIcon { Symbol = ConfigurationIconCatalog.GetSymbol(key), FontSize = 20 } };
            button.Click += (_, _) => { _selectedIconKey = key; BuildIconChoices(); };
            IconPanel.Children.Add(button);
        }
    }

    private void BuildFeatureRows()
    {
        _normalize.IsChecked = _settings.NormalizeContentWidth;
        _flatten.IsChecked = _settings.Protection == ProtectionMethod.Flatten;
        _watermark.IsChecked = _settings.Watermark != WatermarkStyle.None;
        _noise.IsChecked = _settings.Noise != NoiseStrength.Off;
        _width.Text = _settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _dpi.ItemsSource = new[] { 120, 150, 200, 300 }; _dpi.SelectedItem = _settings.Dpi;
        _watermarkType.ItemsSource = new[] { UiText.Get("text"), UiText.Get("fullPage") }; _watermarkType.SelectedIndex = Math.Max(0, (int)_settings.Watermark - 1);
        _watermarkText.Text = _settings.WatermarkText;
        _noiseLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") }; _noiseLevel.SelectedIndex = Math.Max(0, (int)_settings.Noise - 1);
        AddFeature(UiText.Get("normalizeWidth"), UiText.Get("normalizeHint"), _normalize, new UIElement[] { Labeled(UiText.Get("targetWidth"), _width) });
        AddFeature(UiText.Get("flatten"), UiText.Get("protectionHint"), _flatten, new UIElement[] { Labeled(UiText.Get("dpi"), _dpi) });
        AddFeature(UiText.Get("watermark"), UiText.Get("watermarkTypeHint"), _watermark, new UIElement[] { Labeled(UiText.Get("watermarkType"), _watermarkType), Labeled(UiText.Get("watermarkText"), _watermarkText) });
        AddFeature(UiText.Get("noise"), UiText.Get("noiseHint"), _noise, new UIElement[] { Labeled(UiText.Get("noise"), _noiseLevel) });
    }

    private void AddFeature(string title, string hint, UiToggleSwitch toggle, IEnumerable<UIElement> details)
    {
        var card = new UiCard { Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8), MinHeight = 72 };
        var root = new StackPanel();
        var header = new DockPanel();
        var labels = new StackPanel();
        labels.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold });
        labels.Children.Add(new TextBlock { Text = hint, Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 10, 0) });
        DockPanel.SetDock(toggle, Dock.Right);
        header.Children.Add(toggle);
        header.Children.Add(labels);
        root.Children.Add(header);
        var detail = new StackPanel { Visibility = toggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) };
        foreach (var item in details) detail.Children.Add(item);
        root.Children.Add(detail);
        toggle.Checked += (_, _) => detail.Visibility = Visibility.Visible;
        toggle.Unchecked += (_, _) => detail.Visibility = Visibility.Collapsed;
        card.Content = root;
        FeaturePanel.Children.Add(card);
    }

    private UIElement Labeled(string label, Control control)
    {
        var panel = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
        panel.Children.Add(new TextBlock { Text = label, Width = 150, VerticalAlignment = VerticalAlignment.Center });
        control.Width = 190;
        DockPanel.SetDock(control, Dock.Right);
        panel.Children.Add(control);
        return panel;
    }

    private void BreadcrumbBar_ItemClicked(object sender, BreadcrumbBarItemClickedEventArgs e)
    {
        if (e.Index == 0)
            Dispatcher.BeginInvoke(new Action(_listBack), DispatcherPriority.ContextIdle);
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameBox.Text))
        {
            ErrorText.Text = UiText.Get("configurationNameRequired");
            return;
        }
        var width = double.TryParse(_width.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 210;
        var settings = _settings with
        {
            NormalizeContentWidth = _normalize.IsChecked == true,
            TargetWidthMillimeters = width,
            Protection = _flatten.IsChecked == true ? ProtectionMethod.Flatten : ProtectionMethod.None,
            Dpi = _dpi.SelectedItem is int dpi ? dpi : 150,
            Watermark = _watermark.IsChecked == true ? (_watermarkType.SelectedIndex == 1 ? WatermarkStyle.FullPage : WatermarkStyle.Text) : WatermarkStyle.None,
            WatermarkText = _watermarkText.Text ?? string.Empty,
            Noise = _noise.IsChecked == true ? (NoiseStrength)Math.Clamp(_noiseLevel.SelectedIndex + 1, 1, 3) : NoiseStrength.Off
        };
        var errors = SettingsValidator.Validate(settings);
        if (errors.Count > 0)
        {
            ErrorText.Text = string.Join("\n", errors);
            return;
        }
        _save(_source, _isNew, NameBox.Text.Trim(), settings, _selectedColorKey, _selectedIconKey);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _parentBack();
}


