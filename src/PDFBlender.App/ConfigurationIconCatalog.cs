using Wpf.Ui.Controls;

namespace PDFBlender.App;

internal readonly record struct ConfigurationIconDescriptor(
    string Key,
    SymbolRegular Symbol,
    bool Filled)
{
    public string Name => Symbol.ToString();
}

internal static class ConfigurationIconCatalog
{
    private const string RegularPrefix = "regular:";
    private const string FilledPrefix = "filled:";

    public static IReadOnlyList<string> RegularKeys { get; } =
        Enum.GetNames<SymbolRegular>()
            .Where(name => !string.Equals(name, nameof(SymbolRegular.Empty), StringComparison.Ordinal))
            .ToArray();

    public static IReadOnlyList<string> FilledKeys { get; } =
        Enum.GetNames<SymbolFilled>()
            .Where(name => !string.Equals(name, nameof(SymbolFilled.Empty), StringComparison.Ordinal))
            .ToArray();

    public static IReadOnlyList<string> Keys => RegularKeys;

    public static string Normalize(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return CreateKey(RegularKeys[0], false);

        if (TryParse(key, out var descriptor))
            return descriptor.Key;

        return CreateKey(RegularKeys.Contains(key, StringComparer.Ordinal) ? key : RegularKeys[0], false);
    }

    public static string ForIndex(int index) => Normalize(RegularKeys[Math.Abs(index) % RegularKeys.Count]);

    public static SymbolRegular GetSymbol(string? key) => GetDescriptor(key).Symbol;

    public static bool IsFilled(string? key) => GetDescriptor(key).Filled;

    public static ConfigurationIconDescriptor GetDescriptor(string? key)
    {
        if (TryParse(key, out var descriptor))
            return descriptor;

        return new ConfigurationIconDescriptor(
            CreateKey(RegularKeys[0], false),
            Enum.Parse<SymbolRegular>(RegularKeys[0]),
            false);
    }

    public static IEnumerable<ConfigurationIconDescriptor> Search(string? query, bool filled)
    {
        var names = filled ? FilledKeys : RegularKeys;
        var text = query?.Trim() ?? string.Empty;

        foreach (var name in names)
        {
            if (text.Length > 0 && name.IndexOf(text, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            if (Enum.TryParse<SymbolRegular>(name, out var regular))
                yield return new ConfigurationIconDescriptor(CreateKey(name, filled), regular, filled);
        }
    }

    private static string CreateKey(string name, bool filled) =>
        (filled ? FilledPrefix : RegularPrefix) + name;

    private static bool TryParse(string? key, out ConfigurationIconDescriptor descriptor)
    {
        var rawKey = key!;
        var separator = rawKey.IndexOf(':');
        var filled = separator > 0 && rawKey[..separator].Equals("filled", StringComparison.OrdinalIgnoreCase);
        var name = separator > 0 ? rawKey[(separator + 1)..] : rawKey;

        if (!Enum.TryParse<SymbolRegular>(name, out var symbol))
        {
            descriptor = default;
            return false;
        }

        descriptor = new ConfigurationIconDescriptor(CreateKey(name, filled), symbol, filled);
        return true;
    }
}
