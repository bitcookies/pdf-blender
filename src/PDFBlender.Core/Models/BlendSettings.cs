namespace PDFBlender.Core.Models;

public sealed record BlendSettings
{
    public bool NormalizeContentWidth { get; init; } = true;
    public double TargetWidthMillimeters { get; init; } = 210;
    public ProtectionMethod Protection { get; init; } = ProtectionMethod.Flatten;
    public int Dpi { get; init; } = 150;
    public WatermarkStyle Watermark { get; init; } = WatermarkStyle.Text;
    public string WatermarkText { get; init; } = "DO NOT PLAGIARIZE MY PDF.";
    public NoiseStrength Noise { get; init; } = NoiseStrength.Low;

    public static BlendSettings Default => Presets.For(BlendMode.Blend);
}
