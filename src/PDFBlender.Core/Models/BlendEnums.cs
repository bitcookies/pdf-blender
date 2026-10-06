namespace PDFBlender.Core.Models;

public enum BlendMode
{
    Stir,
    Blend,
    Pulse,
    Crush,
    Overblend,
    Custom
}

public enum ProtectionMethod
{
    None,
    Flatten,
    TextScrambling,
    PermissionRestriction
}

public enum NoiseStrength
{
    Off,
    Low,
    Medium,
    High
}

public enum WatermarkStyle
{
    None,
    Text,
    FullPage
}

public enum CopyLayerMode
{
    Preserve,
    UnicodeScramble,
    TextLayoutPerturbation,
    ReplaceWithGibberish
}

public enum PageSizePreset
{
    Original,
    A5,
    A4,
    A3,
    A2,
    Letter,
    Legal,
    Custom
}

public enum TextureStrength
{
    Off,
    Low,
    Medium
}
