namespace PDFBlender.Core.Models;

public sealed record BlendPreset(BlendMode Mode, string DisplayName, string Summary, BlendSettings Settings);

public static class Presets
{
    public static IReadOnlyList<BlendPreset> All { get; } =
    [
        new(BlendMode.Stir, "Format Only", "Page layout normalization", BuiltInConfigurations.All[0].Settings),
        new(BlendMode.Blend, "Balanced Protection", "Readable protection for everyday sharing", BuiltInConfigurations.All[3].Settings),
        new(BlendMode.Pulse, "AI Processing Resistance", "Image, gibberish copy layer, and hidden text", BuiltInConfigurations.All[4].Settings),
        new(BlendMode.Crush, "Visual Obstruction", "Stronger visual extraction resistance", BuiltInConfigurations.All[3].Settings),
        new(BlendMode.Overblend, "Copy Layer Shield", "Readable image with a non-original copy layer", BuiltInConfigurations.All[4].Settings)
    ];

    public static BlendSettings For(BlendMode mode) => All.FirstOrDefault(p => p.Mode == mode)?.Settings ?? BlendSettings.Default;
}
