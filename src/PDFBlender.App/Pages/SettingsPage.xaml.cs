using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PDFBlender.Core.Models;

namespace PDFBlender.App.Pages;

public partial class SettingsPage : Page
{
    private readonly AppSession _session;
    private bool _initializing;

    internal SettingsPage(AppSession session)
    {
        _session = session;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        _initializing = true;
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("settings");
        SubtitleText.Text = UiText.Get("settingsSubtitle");
        AppearanceHeader.Text = UiText.Get("appearance");
        ThemeLabel.Text = UiText.Get("theme");
        ThemeHint.Text = UiText.Get("themeHint");
        LanguageLabel.Text = UiText.Get("language");
        LanguageHint.Text = UiText.Get("languageHint");
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
        OutputHeader.Text = UiText.Get("output");
        OutputHintText.Text = UiText.Get("outputHint");

        ThemeOptions.ItemsSource = new[] { UiText.Get("system"), UiText.Get("light"), UiText.Get("dark") };
        LanguageOptions.ItemsSource = new[] { UiText.Get("system"), "English", "简体中文" };
        ProtectionCombo.ItemsSource = new[] { UiText.Get("none"), UiText.Get("flatten") };
        WatermarkCombo.ItemsSource = new[] { UiText.Get("none"), UiText.Get("text"), UiText.Get("fullPage") };
        NoiseCombo.ItemsSource = new[] { UiText.Get("off"), UiText.Get("low"), UiText.Get("medium"), UiText.Get("high") };

        ThemeOptions.SelectedIndex = (int)_session.Preferences.Theme;
        LanguageOptions.SelectedIndex = (int)_session.Preferences.Language;
        NormalizeWidthToggle.IsChecked = _session.Settings.NormalizeContentWidth;
        TargetWidthText.Text = _session.Settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        ProtectionCombo.SelectedIndex = (int)_session.Settings.Protection;
        WatermarkCombo.SelectedIndex = (int)_session.Settings.Watermark;
        WatermarkTextBox.Text = _session.Settings.WatermarkText;
        NoiseCombo.SelectedIndex = (int)_session.Settings.Noise;
        _initializing = false;
    }

    private void ThemeOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && ThemeOptions.SelectedIndex >= 0)
            _session.ApplyPreferences(theme: (ThemePreference)ThemeOptions.SelectedIndex);
    }

    private void LanguageOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_initializing && LanguageOptions.SelectedIndex >= 0)
            _session.ApplyPreferences(language: (UiLanguage)LanguageOptions.SelectedIndex);
    }

    private void SettingsControl_Changed(object sender, RoutedEventArgs e)
    {
        if (_initializing) return;
        if (!double.TryParse(TargetWidthText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var width)) return;

        var candidate = _session.Settings with
        {
            NormalizeContentWidth = NormalizeWidthToggle.IsChecked == true,
            TargetWidthMillimeters = width,
            Protection = (ProtectionMethod)Math.Clamp(ProtectionCombo.SelectedIndex, 0, 1),
            Watermark = (WatermarkStyle)Math.Clamp(WatermarkCombo.SelectedIndex, 0, 2),
            WatermarkText = WatermarkTextBox.Text,
            Noise = (NoiseStrength)Math.Clamp(NoiseCombo.SelectedIndex, 0, 3)
        };
        if (SettingsValidator.Validate(candidate).Count == 0)
            _session.ApplySettings(candidate, notify: false);
    }
}
