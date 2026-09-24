namespace PDFBlender.Core.Models;

public sealed record BlendPreset(BlendMode Mode, string DisplayName, string Summary, BlendSettings Settings);

public static class Presets
{
    public static IReadOnlyList<BlendPreset> All { get; } =
    [
        new(BlendMode.Stir, "STIR", "Light formatting touch", new BlendSettings
        {
            NormalizeContentWidth = true,
            Protection = ProtectionMethod.None,
            Dpi = 150,
            Watermark = WatermarkStyle.None,
            Noise = NoiseStrength.Off
        }),
        new(BlendMode.Blend, "BLEND", "Balanced everyday blend", new BlendSettings
        {
            NormalizeContentWidth = true,
            Protection = ProtectionMethod.Flatten,
            Dpi = 150,
            Watermark = WatermarkStyle.Text,
            Noise = NoiseStrength.Low
        }),
        new(BlendMode.Pulse, "PULSE", "Stronger visual signature", new BlendSettings
        {
            NormalizeContentWidth = true,
            Protection = ProtectionMethod.Flatten,
            Dpi = 120,
            Watermark = WatermarkStyle.FullPage,
            Noise = NoiseStrength.Medium
        })
    ];

    public static BlendSettings For(BlendMode mode) =>
        All.FirstOrDefault(p => p.Mode == mode)?.Settings ?? BlendSettings.Default;
}
