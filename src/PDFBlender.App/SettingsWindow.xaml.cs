using System.Globalization;
using System.Windows;
using PDFBlender.Core.Models;

namespace PDFBlender.App;

public partial class SettingsWindow : Window
{
    public BlendSettings Settings { get; private set; }

    public SettingsWindow(BlendSettings settings)
    {
        InitializeComponent();
        Settings = settings;
        InitializeControls();
    }

    private void InitializeControls()
    {
        Title = UiText.Get("advanced");
        TitleText.Text = UiText.Get("advanced");
        NormalizeWidthCheck.Content = UiText.Get("normalizeWidth");
        TargetWidthLabel.Text = UiText.Get("targetWidth");
        ProtectionLabel.Text = UiText.Get("protection");
        WatermarkLabel.Text = UiText.Get("watermark");
        WatermarkTextLabel.Text = UiText.Get("watermarkText");
        NoiseLabel.Text = UiText.Get("noise");
        CancelButton.Content = UiText.Get("close");
        SaveButton.Content = UiText.Get("save");

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
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
