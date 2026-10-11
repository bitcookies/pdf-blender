using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using PDFBlender.Core.Models;
using UiCard = Wpf.Ui.Controls.Card;
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
    private readonly ComboBox _imageQuality = new();
    private readonly ComboBox _watermarkAngle = new();
    private readonly ComboBox _copyLayerMode = new();
    private readonly ComboBox _textureLevel = new();
    private readonly UiTextBox _promptInjectionText = new();
    private readonly UiTextBox _copyLayerCustomText = new();
    private readonly UiTextBox _metadataAuthor = new();
    private readonly UiTextBox _metadataOrganization = new();
    private readonly UiTextBox _metadataCopyright = new();
    private readonly ComboBox _promptTemplates = new();
    private readonly HashSet<string> _selectedPromptTemplateIds = new(StringComparer.OrdinalIgnoreCase);
    private readonly CheckBox _allowPrinting = new();
    private readonly CheckBox _allowCopying = new();
    private readonly CheckBox _allowModification = new();
    private readonly CheckBox _allowAnnotations = new();
    private readonly CheckBox _allowForms = new();
    private readonly UiToggleSwitch _promptInjection = new();
    private readonly UiTextBox _width = new();
    private readonly UiTextBox _pageWidth = new();
    private readonly UiTextBox _pageHeight = new();
    private readonly ComboBox _dpi = new();
    private readonly ComboBox _watermarkType = new();
    private readonly UiTextBox _watermarkText = new();
    private readonly ComboBox _noiseLevel = new();
    private string _selectedColorKey;
    private string _selectedIconKey;
    private BlendSettings _settings;
    private readonly Dictionary<FrameworkElement, int> _visibilityVersions = new();

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
        _promptInjection.IsChecked = _settings.PromptInjection;
        _promptInjectionText.Text = _settings.PromptInjectionText;
        _promptInjectionText.AcceptsReturn = true;
        _promptInjectionText.TextWrapping = TextWrapping.Wrap;
        _promptInjectionText.Height = 74;
        _promptInjectionText.MaxLength = 4000;
        _copyLayerCustomText.Text = _settings.CopyLayerCustomText;
        _copyLayerCustomText.AcceptsReturn = true;
        _copyLayerCustomText.TextWrapping = TextWrapping.Wrap;
        _copyLayerCustomText.MaxLength = 4000;
        _metadataAuthor.Text = _settings.MetadataAuthorName;
        _metadataOrganization.Text = _settings.MetadataOrganization;
        _metadataCopyright.Text = _settings.MetadataCopyright;
        _allowPrinting.IsChecked = _settings.PermissionAllowPrinting;
        _allowCopying.IsChecked = _settings.PermissionAllowCopying;
        _allowModification.IsChecked = _settings.PermissionAllowModification;
        _allowAnnotations.IsChecked = _settings.PermissionAllowAnnotations;
        _allowForms.IsChecked = _settings.PermissionAllowFormFilling;
        ConfigurePromptTemplatePicker();
        _metadata.IsChecked = _settings.MetadataNormalization || _settings.ProtectionDeclaration;
        _permissions.IsChecked = _settings.PermissionRestriction;

        _contentWidthPreset.ItemsSource = CommonPageSizes(includeCustom: true);
        _contentWidthPreset.SelectedItem = _settings.ContentWidthPreset == PageSizePreset.Original ? PageSizePreset.A4 : _settings.ContentWidthPreset;
        _width.Text = _settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _dpi.ItemsSource = new[] { 72, 150, 200, 300, 450, 600 };
        _dpi.SelectedItem = _settings.Dpi;
        _pageSizePreset.ItemsSource = CommonPageSizes(includeCustom: true);
        _pageSizePreset.SelectedItem = _settings.PageSize == PageSizePreset.Original ? PageSizePreset.A4 : _settings.PageSize;
        _pageWidth.Text = _settings.PageWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _pageHeight.Text = _settings.PageHeightMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        _imageQuality.ItemsSource = new[] { UiText.Get("qualityCompact"), UiText.Get("qualityStandard"), UiText.Get("qualityHigh"), UiText.Get("qualityLossless") };
        _imageQuality.SelectedIndex = (int)_settings.ImageQuality;
        _watermarkType.ItemsSource = new[] { UiText.Get("singleWatermark"), UiText.Get("repeatedWatermark") };
        _watermarkType.SelectedIndex = Math.Max(0, (int)_settings.Watermark - 1);
        _watermarkAngle.ItemsSource = new[] { -45d, 0d, 45d, 90d };
        _watermarkAngle.SelectedItem = _settings.WatermarkAngle;
        _watermarkText.Text = _settings.WatermarkText;
        _watermarkText.AcceptsReturn = true;
        _watermarkText.TextWrapping = TextWrapping.Wrap;
        _watermarkText.Height = 74;
        _noiseLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") };
        _noiseLevel.SelectedIndex = Math.Max(0, (int)_settings.Noise - 1);
        _textureLevel.ItemsSource = new[] { UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") };
        _textureLevel.SelectedIndex = Math.Max(0, (int)_settings.Texture - 1);
        _copyLayerMode.ItemsSource = new[] { UiText.Get("unicodeScramble"), UiText.Get("textLayoutPerturbation"), UiText.Get("gibberishCopyLayer"), UiText.Get("customWarning") };
        _copyLayerMode.SelectedIndex = Math.Clamp((int)_settings.CopyLayer - 1, 0, 3);

        AddSection(UiText.Get("pageFormat"));
        AddFeature(UiText.Get("normalizeWidth"), UiText.Get("normalizeHint"), _normalize, [Labeled(UiText.Get("targetWidthPreset"), _contentWidthPreset), Labeled(UiText.Get("pageWidth"), _width)]);
        AddFeature(UiText.Get("normalizePageSize"), UiText.Get("normalizePageSizeHint"), _pageSize, [
            Labeled(UiText.Get("pageSize"), _pageSizePreset),
            Labeled(UiText.Get("pageWidth"), _pageWidth),
            Labeled(UiText.Get("pageHeight"), _pageHeight)
        ]);

        AddSection(UiText.Get("flattening"));
        AddFeature(UiText.Get("flatten"), UiText.Get("protectionHint"), _flatten, [Labeled(UiText.Get("dpi"), _dpi), Labeled(UiText.Get("imageQuality"), _imageQuality)]);

        AddSection(UiText.Get("watermarkGroup"));
        AddFeature(UiText.Get("watermark"), UiText.Get("watermarkTypeHint"), _watermark, [Labeled(UiText.Get("watermarkType"), _watermarkType), Labeled(UiText.Get("watermarkAngle"), _watermarkAngle), Labeled(UiText.Get("watermarkText"), _watermarkText)]);

        AddSection(UiText.Get("noisePerturbation"));
        AddFeature(UiText.Get("noise"), UiText.Get("noiseHint"), _noise, [Labeled(UiText.Get("noise"), _noiseLevel)], true);
        AddFeature(UiText.Get("texture"), UiText.Get("textureHint"), _texture, [Labeled(UiText.Get("texture"), _textureLevel)], true);
        AddFeature(UiText.Get("geometryPerturbation"), UiText.Get("geometryPerturbationHint"), _geometry, [], true);
        AddFeature(UiText.Get("microTextPattern"), UiText.Get("microTextPatternHint"), _microText, [], true);

        AddSection(UiText.Get("copyGroup"));
        AddFeature(UiText.Get("copyLayer"), UiText.Get("copyLayerHint"), _copyLayer, [Labeled(UiText.Get("copyLayerMode"), _copyLayerMode), Labeled(UiText.Get("customWarning"), _copyLayerCustomText)], true);

        AddSection(UiText.Get("promptInjectionGroup"));
        AddFeature(UiText.Get("hiddenAiText"), UiText.Get("hiddenAiTextHint"), _hiddenAi, [Labeled(UiText.Get("promptTemplates"), _promptTemplates)]);
        AddFeature(UiText.Get("promptInjection"), UiText.Get("promptInjectionHint"), _promptInjection, [Labeled(UiText.Get("promptText"), _promptInjectionText)]);
        AddSection(UiText.Get("copyrightProtection"));
        AddFeature(UiText.Get("metadata"), UiText.Get("metadataHint"), _metadata, [Labeled(UiText.Get("author"), _metadataAuthor), Labeled(UiText.Get("organization"), _metadataOrganization), Labeled(UiText.Get("copyright"), _metadataCopyright)]);
        AddFeature(UiText.Get("permissionRestriction"), UiText.Get("permissionHint"), _permissions, [CreatePermissionOptions()]);
        UpdateCopyLayerDetailsVisibility();

        _normalize.Checked += (_, _) => { _pageSize.IsChecked = false; UpdateSaveState(); };
        _pageSize.Checked += (_, _) => { _normalize.IsChecked = false; UpdateSaveState(); };
        _contentWidthPreset.SelectionChanged += (_, _) => UpdateWidthFromPreset();
        _pageSizePreset.SelectionChanged += (_, _) => UpdatePageSizeFromPreset();
        _copyLayerMode.SelectionChanged += (_, _) => { UpdateCopyLayerDetailsVisibility(); UpdateSaveState(); };

        _copyLayerCustomText.TextChanged += (_, _) => UpdateSaveState();
        UpdateWidthFromPreset();
        UpdatePageSizeFromPreset();
    }

    private void ConfigurePromptTemplatePicker()
    {
        _selectedPromptTemplateIds.Clear();
        var currentIds = _settings.PromptTemplateIds ?? Array.Empty<string>();
        var labels = new[]
        {
            "promptTemplatePriority",
            "promptTemplateRedirection",
            "promptTemplateRefusal",
            "promptTemplateClassical"
        };

        _promptTemplates.Items.Clear();
        for (var i = 0; i < PromptTemplateCatalog.Ids.Count && i < labels.Length; i++)
            _promptTemplates.Items.Add(UiText.Get(labels[i]));

        var selectedIndex = -1;
        for (var i = 0; i < PromptTemplateCatalog.Ids.Count; i++)
        {
            if (currentIds.Contains(PromptTemplateCatalog.Ids[i], StringComparer.OrdinalIgnoreCase))
            {
                selectedIndex = i;
                break;
            }
        }

        _promptTemplates.Height = 38;
        _promptTemplates.SelectedIndex = selectedIndex;
        if (selectedIndex >= 0)
            _selectedPromptTemplateIds.Add(PromptTemplateCatalog.Ids[selectedIndex]);

        _promptTemplates.SelectionChanged += (_, _) =>
        {
            _selectedPromptTemplateIds.Clear();
            var index = _promptTemplates.SelectedIndex;
            if (index >= 0 && index < PromptTemplateCatalog.Ids.Count)
                _selectedPromptTemplateIds.Add(PromptTemplateCatalog.Ids[index]);
            UpdateSaveState();
        };
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
        SetControlRowVisibility(_width, _width.Visibility);
        if (preset == PageSizePreset.Custom)
            return;
        _width.Text = WidthForPreset(preset).ToString("0.##", CultureInfo.InvariantCulture);
    }

    private void UpdatePageSizeFromPreset()
    {
        if (_pageSizePreset.SelectedItem is not PageSizePreset preset)
            return;
        var custom = preset == PageSizePreset.Custom;
        _pageWidth.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        SetControlRowVisibility(_pageWidth, _pageWidth.Visibility);
        _pageHeight.Visibility = custom ? Visibility.Visible : Visibility.Collapsed;
        SetControlRowVisibility(_pageHeight, _pageHeight.Visibility);
        if (custom)
            return;
        var (width, height) = DimensionsForPreset(preset);
        _pageWidth.Text = width.ToString("0.##", CultureInfo.InvariantCulture);
        _pageHeight.Text = height.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static IReadOnlyList<PageSizePreset> CommonPageSizes(bool includeCustom) =>
        new[]
        {
            PageSizePreset.A6, PageSizePreset.A5, PageSizePreset.A4, PageSizePreset.A3, PageSizePreset.A2,
            PageSizePreset.A1, PageSizePreset.A0, PageSizePreset.B5, PageSizePreset.B4, PageSizePreset.B3,
            PageSizePreset.B2, PageSizePreset.B1, PageSizePreset.B0, PageSizePreset.C5, PageSizePreset.C4,
            PageSizePreset.Letter, PageSizePreset.Legal, PageSizePreset.Tabloid, PageSizePreset.Executive
        }.Concat(includeCustom ? new[] { PageSizePreset.Custom } : Array.Empty<PageSizePreset>()).ToArray();

    private static double WidthForPreset(PageSizePreset preset) => preset switch
    {
        PageSizePreset.A6 => 105,
        PageSizePreset.A5 => 148,
        PageSizePreset.A4 => 210,
        PageSizePreset.A3 => 297,
        PageSizePreset.A2 => 420,
        PageSizePreset.A1 => 594,
        PageSizePreset.A0 => 841,
        PageSizePreset.B5 => 176,
        PageSizePreset.B4 => 250,
        PageSizePreset.B3 => 353,
        PageSizePreset.B2 => 500,
        PageSizePreset.B1 => 707,
        PageSizePreset.B0 => 1000,
        PageSizePreset.C5 => 162,
        PageSizePreset.C4 => 229,
        PageSizePreset.Letter => 215.9,
        PageSizePreset.Legal => 215.9,
        PageSizePreset.Tabloid => 279.4,
        PageSizePreset.Executive => 184.15,
        _ => 210
    };

    private static (double Width, double Height) DimensionsForPreset(PageSizePreset preset) => preset switch
    {
        PageSizePreset.A6 => (105, 148),
        PageSizePreset.A5 => (148, 210),
        PageSizePreset.A4 => (210, 297),
        PageSizePreset.A3 => (297, 420),
        PageSizePreset.A2 => (420, 594),
        PageSizePreset.A1 => (594, 841),
        PageSizePreset.A0 => (841, 1189),
        PageSizePreset.B5 => (176, 250),
        PageSizePreset.B4 => (250, 353),
        PageSizePreset.B3 => (353, 500),
        PageSizePreset.B2 => (500, 707),
        PageSizePreset.B1 => (707, 1000),
        PageSizePreset.B0 => (1000, 1414),
        PageSizePreset.C5 => (162, 229),
        PageSizePreset.C4 => (229, 324),
        PageSizePreset.Letter => (215.9, 279.4),
        PageSizePreset.Legal => (215.9, 355.6),
        PageSizePreset.Tabloid => (279.4, 431.8),
        PageSizePreset.Executive => (184.15, 266.7),
        _ => (210, 297)
    };

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
        var detailContent = new StackPanel { Margin = new Thickness(0, 8, 96, 0) };
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

    private void UpdateCopyLayerDetailsVisibility()
    {
        SetControlRowVisibility(_copyLayerCustomText, _copyLayerMode.SelectedIndex == 3 ? Visibility.Visible : Visibility.Collapsed);
    }
    private UIElement Labeled(string label, Control control)
    {
        var panel = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        panel.Children.Add(new TextBlock
        {
            Text = label,
            VerticalAlignment = (control as UiTextBox)?.AcceptsReturn == true ? VerticalAlignment.Top : VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0)
        });

        if (control is UiTextBox textBox && textBox.AcceptsReturn)
        {
            var editor = new Grid
            {
                Height = 74,
                MinHeight = 64,
                MaxHeight = 180,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            textBox.Width = double.NaN;
            textBox.Height = double.NaN;
            textBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            textBox.VerticalContentAlignment = VerticalAlignment.Top;
            editor.Children.Add(textBox);
            var grip = new Thumb
            {
                Width = 14,
                Height = 14,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Cursor = Cursors.SizeNS,
                Background = Brushes.Transparent,
                Opacity = .7,
                ToolTip = UiText.Get("resizeText")
            };
            grip.DragDelta += (_, e) =>
            {
                editor.Height = Math.Clamp(editor.Height + e.VerticalChange, editor.MinHeight, editor.MaxHeight);
            };
            editor.Children.Add(grip);
            Grid.SetColumn(editor, 1);
            panel.Children.Add(editor);
        }
        else
        {
            control.Width = 190;
            control.HorizontalAlignment = HorizontalAlignment.Left;
            control.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(control, 1);
            panel.Children.Add(control);
        }

        return panel;
    }
    private UIElement CreatePermissionOptions()
    {
        var grid = new Grid { Margin = new Thickness(0, 3, 0, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var options = new[]
        {
            (_allowPrinting, "allowPrinting"),
            (_allowCopying, "allowCopying"),
            (_allowModification, "allowModification"),
            (_allowAnnotations, "allowAnnotations"),
            (_allowForms, "allowForms")
        };
        for (var i = 0; i < options.Length; i++)
        {
            var (checkBox, textKey) = options[i];
            var row = i / 2;
            while (grid.RowDefinitions.Count <= row)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            checkBox.Content = UiText.Get(textKey);
            checkBox.Width = double.NaN;
            checkBox.MinHeight = 32;
            checkBox.Margin = new Thickness(0, 2, 16, 2);
            checkBox.HorizontalAlignment = HorizontalAlignment.Left;
            checkBox.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetRow(checkBox, row);
            Grid.SetColumn(checkBox, i % 2);
            grid.Children.Add(checkBox);
        }
        return grid;
    }
    private void SetControlRowVisibility(FrameworkElement control, Visibility visibility)
    {
        DependencyObject? current = control;
        FrameworkElement? row = null;
        while (current is not null)
        {
            if (current is Grid grid && grid.Parent is StackPanel)
            {
                row = grid;
                break;
            }
            if (current is DockPanel dockPanel)
            {
                row = dockPanel;
                break;
            }
            current = current is FrameworkElement element ? element.Parent : null;
        }
        if (row is null)
            return;

        var show = visibility == Visibility.Visible;
        var version = _visibilityVersions.TryGetValue(row, out var previousVersion) ? previousVersion + 1 : 1;
        _visibilityVersions[row] = version;
        row.BeginAnimation(FrameworkElement.HeightProperty, null);

        if (!IsLoaded || !SystemParameters.ClientAreaAnimation)
        {
            row.Visibility = visibility;
            row.Height = show ? double.NaN : 0;
            return;
        }

        if (show)
        {
            row.Visibility = Visibility.Visible;
            row.Height = double.NaN;
            row.UpdateLayout();
            var availableWidth = FeaturePanel.ActualWidth > 0 ? FeaturePanel.ActualWidth : 760;
            row.Measure(new Size(availableWidth, double.PositiveInfinity));
            var target = Math.Max(1, row.DesiredSize.Height);
            row.Height = 0;
            var animation = new DoubleAnimation(0, target, TimeSpan.FromMilliseconds(160))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            animation.Completed += (_, _) =>
            {
                if (!_visibilityVersions.TryGetValue(row, out var currentVersion) || currentVersion != version)
                    return;
                row.BeginAnimation(FrameworkElement.HeightProperty, null);
                row.Height = double.NaN;
            };
            row.BeginAnimation(FrameworkElement.HeightProperty, animation);
            return;
        }

        row.Visibility = Visibility.Visible;
        row.Height = double.NaN;
        row.UpdateLayout();
        var currentHeight = Math.Max(row.ActualHeight, row.DesiredSize.Height);
        if (currentHeight <= 0)
        {
            row.Visibility = Visibility.Collapsed;
            row.Height = 0;
            return;
        }

        row.Height = currentHeight;
        var collapseAnimation = new DoubleAnimation(currentHeight, 0, TimeSpan.FromMilliseconds(140))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        collapseAnimation.Completed += (_, _) =>
        {
            if (!_visibilityVersions.TryGetValue(row, out var currentVersion) || currentVersion != version)
                return;
            row.BeginAnimation(FrameworkElement.HeightProperty, null);
            row.Visibility = Visibility.Collapsed;
            row.Height = 0;
        };
        row.BeginAnimation(FrameworkElement.HeightProperty, collapseAnimation);
    }
    private void UpdateSaveState() {
        var hasName = !string.IsNullOrWhiteSpace(NameBox.Text);
        var hasFeature = _normalize.IsChecked == true || _pageSize.IsChecked == true || _flatten.IsChecked == true || _watermark.IsChecked == true || _noise.IsChecked == true || _texture.IsChecked == true || _geometry.IsChecked == true || _microText.IsChecked == true || _copyLayer.IsChecked == true || _hiddenAi.IsChecked == true || _promptInjection.IsChecked == true || _metadata.IsChecked == true || _permissions.IsChecked == true;
        SaveButton.IsEnabled = hasName && (!_isNew || hasFeature) && (_copyLayer.IsChecked != true || _copyLayerMode.SelectedIndex != 3 || !string.IsNullOrWhiteSpace(_copyLayerCustomText.Text)) && (_hiddenAi.IsChecked != true || _selectedPromptTemplateIds.Count > 0);
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
        var width = double.TryParse(_width.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : double.NaN;
        if (_normalize.IsChecked != true && !double.IsFinite(width)) width = 210;
        var pageWidth = double.TryParse(_pageWidth.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedPageWidth) ? parsedPageWidth : double.NaN;
        var pageHeight = double.TryParse(_pageHeight.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedPageHeight) ? parsedPageHeight : double.NaN;
        if (_pageSize.IsChecked != true)
        {
            pageWidth = 210;
            pageHeight = 297;
        }
        var settings = _settings with
        {
            NormalizeContentWidth = _normalize.IsChecked == true,
            TargetWidthMillimeters = width,
            ContentWidthPreset = _contentWidthPreset.SelectedItem is PageSizePreset widthPreset ? widthPreset : PageSizePreset.A4,
            NormalizePageSize = _pageSize.IsChecked == true,
            PageSize = _pageSizePreset.SelectedItem is PageSizePreset pageSize ? pageSize : PageSizePreset.A4,
            PageWidthMillimeters = pageWidth,
            PageHeightMillimeters = pageHeight,
            Protection = _flatten.IsChecked == true ? ProtectionMethod.Flatten : ProtectionMethod.None,
            Dpi = _dpi.SelectedItem is int dpi ? dpi : 300,
            ImageQuality = (ImageQuality)Math.Clamp(_imageQuality.SelectedIndex, 0, 3),
            WatermarkAngle = _watermarkAngle.SelectedItem is double angle ? angle : -45,
            Watermark = _watermark.IsChecked == true ? (_watermarkType.SelectedIndex == 1 ? WatermarkStyle.Repeated : WatermarkStyle.Single) : WatermarkStyle.None,
            WatermarkText = _watermarkText.Text ?? string.Empty,
            Noise = _noise.IsChecked == true ? (NoiseStrength)Math.Clamp(_noiseLevel.SelectedIndex + 1, 1, 3) : NoiseStrength.Off,
            Texture = _texture.IsChecked == true ? (TextureStrength)Math.Clamp(_textureLevel.SelectedIndex + 1, 1, 3) : TextureStrength.Off,
            GeometryPerturbation = _geometry.IsChecked == true,
            MicroTextPattern = _microText.IsChecked == true,
            CopyLayer = _copyLayer.IsChecked == true ? (CopyLayerMode)Math.Clamp(_copyLayerMode.SelectedIndex + 1, 1, 4) : CopyLayerMode.Preserve,
            CopyLayerCustomText = _copyLayerCustomText.Text ?? string.Empty,
            HiddenAiText = _hiddenAi.IsChecked == true,
            PromptTemplateIds = _selectedPromptTemplateIds.ToArray(),
            PromptInjection = _promptInjection.IsChecked == true,
            PromptInjectionText = _promptInjectionText.Text ?? string.Empty,
            MetadataAuthorName = _metadataAuthor.Text ?? string.Empty,
            MetadataOrganization = _metadataOrganization.Text ?? string.Empty,
            MetadataCopyright = _metadataCopyright.Text ?? string.Empty,
            PermissionAllowPrinting = _allowPrinting.IsChecked == true,
            PermissionAllowCopying = _allowCopying.IsChecked == true,
            PermissionAllowModification = _allowModification.IsChecked == true,
            PermissionAllowAnnotations = _allowAnnotations.IsChecked == true,
            PermissionAllowFormFilling = _allowForms.IsChecked == true,
            GanAdversarialPerturbation = false,
            MetadataNormalization = _metadata.IsChecked == true,
            ProtectionDeclaration = _metadata.IsChecked == true,
            PermissionRestriction = _permissions.IsChecked == true
        };
        var errors = SettingsValidator.Validate(settings);
        if (errors.Count > 0)
        {
            ErrorText.Text = string.Join("\n", errors.Select(UiText.Get));
            return;
        }
        _save(_source, _isNew, NameBox.Text.Trim(), settings, _selectedColorKey, _selectedIconKey);
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _parentBack();
}
