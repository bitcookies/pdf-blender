namespace PDFBlender.Core.Models;

public static class SettingsValidator
{
    public static IReadOnlyList<string> Validate(BlendSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var errors = new List<string>();
        if (settings.NormalizeContentWidth && settings.NormalizePageSize) errors.Add("validationLayoutExclusive");
        if (!Enum.IsDefined(settings.Protection) || settings.Protection is not (ProtectionMethod.None or ProtectionMethod.Flatten) || !Enum.IsDefined(settings.Watermark) || !Enum.IsDefined(settings.Noise)
            || !Enum.IsDefined(settings.Texture) || !Enum.IsDefined(settings.CopyLayer)) errors.Add("validationMode");
        if (settings.NormalizeContentWidth && (!Enum.IsDefined(settings.ContentWidthPreset) || settings.ContentWidthPreset == PageSizePreset.Original
            || !double.IsFinite(settings.TargetWidthMillimeters) || settings.TargetWidthMillimeters is < 10 or > 2000)) errors.Add("validationWidth");
        if (settings.NormalizePageSize && (!Enum.IsDefined(settings.PageSize) || settings.PageSize == PageSizePreset.Original)) errors.Add("validationPaper");
        if (settings.NormalizePageSize && settings.PageSize == PageSizePreset.Custom &&
            (!double.IsFinite(settings.PageWidthMillimeters) || !double.IsFinite(settings.PageHeightMillimeters) || settings.PageWidthMillimeters is < 10 or > 2000 || settings.PageHeightMillimeters is < 10 or > 2000)) errors.Add("validationPaperSize");
        var raster = settings.Protection == ProtectionMethod.Flatten;
        if (raster && settings.Dpi is < 72 or > 600) errors.Add("validationDpi");
        if (raster && !Enum.IsDefined(settings.ImageQuality)) errors.Add("validationQuality");
        if (settings.Watermark != WatermarkStyle.None)
        {
            if (string.IsNullOrWhiteSpace(settings.WatermarkText) || settings.WatermarkText.Length > 2000) errors.Add("validationWatermarkText");
            if (!double.IsFinite(settings.WatermarkAngle) || settings.WatermarkAngle is < -180 or > 180) errors.Add("validationAngle");
        }
        if ((settings.CopyLayer != CopyLayerMode.Preserve || settings.Noise != NoiseStrength.Off || settings.Texture != TextureStrength.Off || settings.GeometryPerturbation || settings.MicroTextPattern) && !raster) errors.Add("validationFlattenRequired");
        if (settings.CopyLayer == CopyLayerMode.CustomWarning && (string.IsNullOrWhiteSpace(settings.CopyLayerCustomText) || settings.CopyLayerCustomText.Length > 4000)) errors.Add("validationCopyWarning");
        if (settings.HiddenAiText && !settings.PromptInjection && (settings.PromptTemplateIds is null || settings.PromptTemplateIds.Length == 0)) errors.Add("validationPromptTemplates");
        if (settings.PromptInjection && (string.IsNullOrWhiteSpace(settings.PromptInjectionText) || settings.PromptInjectionText.Length > 4000)) errors.Add("validationPrompt");
        if (settings.MetadataAuthorName.Length > 200 || settings.MetadataOrganization.Length > 200 || settings.MetadataCopyright.Length > 500) errors.Add("validationMetadata");
        if (settings.GanAdversarialPerturbation) errors.Add("validationGanUnsupported");
        return errors;
    }
}
