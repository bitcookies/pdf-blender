using System.Windows.Media;

namespace PDFBlender.App;

internal static class ConfigurationPalette
{
    private static readonly IReadOnlyList<(string Key, string Light, string Dark)> Entries =
    [
        ("coral", "#EF493E", "#F47A72"),
        ("brick", "#C7382C", "#E16458"),
        ("scarlet", "#EE3536", "#F56666"),
        ("magenta", "#D91B5D", "#E95A88"),
        ("pink", "#EC145B", "#F05E91"),
        ("rose", "#EA3F7A", "#F276A1"),
        ("fuchsia", "#FF3B81", "#FF74A5"),
        ("lilac", "#9B59B6", "#B883C9"),
        ("violet", "#8E44AD", "#B06BC6"),
        ("indigo", "#673AB7", "#9670D2"),
        ("sky", "#3498DB", "#67B3E5"),
        ("ocean", "#2980B9", "#5B9DCA"),
        ("azure", "#2196F3", "#62A9F5"),
        ("cyan", "#03A9F4", "#53BFF5"),
        ("turquoise", "#12B6C6", "#55CAD4"),
        ("teal", "#009688", "#4EB9AE"),
        ("mint", "#1ABC9C", "#59C9B1"),
        ("jade", "#16A085", "#4DBBA4"),
        ("green", "#2ECC71", "#65D794"),
        ("emerald", "#27AE60", "#5ACB8B"),
        ("leaf", "#4CAF50", "#75C77A"),
        ("lime", "#8BC34A", "#A9D47A"),
        ("yellow", "#F1C40F", "#F6D65C"),
        ("amber", "#F39C12", "#F6B94D"),
        ("orange", "#FF9800", "#FFB84D"),
        ("tangerine", "#FF7A3D", "#FF9C70"),
        ("tomato", "#FF5126", "#FF8060"),
        ("brown", "#795548", "#A47F71"),
        ("terracotta", "#A8532E", "#C98261"),
        ("gray", "#9E9E9E", "#BDBDBD"),
        ("blueGray", "#607D8B", "#8DA8B5"),
        ("slate", "#34495E", "#668098"),
    ];

    private static readonly IReadOnlyDictionary<string, string> LegacyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["coral"] = "coral",
            ["orange"] = "orange",
            ["amber"] = "amber",
            ["green"] = "green",
            ["teal"] = "teal",
            ["blue"] = "sky",
            ["purple"] = "violet",
            ["slate"] = "slate"
        };

    public static IReadOnlyList<string> Keys { get; } = Entries.Select(entry => entry.Key).ToArray();

    public static string Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return Entries[0].Key;

        if (Entries.Any(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)))
            return Entries.First(entry => string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase)).Key;

        return LegacyAliases.TryGetValue(key, out var alias) ? alias : Entries[0].Key;
    }

    public static string ForIndex(int index) => Entries[Math.Abs(index) % Entries.Count].Key;

    public static string ForPresetIndex(int index) => new[] { "coral", "sky", "mint", "orange", "violet" }[Math.Abs(index) % 5];

    private static readonly string[] IntensityScale =
    [
        "gray", "green", "emerald", "leaf", "mint", "teal",
        "cyan", "sky", "azure", "ocean", "blueGray", "slate",
        "yellow", "amber", "orange", "tangerine", "tomato", "terracotta",
        "scarlet", "brick", "coral", "rose", "pink", "magenta",
        "fuchsia", "lilac", "violet", "indigo", "jade", "lime", "brown", "gray"
    ];

    public static string ForIntensity(int intensity)
    {
        if (intensity <= 0) return "gray";

        // Keep light protection green, move medium protection through blue and warm colors,
        // and reserve red tones for the strongest combinations.
        var position = intensity switch
        {
            <= 5 => 1 + (intensity - 1) * 4 / 4,
            <= 12 => 6 + (intensity - 6) * 5 / 7,
            <= 21 => 12 + (intensity - 13) * 6 / 9,
            <= 35 => 18 + (intensity - 22) * 5 / 14,
            _ => 24 + Math.Min(7, (intensity - 36) * 7 / 27)
        };
        return IntensityScale[Math.Clamp(position, 0, IntensityScale.Length - 1)];
    }
    public static Brush GetBrush(string? key) => CreateBrush(GetColor(key));

    public static Color GetColor(string? key)
    {
        var normalized = Normalize(key);
        var entry = Entries.First(item => string.Equals(item.Key, normalized, StringComparison.OrdinalIgnoreCase));
        var value = ThemeManager.IsDark ? entry.Dark : entry.Light;
        return (Color)ColorConverter.ConvertFromString(value)!;
    }

    private static Brush CreateBrush(Color color)
    {
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }
}
