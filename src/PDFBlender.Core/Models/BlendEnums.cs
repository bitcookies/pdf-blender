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
