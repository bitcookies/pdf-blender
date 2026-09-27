using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PDFBlender.Core.Models;
using UiToggleSwitch = Wpf.Ui.Controls.ToggleSwitch;

namespace PDFBlender.App.Pages;

public partial class ConfigurationEditorWindow : Wpf.Ui.Controls.FluentWindow
{
    private readonly BlendConfiguration _source;
    private readonly UiToggleSwitch _normalize = new();
    private readonly UiToggleSwitch _flatten = new();
    private readonly UiToggleSwitch _watermark = new();
    private readonly UiToggleSwitch _noise = new();
    private readonly TextBox _width = new();
    private readonly ComboBox _dpi = new();
    private readonly ComboBox _watermarkType = new();
    private readonly TextBox _watermarkText = new();
    private readonly ComboBox _noiseLevel = new();

    public string ConfigurationName => NameBox.Text.Trim();
    public BlendSettings Settings { get; private set; }

    public ConfigurationEditorWindow(BlendConfiguration source)
    {
        _source = source;
        Settings = source.Settings;
        InitializeComponent();
        TitleText.Text = UiText.Get("editConfiguration");
        NameBox.Text = source.Name;
        CancelButton.Content = UiText.Get("cancel");
        SaveButton.Content = UiText.Get("save");
        BuildFeatureRows();
    }

    private void BuildFeatureRows()
    {
        _normalize.IsChecked = Settings.NormalizeContentWidth;
        _flatten.IsChecked = Settings.Protection == ProtectionMethod.Flatten;
        _watermark.IsChecked = Settings.Watermark != WatermarkStyle.None;
        _noise.IsChecked = Settings.Noise != NoiseStrength.Off;
        _width.Text = Settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _dpi.ItemsSource = new[] { 120, 150, 200, 300 }; _dpi.SelectedItem = Settings.Dpi;
        _watermarkType.ItemsSource = new[] { UiText.Get("text"), UiText.Get("fullPage") }; _watermarkType.SelectedIndex = Math.Max(0, (int)Settings.Watermark - 1);
        _watermarkText.Text = Settings.WatermarkText;
        _noiseLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") }; _noiseLevel.SelectedIndex = Math.Max(0, (int)Settings.Noise - 1);
        AddFeature(UiText.Get("normalizeWidth"), UiText.Get("normalizeHint"), _normalize, new UIElement[] { Labeled(UiText.Get("targetWidth"), _width) });
        AddFeature(UiText.Get("flatten"), UiText.Get("protectionHint"), _flatten, new UIElement[] { Labeled(UiText.Get("dpi"), _dpi) });
        AddFeature(UiText.Get("watermark"), UiText.Get("watermarkTypeHint"), _watermark, new UIElement[] { Labeled(UiText.Get("watermarkType"), _watermarkType), Labeled(UiText.Get("watermarkText"), _watermarkText) });
        AddFeature(UiText.Get("noise"), UiText.Get("noiseHint"), _noise, new UIElement[] { Labeled(UiText.Get("noise"), _noiseLevel) });
    }

    private void AddFeature(string title, string hint, UiToggleSwitch toggle, IEnumerable<UIElement> details)
    {
        var border = new Border { Style = (Style)FindResource("SettingRowStyle"), Padding = new Thickness(12), Margin = new Thickness(0, 0, 0, 8) };
        var root = new StackPanel();
        var header = new DockPanel();
        var labels = new StackPanel(); labels.Children.Add(new TextBlock { Text = title, FontWeight = FontWeights.SemiBold }); labels.Children.Add(new TextBlock { Text = hint, Foreground = (System.Windows.Media.Brush)FindResource("MutedTextBrush"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 10, 0) });
        DockPanel.SetDock(toggle, Dock.Right); header.Children.Add(toggle); header.Children.Add(labels); root.Children.Add(header);
        var detail = new StackPanel { Visibility = toggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed, Margin = new Thickness(0, 8, 0, 0) }; foreach (var item in details) detail.Children.Add(item); root.Children.Add(detail);
        toggle.Checked += (_, _) => detail.Visibility = Visibility.Visible; toggle.Unchecked += (_, _) => detail.Visibility = Visibility.Collapsed;
        border.Child = root; FeaturePanel.Children.Add(border);
    }

    private UIElement Labeled(string label, Control control)
    {
        var panel = new DockPanel { Margin = new Thickness(0, 3, 0, 3) }; panel.Children.Add(new TextBlock { Text = label, Width = 150, VerticalAlignment = VerticalAlignment.Center }); control.Width = 190; DockPanel.SetDock(control, Dock.Right); panel.Children.Add(control); return panel;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ConfigurationName)) return;
        var width = double.TryParse(_width.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 210;
        var settings = Settings with
        {
            NormalizeContentWidth = _normalize.IsChecked == true,
            TargetWidthMillimeters = width,
            Protection = _flatten.IsChecked == true ? ProtectionMethod.Flatten : ProtectionMethod.None,
            Dpi = _dpi.SelectedItem is int dpi ? dpi : 150,
            Watermark = _watermark.IsChecked == true ? (_watermarkType.SelectedIndex == 1 ? WatermarkStyle.FullPage : WatermarkStyle.Text) : WatermarkStyle.None,
            WatermarkText = _watermarkText.Text,
            Noise = _noise.IsChecked == true ? (NoiseStrength)(_noiseLevel.SelectedIndex + 1) : NoiseStrength.Off
        };
        var errors = SettingsValidator.Validate(settings);
        if (errors.Count > 0) { MessageBox.Show(string.Join("\n", errors), UiText.Get("invalidSettings"), MessageBoxButton.OK, MessageBoxImage.Warning); return; }
        Settings = settings; DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) { DialogResult = false; }
}
