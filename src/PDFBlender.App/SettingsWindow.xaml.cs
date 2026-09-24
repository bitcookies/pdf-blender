using System.Globalization;
using System.Windows;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

public partial class SettingsWindow : Window
{
    public BlendSettings Settings { get; private set; }
    public AppPreferences Preferences { get; private set; }

    public SettingsWindow(BlendSettings settings, AppPreferences preferences)
    {
        InitializeComponent();
        Settings = settings;
        Preferences = preferences;
        InitializeControls();
    }

    private void InitializeControls()
    {
        Title = UiText.Get("advanced");
        TitleText.Text = UiText.Get("advanced");
        SubtitleText.Text = UiText.Get("settingsSubtitle");
        AppearanceHeader.Text = UiText.Get("appearance");
        ThemeLabel.Text = UiText.Get("theme");
        LanguageLabel.Text = UiText.Get("language");
        FormatHeader.Text = UiText.Get("format");
        ProtectionHeader.Text = UiText.Get("protection");
        WatermarkHeader.Text = UiText.Get("watermark");
        ImageHeader.Text = UiText.Get("image");
        OutputHeader.Text = UiText.Get("output");
        NormalizeWidthCheck.Content = UiText.Get("normalizeWidth");
        TargetWidthLabel.Text = UiText.Get("targetWidth");
        TargetWidthHint.Text = UiText.Get("targetWidthHint");
        WatermarkTextLabel.Text = UiText.Get("watermarkText");
        NoiseLabel.Text = UiText.Get("noise");
        OutputHintText.Text = UiText.Get("outputHint");
        CancelButton.Content = UiText.Get("close");
        SaveButton.Content = UiText.Get("save");

        ThemeOptions.Items.Add(UiText.Get("system"));
        ThemeOptions.Items.Add(UiText.Get("light"));
        ThemeOptions.Items.Add(UiText.Get("dark"));
        ThemeOptions.SelectedIndex = (int)Preferences.Theme;

        LanguageOptions.Items.Add(UiText.Get("system"));
        LanguageOptions.Items.Add("English");
        LanguageOptions.Items.Add("简体中文");
        LanguageOptions.SelectedIndex = (int)Preferences.Language;

        ProtectionCombo.Items.Add(UiText.Get("none"));
        ProtectionCombo.Items.Add(UiText.Get("flatten"));
        WatermarkCombo.Items.Add(UiText.Get("none"));
        WatermarkCombo.Items.Add(UiText.Get("text"));
        WatermarkCombo.Items.Add(UiText.Get("fullPage"));
        NoiseCombo.Items.Add(UiText.Get("off"));
        NoiseCombo.Items.Add(UiText.Get("low"));
        NoiseCombo.Items.Add(UiText.Get("medium"));
        NoiseCombo.Items.Add(UiText.Get("high"));

        NormalizeWidthCheck.IsChecked = Settings.NormalizeContentWidth;
        TargetWidthText.Text = Settings.TargetWidthMillimeters.ToString("0.##", CultureInfo.InvariantCulture);
        ProtectionCombo.SelectedIndex = (int)Settings.Protection;
        WatermarkCombo.SelectedIndex = (int)Settings.Watermark;
        WatermarkTextBox.Text = Settings.WatermarkText;
        NoiseCombo.SelectedIndex = (int)Settings.Noise;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (!double.TryParse(TargetWidthText.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var width))
        {
            ErrorText.Text = UiText.Get("invalidSettings");
            return;
        }

        var candidate = Settings with
        {
            NormalizeContentWidth = NormalizeWidthCheck.IsChecked == true,
            TargetWidthMillimeters = width,
            Protection = (ProtectionMethod)Math.Max(0, ProtectionCombo.SelectedIndex),
            Watermark = (WatermarkStyle)Math.Max(0, WatermarkCombo.SelectedIndex),
            WatermarkText = WatermarkTextBox.Text,
            Noise = (NoiseStrength)Math.Max(0, NoiseCombo.SelectedIndex)
        };
        if (SettingsValidator.Validate(candidate).Count > 0)
        {
            ErrorText.Text = UiText.Get("invalidSettings");
            return;
        }

        Settings = candidate;
        Preferences = Preferences with
        {
            Theme = (ThemePreference)Math.Clamp(ThemeOptions.SelectedIndex, 0, 2),
            Language = (UiLanguage)Math.Clamp(LanguageOptions.SelectedIndex, 0, 2)
        };
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
