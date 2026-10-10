using System.Collections.ObjectModel;

namespace PDFBlender.Core.Models;

public enum BlendFeature
{
    NormalizeContentWidth,
    NormalizePageSize,
    Flatten,
    Watermark,
    PixelNoise,
    Texture,
    GeometryPerturbation,
    MicroTextPattern,
    UnicodeScramble,
    TextLayoutPerturbation,
    GibberishCopyLayer,
    HiddenAiText, PromptInjection, GanAdversarialPerturbation, MetadataNormalization,
    PermissionRestriction
}

public static class BlendFeatureCatalog
{
    public static IReadOnlyList<BlendFeature> All { get; } =
    [
        BlendFeature.NormalizeContentWidth, BlendFeature.NormalizePageSize, BlendFeature.Flatten,
        BlendFeature.Watermark, BlendFeature.PixelNoise, BlendFeature.Texture,
        BlendFeature.GeometryPerturbation, BlendFeature.MicroTextPattern,
        BlendFeature.UnicodeScramble, BlendFeature.TextLayoutPerturbation,
        BlendFeature.GibberishCopyLayer, BlendFeature.HiddenAiText, BlendFeature.PromptInjection, BlendFeature.GanAdversarialPerturbation, BlendFeature.MetadataNormalization, BlendFeature.PermissionRestriction
    ];

    public static bool IsEnabled(BlendSettings settings, BlendFeature feature) => feature switch
    {
        BlendFeature.NormalizeContentWidth => settings.NormalizeContentWidth,
        BlendFeature.NormalizePageSize => settings.NormalizePageSize,
        BlendFeature.Flatten => settings.Protection == ProtectionMethod.Flatten,
        BlendFeature.Watermark => settings.Watermark != WatermarkStyle.None,
        BlendFeature.PixelNoise => settings.Noise != NoiseStrength.Off,
        BlendFeature.Texture => settings.Texture != TextureStrength.Off,
        BlendFeature.GeometryPerturbation => settings.GeometryPerturbation,
        BlendFeature.MicroTextPattern => settings.MicroTextPattern,
        BlendFeature.UnicodeScramble => settings.CopyLayer == CopyLayerMode.UnicodeScramble,
        BlendFeature.TextLayoutPerturbation => settings.CopyLayer == CopyLayerMode.TextLayoutPerturbation,
        BlendFeature.GibberishCopyLayer => settings.CopyLayer == CopyLayerMode.ReplaceWithGibberish,
        BlendFeature.HiddenAiText => settings.HiddenAiText, BlendFeature.PromptInjection => settings.PromptInjection, BlendFeature.GanAdversarialPerturbation => settings.GanAdversarialPerturbation, BlendFeature.MetadataNormalization => settings.MetadataNormalization || settings.ProtectionDeclaration,
        BlendFeature.PermissionRestriction => settings.PermissionRestriction,
        _ => false
    };

    public static int Weight(BlendSettings settings, BlendFeature feature) => feature switch
    {
        BlendFeature.NormalizeContentWidth when settings.NormalizeContentWidth => 0,
        BlendFeature.NormalizePageSize when settings.NormalizePageSize => 0,
        BlendFeature.Flatten when settings.Protection == ProtectionMethod.Flatten => 6,
        BlendFeature.Watermark when settings.Watermark == WatermarkStyle.Text => 2,
        BlendFeature.Watermark when settings.Watermark == WatermarkStyle.FullPage => 4,
        BlendFeature.PixelNoise when settings.Noise == NoiseStrength.Low => 2,
        BlendFeature.PixelNoise when settings.Noise == NoiseStrength.Medium => 4,
        BlendFeature.PixelNoise when settings.Noise == NoiseStrength.High => 6,
        BlendFeature.Texture when settings.Texture == TextureStrength.Low => 2,
        BlendFeature.Texture when settings.Texture == TextureStrength.Medium => 4,
        BlendFeature.GeometryPerturbation when settings.GeometryPerturbation => 5,
        BlendFeature.MicroTextPattern when settings.MicroTextPattern => 3,
        BlendFeature.UnicodeScramble when settings.CopyLayer == CopyLayerMode.UnicodeScramble => 4,
        BlendFeature.TextLayoutPerturbation when settings.CopyLayer == CopyLayerMode.TextLayoutPerturbation => 5,
        BlendFeature.GibberishCopyLayer when settings.CopyLayer == CopyLayerMode.ReplaceWithGibberish => 7,
        BlendFeature.HiddenAiText when settings.HiddenAiText => 7, BlendFeature.PromptInjection when settings.PromptInjection => 7, BlendFeature.GanAdversarialPerturbation when settings.GanAdversarialPerturbation => 8, BlendFeature.PermissionRestriction when settings.PermissionRestriction => 1,
        _ => 0
    };
}

public static class ConfigurationIntensity
{
    public static int For(BlendSettings settings) => BlendFeatureCatalog.All.Max(feature => BlendFeatureCatalog.Weight(settings, feature));
}

public sealed record BlendConfiguration(
    string Id, string Name, bool IsBuiltIn, BlendSettings Settings,
    bool IsLocked = false, string? ShortId = null, string? ColorKey = null, string? IconKey = null)
{
    public int Intensity => ConfigurationIntensity.For(Settings);
    public IReadOnlyList<BlendFeature> EnabledFeatures =>
        new ReadOnlyCollection<BlendFeature>(BlendFeatureCatalog.All.Where(f => BlendFeatureCatalog.IsEnabled(Settings, f)).ToList());
}

public static class BuiltInConfigurations
{
    public static IReadOnlyList<BlendConfiguration> All { get; } =
    [
        new("format-only", "Format Only", true, new BlendSettings { NormalizeContentWidth = true }, true),
        new("light-mark", "Light Mark", true, new BlendSettings { NormalizeContentWidth = true, Watermark = WatermarkStyle.Text }, true),
        new("reading-first", "Reading First", true, new BlendSettings { NormalizeContentWidth = true, Watermark = WatermarkStyle.FullPage, MetadataNormalization = true, ProtectionDeclaration = true }, true),
        new("balanced-protection", "Balanced Protection", true, new BlendSettings { NormalizeContentWidth = true, Protection = ProtectionMethod.Flatten, Watermark = WatermarkStyle.Text, Noise = NoiseStrength.Low, PermissionRestriction = true }, true),
        new("copy-layer-shield", "Copy Layer Shield", true, new BlendSettings { NormalizeContentWidth = true, Protection = ProtectionMethod.Flatten, CopyLayer = CopyLayerMode.ReplaceWithGibberish, ImageQuality = ImageQuality.High }, true)
    ];

    public static IReadOnlySet<string> RetiredIds { get; } =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "visual-obstruction",
            "ai-processing-resistance",
            "custom"
        };

    public static bool IsReservedId(string? id) =>
        id is not null &&
        (All.Any(preset => string.Equals(preset.Id, id, StringComparison.OrdinalIgnoreCase)) ||
         RetiredIds.Contains(id));
}
