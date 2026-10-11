namespace PDFBlender.Core.Models;

public enum ProtectionMethod
{
    None,
    Flatten
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
    Custom,
    A6,
    A1,
    A0,
    B5,
    B4,
    B3,
    B2,
    B1,
    B0,
    C5,
    C4,
    Tabloid,
    Executive
}

public enum TextureStrength
{
    Off,
    Low,
    Medium
}

public enum ImageQuality
{
    Compact,
    Standard,
    High,
    Lossless
}
