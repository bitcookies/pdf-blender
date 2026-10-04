using System.Collections.ObjectModel;

namespace PDFBlender.Core.Models;

public enum BlendFeature
{
    NormalizeContentWidth,
    Flatten,
    Watermark,
    PixelNoise
}

public static class BlendFeatureCatalog
{
    public static IReadOnlyList<BlendFeature> All { get; } =
    [
        BlendFeature.NormalizeContentWidth,
        BlendFeature.Flatten,
        BlendFeature.Watermark,
        BlendFeature.PixelNoise
    ];

    public static bool IsEnabled(BlendSettings settings, BlendFeature feature) => feature switch
    {
        BlendFeature.NormalizeContentWidth => settings.NormalizeContentWidth,
        BlendFeature.Flatten => settings.Protection == ProtectionMethod.Flatten,
        BlendFeature.Watermark => settings.Watermark != WatermarkStyle.None,
        BlendFeature.PixelNoise => settings.Noise != NoiseStrength.Off,
        _ => false
    };

    public static int Weight(BlendSettings settings, BlendFeature feature) => feature switch
    {
        BlendFeature.NormalizeContentWidth when settings.NormalizeContentWidth => 0,
        BlendFeature.Flatten when settings.Protection == ProtectionMethod.Flatten => 6,
        BlendFeature.Watermark when settings.Watermark == WatermarkStyle.Text => 3,
        BlendFeature.Watermark when settings.Watermark == WatermarkStyle.FullPage => 5,
        BlendFeature.PixelNoise when settings.Noise == NoiseStrength.Low => 2,
        BlendFeature.PixelNoise when settings.Noise == NoiseStrength.Medium => 4,
        BlendFeature.PixelNoise when settings.Noise == NoiseStrength.High => 6,
        _ => 0
    };
}

public static class ConfigurationIntensity
{
    public static int For(BlendSettings settings) =>
        BlendFeatureCatalog.All.Max(feature => BlendFeatureCatalog.Weight(settings, feature));

    public static string LabelKey(int intensity) => intensity switch
    {
        <= 0 => "intensityNone",
        <= 2 => "intensityLight",
        <= 4 => "intensityModerate",
        <= 6 => "intensityStrong",
        _ => "intensityVeryStrong"
    };
}

public sealed record BlendConfiguration(
    string Id,
    string Name,
    bool IsBuiltIn,
    BlendSettings Settings,
    bool IsLocked = false,
    string? ShortId = null,
    string? ColorKey = null)
{
    public int Intensity => ConfigurationIntensity.For(Settings);

    public IReadOnlyList<BlendFeature> EnabledFeatures =>
        new ReadOnlyCollection<BlendFeature>(BlendFeatureCatalog.All.Where(feature => BlendFeatureCatalog.IsEnabled(Settings, feature)).ToList());
}

public static class BuiltInConfigurations
{
    public static IReadOnlyList<BlendConfiguration> All { get; } =
    [
        new("builtin-light", "Light", true, Presets.For(BlendMode.Stir), true),
        new("builtin-medium", "Medium", true, Presets.For(BlendMode.Blend), true),
        new("builtin-heavy", "Heavy", true, Presets.For(BlendMode.Pulse), true)
    ];
}
