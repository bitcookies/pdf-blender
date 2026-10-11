namespace PDFBlender.Core.Models;

public sealed record BlendSettings
{
    public bool NormalizeContentWidth { get; init; }
    public PageSizePreset ContentWidthPreset { get; init; } = PageSizePreset.A4;
    public double TargetWidthMillimeters { get; init; } = 210;
    public bool NormalizePageSize { get; init; }
    public PageSizePreset PageSize { get; init; } = PageSizePreset.A4;
    public double PageWidthMillimeters { get; init; } = 210;
    public double PageHeightMillimeters { get; init; } = 297;
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
    public string CopyLayerCustomText { get; init; } = string.Empty;
    public bool HiddenAiText { get; init; }
    public string[] PromptTemplateIds { get; init; } = Array.Empty<string>();
    public bool PromptInjection { get; init; }
    public string PromptInjectionText { get; init; } = string.Empty;
    public bool GanAdversarialPerturbation { get; init; }
    public bool MetadataNormalization { get; init; }
    public bool ProtectionDeclaration { get; init; }
    public string MetadataAuthorName { get; init; } = string.Empty;
    public string MetadataOrganization { get; init; } = string.Empty;
    public string MetadataCopyright { get; init; } = string.Empty;
    public bool PermissionRestriction { get; init; }
    public bool PermissionAllowPrinting { get; init; } = true;
    public bool PermissionAllowCopying { get; init; }
    public bool PermissionAllowModification { get; init; }
    public bool PermissionAllowAnnotations { get; init; }
    public bool PermissionAllowFormFilling { get; init; }

    public bool Equals(BlendSettings? other)
    {
        if (ReferenceEquals(this, other)) return true;
        if (other is null) return false;
        return NormalizeContentWidth == other.NormalizeContentWidth && ContentWidthPreset == other.ContentWidthPreset && TargetWidthMillimeters == other.TargetWidthMillimeters
            && NormalizePageSize == other.NormalizePageSize && PageSize == other.PageSize && PageWidthMillimeters == other.PageWidthMillimeters && PageHeightMillimeters == other.PageHeightMillimeters
            && Protection == other.Protection && ImageQuality == other.ImageQuality && Dpi == other.Dpi && Watermark == other.Watermark && WatermarkAngle == other.WatermarkAngle
            && WatermarkText == other.WatermarkText && Noise == other.Noise && Texture == other.Texture && GeometryPerturbation == other.GeometryPerturbation && MicroTextPattern == other.MicroTextPattern
            && CopyLayer == other.CopyLayer && CopyLayerCustomText == other.CopyLayerCustomText && PromptInjection == other.PromptInjection && HiddenAiText == other.HiddenAiText
            && PromptTemplateIds.SequenceEqual(other.PromptTemplateIds) && PromptInjectionText == other.PromptInjectionText && GanAdversarialPerturbation == other.GanAdversarialPerturbation
            && MetadataNormalization == other.MetadataNormalization && ProtectionDeclaration == other.ProtectionDeclaration && MetadataAuthorName == other.MetadataAuthorName
            && MetadataOrganization == other.MetadataOrganization && MetadataCopyright == other.MetadataCopyright && PermissionRestriction == other.PermissionRestriction
            && PermissionAllowPrinting == other.PermissionAllowPrinting && PermissionAllowCopying == other.PermissionAllowCopying && PermissionAllowModification == other.PermissionAllowModification
            && PermissionAllowAnnotations == other.PermissionAllowAnnotations && PermissionAllowFormFilling == other.PermissionAllowFormFilling;
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(NormalizeContentWidth); hash.Add(ContentWidthPreset); hash.Add(TargetWidthMillimeters); hash.Add(NormalizePageSize); hash.Add(PageSize);
        hash.Add(PageWidthMillimeters); hash.Add(PageHeightMillimeters); hash.Add(Protection); hash.Add(ImageQuality); hash.Add(Dpi); hash.Add(Watermark); hash.Add(WatermarkAngle);
        hash.Add(WatermarkText); hash.Add(Noise); hash.Add(Texture); hash.Add(GeometryPerturbation); hash.Add(MicroTextPattern); hash.Add(CopyLayer); hash.Add(CopyLayerCustomText);
        foreach (var id in PromptTemplateIds) hash.Add(id); hash.Add(HiddenAiText); hash.Add(PromptInjection); hash.Add(PromptInjectionText); hash.Add(GanAdversarialPerturbation);
        hash.Add(MetadataNormalization); hash.Add(ProtectionDeclaration); hash.Add(MetadataAuthorName); hash.Add(MetadataOrganization); hash.Add(MetadataCopyright); hash.Add(PermissionRestriction);
        hash.Add(PermissionAllowPrinting); hash.Add(PermissionAllowCopying); hash.Add(PermissionAllowModification); hash.Add(PermissionAllowAnnotations); hash.Add(PermissionAllowFormFilling);
        return hash.ToHashCode();
    }
    public static BlendSettings Default => new();
}
