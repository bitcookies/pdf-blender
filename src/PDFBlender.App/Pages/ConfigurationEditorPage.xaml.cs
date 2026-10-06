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
    private readonly DialogService _dialogs;
    private readonly BlendConfiguration _source;
    private readonly bool _isNew;
    private readonly Action _parentBack;
    private readonly Action _listBack;
    private readonly Action<BlendConfiguration, bool, string, BlendSettings, string, string> _save;
    private readonly UiToggleSwitch _normalize = new();
    private readonly UiToggleSwitch _flatten = new();
    private readonly UiToggleSwitch _watermark = new();
    private readonly UiToggleSwitch _noise = new();
    private readonly UiToggleSwitch _pageSize = new();
    private readonly UiToggleSwitch _texture = new();
    private readonly UiToggleSwitch _geometry = new();
    private readonly UiToggleSwitch _microText = new();
    private readonly UiToggleSwitch _copyLayer = new();
    private readonly UiToggleSwitch _hiddenAi = new();
    private readonly UiToggleSwitch _metadata = new();
    private readonly UiToggleSwitch _permissions = new();
    private readonly ComboBox _pageSizePreset = new();
    private readonly ComboBox _copyLayerMode = new();
    private readonly ComboBox _textureLevel = new();
    private readonly UiTextBox _width = new();
    private readonly ComboBox _dpi = new();
    private readonly ComboBox _watermarkType = new();
    private readonly UiTextBox _watermarkText = new();
    private readonly ComboBox _noiseLevel = new();
    private string _selectedColorKey;
    private string _selectedIconKey;
    private BlendSettings _settings;

    internal ConfigurationEditorPage(AppSession session, DialogService dialogs, BlendConfiguration? source, bool isNew, Action back, Action<BlendConfiguration, bool, string, BlendSettings, string, string> save, Action listBack)
    {
        _session = session;
        _dialogs = dialogs;
        _source = source ?? new BlendConfiguration("new", string.Empty, false, new BlendSettings { NormalizeContentWidth = false, Protection = ProtectionMethod.None, Watermark = WatermarkStyle.None, Noise = NoiseStrength.Off }, false, null, ConfigurationPalette.ForIndex(session.AllConfigurations.Count), ConfigurationIconCatalog.Normalize("regular:DocumentSettings20"));
        _isNew = isNew;
        _parentBack = back;
        _listBack = listBack;
        _save = save;
        _selectedColorKey = ConfigurationPalette.Normalize(_source.ColorKey);
        _selectedIconKey = ConfigurationIconCatalog.Normalize(_source.IconKey);
        _settings = _source.Settings ?? BlendSettings.Default;
        InitializeComponent();
        ConfigureBreadcrumb(listBack);
        BreadcrumbBarStyling.Attach(BreadcrumbBar);
        RefreshText();
        BuildFeatureRows();
        BuildColorChoices();
        UpdateIconPreview();
        NameBox.TextChanged += (_, _) => UpdateSaveState();
        UpdateSaveState();
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
        NameLabel.Text = UiText.Get("configurationName");
        NameBox.PlaceholderText = UiText.Get("configurationNamePlaceholder");
        NameBox.Text = _source.Name;
        ColorText.Text = UiText.Get("selectColor");
        IconText.Text = UiText.Get("selectIcon");
        CancelButton.Content = UiText.Get("cancel");
        SaveButton.Content = new TextBlock { Text = UiText.Get("save") };
    }

    private void BuildColorChoices()
    {
        ColorPanel.Children.Clear();
        foreach (var key in ConfigurationPalette.Keys)
        {
            var selected = string.Equals(key, _selectedColorKey, StringComparison.OrdinalIgnoreCase);
            var button = new Border
            {
                Width = 28,
                Height = 28,
                Margin = new Thickness(3, 2, 3, 2),
                Background = ConfigurationPalette.GetBrush(key),
                BorderBrush = selected ? (Brush)FindResource("TextBrush") : Brushes.Transparent,
                BorderThickness = new Thickness(selected ? 2 : 0),
                CornerRadius = new CornerRadius(14),
                Cursor = Cursors.Hand,
                ToolTip = key,
                Tag = key,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            button.MouseEnter += (_, _) =>
            {
                button.Background = ConfigurationPalette.GetBrush(key);
                button.BorderBrush = new SolidColorBrush(Color.FromRgb(0x8C, 0x8C, 0x8C));
                button.BorderThickness = new Thickness(2);
            };
            button.MouseLeave += (_, _) =>
            {
                button.Background = ConfigurationPalette.GetBrush(key);
                button.BorderBrush = selected ? (Brush)FindResource("TextBrush") : Brushes.Transparent;
                button.BorderThickness = new Thickness(selected ? 2 : 0);
            };
            button.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                _selectedColorKey = key;
                BuildColorChoices();
            };
            ColorPanel.Children.Add(button);
        }
    }

    private void UpdateIconPreview()
    {
        var descriptor = ConfigurationIconCatalog.GetDescriptor(_selectedIconKey);
        IconButton.Content = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Children =
            {
                new SymbolIcon { Symbol = descriptor.Symbol, Filled = descriptor.Filled, FontSize = 17, Margin = new Thickness(0, 0, 8, 0) },
                new TextBlock { Text = descriptor.Name, VerticalAlignment = VerticalAlignment.Center }
            }
        };
        IconButton.ToolTip = descriptor.Key;
    }

    private async void IconButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = await _dialogs.ShowIconPickerAsync(_selectedIconKey, IconButton);
        if (!string.IsNullOrWhiteSpace(selected))
        {
            _selectedIconKey = selected;
            UpdateIconPreview();
        }
    }

    private void BuildFeatureRows()
    {
        _normalize.IsChecked = _settings.NormalizeContentWidth;
        _pageSize.IsChecked = _settings.NormalizePageSize;
        _flatten.IsChecked = _settings.Protection == ProtectionMethod.Flatten;
        _watermark.IsChecked = _settings.Watermark != WatermarkStyle.None;
        _noise.IsChecked = _settings.Noise != NoiseStrength.Off;
        _texture.IsChecked = _settings.Texture != TextureStrength.Off;
        _geometry.IsChecked = _settings.GeometryPerturbation;
        _microText.IsChecked = _settings.MicroTextPattern;
        _copyLayer.IsChecked = _settings.CopyLayer != CopyLayerMode.Preserve;
        _hiddenAi.IsChecked = _settings.HiddenAiText;
        _metadata.IsChecked = _settings.MetadataNormalization || _settings.ProtectionDeclaration;
        _permissions.IsChecked = _settings.PermissionRestriction;
        _width.Text = _settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _dpi.ItemsSource = new[] { 120, 150, 180, 200, 300 }; _dpi.SelectedItem = _settings.Dpi;
        _pageSizePreset.ItemsSource = new[] { PageSizePreset.A5, PageSizePreset.A4, PageSizePreset.A3, PageSizePreset.A2, PageSizePreset.Letter, PageSizePreset.Legal, PageSizePreset.Custom };
        _pageSizePreset.SelectedItem = _settings.PageSize == PageSizePreset.Original ? PageSizePreset.A4 : _settings.PageSize;
        _watermarkType.ItemsSource = new[] { UiText.Get("text"), UiText.Get("fullPage") }; _watermarkType.SelectedIndex = Math.Max(0, (int)_settings.Watermark - 1);
        _watermarkText.Text = _settings.WatermarkText;
        _noiseLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") }; _noiseLevel.SelectedIndex = Math.Max(0, (int)_settings.Noise - 1);
        _textureLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium") }; _textureLevel.SelectedIndex = Math.Max(0, (int)_settings.Texture - 1);
        _copyLayerMode.ItemsSource = new[] { UiText.Get("unicodeScramble"), UiText.Get("textLayoutPerturbation"), UiText.Get("gibberishCopyLayer") }; _copyLayerMode.SelectedIndex = Math.Clamp((int)_settings.CopyLayer - 1, 0, 2);
        AddFeature(UiText.Get("normalizeWidth"), UiText.Get("normalizeHint"), _normalize, [Labeled(UiText.Get("targetWidth"), _width)]);
        AddFeature(UiText.Get("normalizePageSize"), UiText.Get("normalizePageSizeHint"), _pageSize, [Labeled(UiText.Get("pageSize"), _pageSizePreset)]);
        AddFeature(UiText.Get("flatten"), UiText.Get("protectionHint"), _flatten, [Labeled(UiText.Get("dpi"), _dpi)]);
        AddFeature(UiText.Get("watermark"), UiText.Get("watermarkTypeHint"), _watermark, [Labeled(UiText.Get("watermarkType"), _watermarkType), Labeled(UiText.Get("watermarkText"), _watermarkText)]);
        AddFeature(UiText.Get("noise"), UiText.Get("noiseHint"), _noise, [Labeled(UiText.Get("noise"), _noiseLevel)], true);
        AddFeature(UiText.Get("texture"), UiText.Get("textureHint"), _texture, [Labeled(UiText.Get("texture"), _textureLevel)], true);
        AddFeature(UiText.Get("geometryPerturbation"), UiText.Get("geometryPerturbationHint"), _geometry, [], true);
        AddFeature(UiText.Get("microTextPattern"), UiText.Get("microTextPatternHint"), _microText, [], true);
        AddFeature(UiText.Get("copyLayer"), UiText.Get("copyLayerHint"), _copyLayer, [Labeled(UiText.Get("copyLayerMode"), _copyLayerMode)], true);
        AddFeature(UiText.Get("hiddenAiText"), UiText.Get("hiddenAiTextHint"), _hiddenAi, []);
        AddFeature(UiText.Get("metadata"), UiText.Get("metadataHint"), _metadata, []);
        AddFeature(UiText.Get("permissionRestriction"), UiText.Get("permissionHint"), _permissions, [], true);
    }

    private void AddFeature(string title, string hint, UiToggleSwitch toggle, IEnumerable<UIElement> details, bool requiresFlatten = false)
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
        toggle.Checked += (_, _) => { detail.Visibility = Visibility.Visible; UpdateSaveState(); };
        toggle.Unchecked += (_, _) => { detail.Visibility = Visibility.Collapsed; UpdateSaveState(); };
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

    private void UpdateSaveState() {
        var hasName = !string.IsNullOrWhiteSpace(NameBox.Text);
        var hasFeature = _normalize.IsChecked == true || _pageSize.IsChecked == true || _flatten.IsChecked == true || _watermark.IsChecked == true || _noise.IsChecked == true || _texture.IsChecked == true || _geometry.IsChecked == true || _microText.IsChecked == true || _copyLayer.IsChecked == true || _hiddenAi.IsChecked == true || _metadata.IsChecked == true || _permissions.IsChecked == true;
        SaveButton.IsEnabled = hasName && (!_isNew || hasFeature);
        var saveBrush = SaveButton.IsEnabled ? (Brush)FindResource("AccentTextBrush") : (Brush)FindResource("MutedTextBrush");
        SaveButton.Foreground = saveBrush;
        if (SaveButton.Content is TextBlock saveText) saveText.Foreground = saveBrush;
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
            NormalizePageSize = _pageSize.IsChecked == true,
            PageSize = _pageSizePreset.SelectedItem is PageSizePreset pageSize ? pageSize : PageSizePreset.Original,
            Protection = _flatten.IsChecked == true ? ProtectionMethod.Flatten : ProtectionMethod.None,
            Dpi = _dpi.SelectedItem is int dpi ? dpi : 150,
            Watermark = _watermark.IsChecked == true ? (_watermarkType.SelectedIndex == 1 ? WatermarkStyle.FullPage : WatermarkStyle.Text) : WatermarkStyle.None,
            WatermarkText = _watermarkText.Text ?? string.Empty,
            Noise = _noise.IsChecked == true ? (NoiseStrength)Math.Clamp(_noiseLevel.SelectedIndex + 1, 1, 3) : NoiseStrength.Off,
            Texture = _texture.IsChecked == true ? (TextureStrength)Math.Clamp(_textureLevel.SelectedIndex + 1, 1, 2) : TextureStrength.Off,
            GeometryPerturbation = _geometry.IsChecked == true,
            MicroTextPattern = _microText.IsChecked == true,
            CopyLayer = _copyLayer.IsChecked == true ? (CopyLayerMode)Math.Clamp(_copyLayerMode.SelectedIndex + 1, 1, 3) : CopyLayerMode.Preserve,
            HiddenAiText = _hiddenAi.IsChecked == true,
            MetadataNormalization = _metadata.IsChecked == true,
            ProtectionDeclaration = _metadata.IsChecked == true,
            PermissionRestriction = _permissions.IsChecked == true
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
