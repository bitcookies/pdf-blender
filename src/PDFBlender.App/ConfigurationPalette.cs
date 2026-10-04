using System.Windows.Media;

namespace PDFBlender.App;

internal static class ConfigurationPalette
{
    private static readonly IReadOnlyDictionary<string, (string Light, string Dark)> Colors =
        new Dictionary<string, (string Light, string Dark)>(StringComparer.OrdinalIgnoreCase)
        {
            ["coral"] = ("#C9021E", "#F06A73"),
            ["orange"] = ("#D97706", "#F5A043"),
            ["amber"] = ("#B7791F", "#E7B65B"),
            ["green"] = ("#2F855A", "#67C58B"),
            ["teal"] = ("#0F766E", "#4FC1B6"),
            ["blue"] = ("#2563A6", "#76B5E8"),
            ["purple"] = ("#6B46C1", "#B49AEF"),
            ["slate"] = ("#64748B", "#A9B4C4")
        };

    public static IReadOnlyList<string> Keys { get; } = Colors.Keys.ToArray();

    public static string Normalize(string? key) =>
        !string.IsNullOrWhiteSpace(key) && Colors.ContainsKey(key) ? key.ToLowerInvariant() : "coral";

    public static string ForIndex(int index) => Keys[Math.Abs(index) % Keys.Count];

    public static Brush GetBrush(string? key)
    {
        var pair = Colors[Normalize(key)];
        var value = ThemeManager.IsDark ? pair.Dark : pair.Light;
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(value)!);
        brush.Freeze();
        return brush;
    }

    public static Color GetColor(string? key)
    {
        var pair = Colors[Normalize(key)];
        var value = ThemeManager.IsDark ? pair.Dark : pair.Light;
        return (Color)ColorConverter.ConvertFromString(value)!;
    }
}