using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PDFBlender.Core.Models;

namespace PDFBlender.App.Pages;

public partial class ConfigurationPage : Page
{
    private readonly AppSession _session;
    private bool _initializing;

    internal ConfigurationPage(AppSession session)
    {
        _session = session;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        _initializing = true;
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("configuration");
        SubtitleText.Text = UiText.Get("configurationSubtitle");
        ConfigurationNameLabel.Text = UiText.Get("configurationName");
        ConfigurationNameHint.Text = UiText.Get("configurationNameHint");
        LevelLabel.Text = UiText.Get("blendLevel");
        LevelHint.Text = UiText.Get("configurationLevelHint");
        FormatHeader.Text = UiText.Get("format");
        NormalizeLabel.Text = UiText.Get("normalizeWidth");
        NormalizeHint.Text = UiText.Get("normalizeHint");
        TargetWidthLabel.Text = UiText.Get("targetWidth");
        TargetWidthHint.Text = UiText.Get("targetWidthHint");
        ProtectionHeader.Text = UiText.Get("protection");
        ProtectionLabel.Text = UiText.Get("protectionMethod");
        ProtectionHint.Text = UiText.Get("protectionHint");
        WatermarkHeader.Text = UiText.Get("watermark");
        WatermarkTypeLabel.Text = UiText.Get("watermarkType");
        WatermarkTypeHint.Text = UiText.Get("watermarkTypeHint");
        WatermarkTextLabel.Text = UiText.Get("watermarkText");
        ImageHeader.Text = UiText.Get("image");
        NoiseLabel.Text = UiText.Get("noise");
        NoiseHint.Text = UiText.Get("noiseHint");

        ConfigurationCombo.ItemsSource = _session.Configurations.Select(ConfigurationDisplayName).ToList();
        ConfigurationCombo.SelectedIndex = Math.Clamp(_session.SelectedConfigurationIndex, 0, Math.Max(0, _session.Configurations.Count - 1));
        LevelCombo.ItemsSource = _session.Presets.Select(p => p.DisplayName).ToList();
        LevelCombo.SelectedIndex = _session.SelectedBlendLevel;
        ProtectionCombo.ItemsSource = new[] { UiText.Get("none"), UiText.Get("flatten") };
        WatermarkCombo.ItemsSource = new[] { UiText.Get("none"), UiText.Get("text"), UiText.Get("fullPage") };
        NoiseCombo.ItemsSource = new[] { UiText.Get("off"), UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") };

        NormalizeWidthToggle.IsChecked = _session.Settings.NormalizeContentWidth;
        TargetWidthText.Text = _session.Settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        ProtectionCombo.SelectedIndex = Math.Clamp((int)_session.Settings.Protection, 0, 1);
        WatermarkCombo.SelectedIndex = Math.Clamp((int)_session.Settings.Watermark, 0, 2);
        WatermarkTextBox.Text = _session.Settings.WatermarkText;
        NoiseCombo.SelectedIndex = Math.Clamp((int)_session.Settings.Noise, 0, 3);
        ErrorText.Text = string.Empty;
        _initializing = false;
    }

    private void ConfigurationCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || ConfigurationCombo.SelectedIndex < 0) return;
        _session.SelectConfiguration(ConfigurationCombo.SelectedIndex);
    }

    private void LevelCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_initializing || LevelCombo.SelectedIndex < 0) return;
        _session.SetBlendLevel(LevelCombo.SelectedIndex);
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        if (!double.TryParse(TargetWidthText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var width))
        {
            ErrorText.Text = UiText.Get("invalidSettings");
            return;
        }

        var candidate = _session.Settings with
        {
            NormalizeContentWidth = NormalizeWidthToggle.IsChecked == true,
            TargetWidthMillimeters = width,
            Protection = (ProtectionMethod)Math.Clamp(ProtectionCombo.SelectedIndex, 0, 1),
            Watermark = (WatermarkStyle)Math.Clamp(WatermarkCombo.SelectedIndex, 0, 2),
            WatermarkText = WatermarkTextBox.Text,
            Noise = (NoiseStrength)Math.Clamp(NoiseCombo.SelectedIndex, 0, 3)
        };
        var errors = SettingsValidator.Validate(candidate);
        ErrorText.Text = errors.Count == 0 ? string.Empty : string.Join(" ", errors);
        if (errors.Count == 0) _session.ApplySettings(candidate, notify: false);
    }

    private static string ConfigurationDisplayName(ProfileDefinition profile) => profile.Id switch
    {
        "research" => UiText.Get("profileResearch"),
        "public" => UiText.Get("profilePublic"),
        _ => UiText.Get("profileDefault")
    };
}
