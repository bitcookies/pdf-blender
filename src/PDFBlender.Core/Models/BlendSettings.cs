namespace PDFBlender.Core.Models;

public sealed record BlendSettings
{
    public bool NormalizeContentWidth { get; init; }
    public PageSizePreset ContentWidthPreset { get; init; } = PageSizePreset.A4;
    public double TargetWidthMillimeters { get; init; } = 210;
    public bool NormalizePageSize { get; init; }
    public PageSizePreset PageSize { get; init; } = PageSizePreset.A4;
    public ProtectionMethod Protection { get; init; } = ProtectionMethod.None;
    public ImageQuality ImageQuality { get; init; } = ImageQuality.Standard;
    public int Dpi { get; init; } = 300;
    public WatermarkStyle Watermark { get; init; } = WatermarkStyle.None;
    public double WatermarkAngle { get; init; } = -45;
    public string WatermarkText { get; init; } = "DO NOT PLAGIARIZE MY PDF.";
    public NoiseStrength Noise { get; init; } = NoiseStrength.Off;
    public TextureStrength Texture { get; init; } = TextureStrength.Off;
    public bool GeometryPerturbation { get; init; }
    public bool MicroTextPattern { get; init; }
    public CopyLayerMode CopyLayer { get; init; } = CopyLayerMode.Preserve;
    public bool HiddenAiText { get; init; }
    public bool PromptInjection { get; init; }
    public string PromptInjectionText { get; init; } = string.Empty;
    public bool GanAdversarialPerturbation { get; init; }
    public bool MetadataNormalization { get; init; }
    public bool ProtectionDeclaration { get; init; }
    public bool PermissionRestriction { get; init; }

    public static BlendSettings Default => new();
}
