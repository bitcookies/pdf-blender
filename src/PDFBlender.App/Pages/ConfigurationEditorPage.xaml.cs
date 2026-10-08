using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
    private readonly ComboBox _contentWidthPreset = new();
    private readonly ComboBox _pageSizePreset = new();
    private readonly ComboBox _imageFormat = new();
    private readonly ComboBox _imageQuality = new();
    private readonly ComboBox _watermarkAngle = new();
    private readonly ComboBox _copyLayerMode = new();
    private readonly ComboBox _textureLevel = new();
    private readonly UiTextBox _promptInjectionText = new();
    private readonly UiToggleSwitch _promptInjection = new();
    private readonly UiToggleSwitch _gan = new();
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
        _source = source ?? new BlendConfiguration("new", string.Empty, false, new BlendSettings { NormalizeContentWidth = false, Protection = ProtectionMethod.None, Watermark = WatermarkStyle.None, Noise = NoiseStrength.Off }, false, null, ConfigurationPalette.Keys[0], ConfigurationIconCatalog.Normalize("regular:DocumentSettings20"));
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

    public void RefreshFromSession()
    {
        var name = NameBox.Text;
        UiText.SetLanguage(_session.Preferences.Language);
        RefreshText();
        NameBox.Text = name;
        BuildColorChoices();
        UpdateIconPreview();
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
        SaveButton.Content = new TextBlock { Text = UiText.Get("save"), VerticalAlignment = VerticalAlignment.Center, TextAlignment = TextAlignment.Center };
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
                BorderBrush = selected ? (Brush)FindResource("ColorChoiceActiveBorderBrush") : Brushes.Transparent,
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
                button.BorderBrush = (Brush)FindResource(selected ? "ColorChoiceActiveBorderBrush" : "ColorChoiceHoverBorderBrush");
                button.BorderThickness = new Thickness(2);
            };
            button.MouseLeave += (_, _) =>
            {
                button.Background = ConfigurationPalette.GetBrush(key);
                button.BorderBrush = selected ? (Brush)FindResource("ColorChoiceActiveBorderBrush") : Brushes.Transparent;
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
                new SymbolIcon
                {
                    Symbol = descriptor.Symbol,
                    Filled = descriptor.Filled,
                    FontSize = 17,
                    Foreground = (Brush)FindResource("IconPickerForegroundBrush"),
                    Margin = new Thickness(0, 0, 8, 0)
                },
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
        _promptInjection.IsChecked = !string.IsNullOrWhiteSpace(_settings.PromptInjectionText);
        _promptInjectionText.Text = _settings.PromptInjectionText;
        _gan.IsChecked = _settings.GanAdversarialPerturbation;
        _metadata.IsChecked = _settings.MetadataNormalization || _settings.ProtectionDeclaration;
        _permissions.IsChecked = _settings.PermissionRestriction;

        _contentWidthPreset.ItemsSource = new[] { PageSizePreset.A5, PageSizePreset.A4, PageSizePreset.A3, PageSizePreset.Letter, PageSizePreset.Legal, PageSizePreset.Custom };
        _contentWidthPreset.SelectedItem = _settings.ContentWidthPreset == PageSizePreset.Original ? PageSizePreset.A4 : _settings.ContentWidthPreset;
        _width.Text = _settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _dpi.ItemsSource = new[] { 120, 150, 180, 200, 300 };
        _dpi.SelectedItem = _settings.Dpi;
        _pageSizePreset.ItemsSource = new[] { PageSizePreset.A5, PageSizePreset.A4, PageSizePreset.A3, PageSizePreset.A2, PageSizePreset.Letter, PageSizePreset.Legal };
        _pageSizePreset.SelectedItem = _settings.PageSize == PageSizePreset.Original ? PageSizePreset.A4 : _settings.PageSize;
        _imageFormat.ItemsSource = new[] { PageImageFormat.WebP, PageImageFormat.Jpeg, PageImageFormat.Png };
        _imageFormat.SelectedItem = _settings.ImageFormat;
        _imageQuality.ItemsSource = new[] { ImageQuality.Low, ImageQuality.Medium, ImageQuality.High, ImageQuality.Maximum };
        _imageQuality.SelectedItem = _settings.ImageQuality;
        _watermarkType.ItemsSource = new[] { UiText.Get("text"), UiText.Get("fullPage") };
        _watermarkType.SelectedIndex = Math.Max(0, (int)_settings.Watermark - 1);
        _watermarkAngle.ItemsSource = new[] { -45d, 0d, 45d, 90d };
        _watermarkAngle.SelectedItem = _settings.WatermarkAngle;
        _watermarkText.Text = _settings.WatermarkText;
        _watermarkText.AcceptsReturn = true;
        _watermarkText.TextWrapping = TextWrapping.Wrap;
        _watermarkText.Height = 74;
        _noiseLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") };
        _noiseLevel.SelectedIndex = Math.Max(0, (int)_settings.Noise - 1);
        _textureLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium") };
        _textureLevel.SelectedIndex = Math.Max(0, (int)_settings.Texture - 1);
        _copyLayerMode.ItemsSource = new[] { UiText.Get("unicodeScramble"), UiText.Get("textLayoutPerturbation"), UiText.Get("gibberishCopyLayer") };
        _copyLayerMode.SelectedIndex = Math.Clamp((int)_settings.CopyLayer - 1, 0, 2);

        AddSection(UiText.Get("pageFormat"));
        AddFeature(UiText.Get("normalizeWidth"), UiText.Get("normalizeHint"), _normalize, [Labeled(UiText.Get("targetWidthPreset"), _contentWidthPreset), Labeled(UiText.Get("targetWidth"), _width)]);
        AddFeature(UiText.Get("normalizePageSize"), UiText.Get("normalizePageSizeHint"), _pageSize, [Labeled(UiText.Get("pageSize"), _pageSizePreset)]);

        AddSection(UiText.Get("flattening"));
        AddFeature(UiText.Get("flatten"), UiText.Get("protectionHint"), _flatten, [Labeled(UiText.Get("dpi"), _dpi), Labeled(UiText.Get("imageFormat"), _imageFormat), Labeled(UiText.Get("imageQuality"), _imageQuality)]);

        AddSection(UiText.Get("watermarkGroup"));
        AddFeature(UiText.Get("watermark"), UiText.Get("watermarkTypeHint"), _watermark, [Labeled(UiText.Get("watermarkType"), _watermarkType), Labeled(UiText.Get("watermarkAngle"), _watermarkAngle), Labeled(UiText.Get("watermarkText"), _watermarkText)]);

        AddSection(UiText.Get("noisePerturbation"));
        AddFeature(UiText.Get("noise"), UiText.Get("noiseHint"), _noise, [Labeled(UiText.Get("noise"), _noiseLevel)], true);
        AddFeature(UiText.Get("texture"), UiText.Get("textureHint"), _texture, [Labeled(UiText.Get("texture"), _textureLevel)], true);
        AddFeature(UiText.Get("geometryPerturbation"), UiText.Get("geometryPerturbationHint"), _geometry, [], true);
        AddFeature(UiText.Get("microTextPattern"), UiText.Get("microTextPatternHint"), _microText, [], true);

        AddSection(UiText.Get("copyGroup"));
        AddFeature(UiText.Get("copyLayer"), UiText.Get("copyLayerHint"), _copyLayer, [Labeled(UiText.Get("copyLayerMode"), _copyLayerMode)], true);

        AddSection(UiText.Get("promptInjectionGroup"));
        AddFeature(UiText.Get("hiddenAiText"), UiText.Get("hiddenAiTextHint"), _hiddenAi, []);
        AddFeature(UiText.Get("promptInjection"), UiText.Get("promptInjectionHint"), _promptInjection, [Labeled(UiText.Get("promptText"), _promptInjectionText)]);

        AddSection(UiText.Get("ganGroup"));
        AddFeature(UiText.Get("ganPerturbation"), UiText.Get("ganPerturbationHint"), _gan, [], true);

        AddSection(UiText.Get("copyrightProtection"));
        AddFeature(UiText.Get("metadata"), UiText.Get("metadataHint"), _metadata, []);
        AddFeature(UiText.Get("permissionRestriction"), UiText.Get("permissionHint"), _permissions, [], true);

        _normalize.Checked += (_, _) => { _pageSize.IsChecked = false; UpdateSaveState(); };
        _pageSize.Checked += (_, _) => { _normalize.IsChecked = false; UpdateSaveState(); };
        _contentWidthPreset.SelectionChanged += (_, _) => UpdateWidthFromPreset();
        UpdateWidthFromPreset();
    }

    private void AddSection(string title)
    {
        FeaturePanel.Children.Add(new TextBlock
        {
            Text = title,
            Style = (Style)FindResource("SectionTitleTextStyle"),
            Margin = new Thickness(0, 14, 0, 8)
        });
    }

    private void UpdateWidthFromPreset()
    {
        if (_contentWidthPreset.SelectedItem is not PageSizePreset preset)
            return;
        _width.Visibility = preset == PageSizePreset.Custom ? Visibility.Visible : Visibility.Collapsed;
        if (preset == PageSizePreset.Custom)
            return;
        _width.Text = preset switch
        {
            PageSizePreset.A5 => "148",
            PageSizePreset.A4 => "210",
            PageSizePreset.A3 => "297",
            PageSizePreset.Letter => "215.9",
            PageSizePreset.Legal => "215.9",
            _ => _width.Text
        };
    }

    private void AddFeature(string title, string hint, UiToggleSwitch toggle, IEnumerable<UIElement> details, bool requiresFlatten = false)
    {
        // WPF-UI binds ContentBorder.VerticalAlignment to the Card itself.
        // Center would move the visible border up when content outgrows MinHeight.
        var card = new UiCard
        {
            Padding = new Thickness(12),
            Margin = new Thickness(0, 0, 0, 8),
            Height = 72,
            MinHeight = 72,
            VerticalAlignment = VerticalAlignment.Top,
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        var root = new StackPanel();
        // Reserve the full collapsed surface: 72 minus padding and the 1px border.
        var header = new DockPanel { MinHeight = 72 - 24 - 2 };
        var labels = new StackPanel();
        var titleText = new TextBlock { Text = title, FontWeight = FontWeights.SemiBold };
        titleText.SetResourceReference(TextBlock.ForegroundProperty, "TextBrush");
        var hintText = new TextBlock
        {
            Text = hint,
            FontSize = 11,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 3, 10, 0)
        };
        hintText.SetResourceReference(TextBlock.ForegroundProperty, "MutedTextBrush");
        labels.Children.Add(titleText);
        labels.Children.Add(hintText);
        DockPanel.SetDock(toggle, Dock.Right);
        header.Children.Add(toggle);
        header.Children.Add(labels);
        root.Children.Add(header);

        // Keep spacing inside the clipped host so it expands with the details.
        var detailContent = new StackPanel { Margin = new Thickness(0, 8, 0, 0) };
        foreach (var item in details)
            detailContent.Children.Add(item);
        var detail = new Border
        {
            Visibility = Visibility.Collapsed,
            Height = 0,
            ClipToBounds = true,
            VerticalAlignment = VerticalAlignment.Top,
            Child = detailContent
        };
        root.Children.Add(detail);
        card.Content = root;
        FeaturePanel.Children.Add(card);

        var disabledBrush = (Brush)FindResource("DisabledTextBrush");

        void RefreshEnabledVisual()
        {
            var foreground = toggle.IsEnabled
                ? (Brush)FindResource("TextBrush")
                : disabledBrush;
            var descriptionForeground = toggle.IsEnabled
                ? (Brush)FindResource("MutedTextBrush")
                : disabledBrush;
            titleText.Foreground = foreground;
            hintText.Foreground = descriptionForeground;
        }

        var expansionVersion = 0;
        void SetExpanded(bool expanded, bool animate = true)
        {
            var version = ++expansionVersion;
            var current = detail.Visibility == Visibility.Visible ? detail.ActualHeight : 0;
            detail.BeginAnimation(FrameworkElement.HeightProperty, null);

            if (detailContent.Children.Count == 0)
                return;

            card.Height = double.NaN;
            detail.Visibility = Visibility.Visible;
            // Measure the natural content, not the host whose animated Height may be zero.
            var width = root.ActualWidth > 0 ? root.ActualWidth : double.PositiveInfinity;
            detailContent.Measure(new Size(width, double.PositiveInfinity));
            var target = expanded ? detailContent.DesiredSize.Height : 0;

            void Complete()
            {
                if (version != expansionVersion)
                    return;
                detail.BeginAnimation(FrameworkElement.HeightProperty, null);
                detail.Height = expanded ? double.NaN : 0;
                detail.Visibility = expanded ? Visibility.Visible : Visibility.Collapsed;
                card.Height = expanded ? double.NaN : 72;
            }

            if (!animate || !IsLoaded || !SystemParameters.ClientAreaAnimation ||
                Math.Abs(current - target) < 0.1)
            {
                Complete();
                return;
            }

            detail.Height = current;
            var animation = new DoubleAnimation(current, target,
                TimeSpan.FromMilliseconds(expanded ? 180 : 150))
            {
                EasingFunction = new CubicEase
                {
                    EasingMode = expanded ? EasingMode.EaseOut : EasingMode.EaseIn
                }
            };
            animation.Completed += (_, _) => Complete();
            detail.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        toggle.IsEnabled = !requiresFlatten || _flatten.IsChecked == true;
        RefreshEnabledVisual();
        toggle.IsEnabledChanged += (_, _) => RefreshEnabledVisual();
        toggle.Checked += (_, _) =>
        {
            SetExpanded(true);
            UpdateSaveState();
        };
        toggle.Unchecked += (_, _) =>
        {
            SetExpanded(false);
            UpdateSaveState();
        };
        if (requiresFlatten)
        {
            _flatten.Checked += (_, _) =>
            {
                toggle.IsEnabled = true;
                RefreshEnabledVisual();
            };
            _flatten.Unchecked += (_, _) =>
            {
                toggle.IsChecked = false;
                toggle.IsEnabled = false;
                RefreshEnabledVisual();
            };
        }

        SetExpanded(toggle.IsChecked == true, animate: false);
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
        var hasFeature = _normalize.IsChecked == true || _pageSize.IsChecked == true || _flatten.IsChecked == true || _watermark.IsChecked == true || _noise.IsChecked == true || _texture.IsChecked == true || _geometry.IsChecked == true || _microText.IsChecked == true || _copyLayer.IsChecked == true || _hiddenAi.IsChecked == true || _promptInjection.IsChecked == true || _gan.IsChecked == true || _metadata.IsChecked == true || _permissions.IsChecked == true;
        SaveButton.IsEnabled = hasName && (!_isNew || hasFeature);
        var saveTextResource = SaveButton.IsEnabled ? "AccentTextBrush" : "EditorSaveDisabledTextBrush";

        if (SaveButton.Content is TextBlock saveText)
        {
            saveText.ClearValue(TextBlock.ForegroundProperty);
            saveText.SetResourceReference(TextBlock.ForegroundProperty, saveTextResource);
        }
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
