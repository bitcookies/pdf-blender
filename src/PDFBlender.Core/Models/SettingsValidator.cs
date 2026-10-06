namespace PDFBlender.Core.Models;

public static class SettingsValidator
{
    public static IReadOnlyList<string> Validate(BlendSettings settings)
    {
        var errors = new List<string>();
        if (settings.TargetWidthMillimeters is < 10 or > 2000) errors.Add("Target content width must be between 10 and 2000 millimeters.");
        if (settings.CustomPageWidthMillimeters is < 10 or > 2000) errors.Add("Custom page width must be between 10 and 2000 millimeters.");
        if (settings.Dpi is < 72 or > 600) errors.Add("DPI must be between 72 and 600.");
        if (settings.Watermark != WatermarkStyle.None && string.IsNullOrWhiteSpace(settings.WatermarkText)) errors.Add("Watermark text cannot be empty when watermarking is enabled.");
        var raster = settings.Protection == ProtectionMethod.Flatten;
        if (settings.CopyLayer == CopyLayerMode.ReplaceWithGibberish && !raster) errors.Add("The gibberish copy layer requires page image flattening.");
        if ((settings.Noise != NoiseStrength.Off || settings.Texture != TextureStrength.Off || settings.GeometryPerturbation || settings.MicroTextPattern) && !raster) errors.Add("Noise, texture, geometry perturbation, and micro patterns require page image flattening.");
        if (settings.PermissionRestriction && !raster) errors.Add("Permission restrictions require page image flattening.");
        if (settings.Protection == ProtectionMethod.TextScrambling) errors.Add("Use a copy layer mode instead of the legacy text scrambling option.");
        return errors;
    }
}
