namespace PDFBlender.Core.Models;

public static class SettingsValidator
{
    public static IReadOnlyList<string> Validate(BlendSettings settings)
    {
        var errors = new List<string>();

        if (settings.TargetWidthMillimeters is < 10 or > 2000)
        {
            errors.Add("Target content width must be between 10 and 2000 millimeters.");
        }

        if (settings.Dpi is < 72 or > 600)
        {
            errors.Add("DPI must be between 72 and 600.");
        }

        if (settings.Watermark != WatermarkStyle.None && string.IsNullOrWhiteSpace(settings.WatermarkText))
        {
            errors.Add("Watermark text cannot be empty when watermarking is enabled.");
        }

        if (settings.Protection is ProtectionMethod.TextScrambling or ProtectionMethod.PermissionRestriction)
        {
            errors.Add("This protection method is reserved for a future release.");
        }

        return errors;
    }
}
