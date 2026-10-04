using Wpf.Ui.Controls;

namespace PDFBlender.App;

internal static class ConfigurationIconCatalog
{
    private static readonly IReadOnlyList<SymbolRegular> Values =
    [
        SymbolRegular.DocumentMultiple16,
        SymbolRegular.DocumentSettings20,
        SymbolRegular.DocumentTable16,
        SymbolRegular.BookOpen20,
        SymbolRegular.BoxToolbox24,
        SymbolRegular.ClipboardText32,
        SymbolRegular.DocumentFlowchart20,
        SymbolRegular.DocumentRibbon20
    ];

    public static IReadOnlyList<string> Keys { get; } = Values.Select(value => value.ToString()).ToArray();

    public static string Normalize(string? key) =>
        !string.IsNullOrWhiteSpace(key) && Enum.TryParse<SymbolRegular>(key, out var symbol) && Values.Contains(symbol)
            ? symbol.ToString()
            : Values[0].ToString();

    public static SymbolRegular GetSymbol(string? key) =>
        Enum.TryParse<SymbolRegular>(Normalize(key), out var symbol) ? symbol : Values[0];

    public static string ForIndex(int index) => Keys[Math.Abs(index) % Keys.Count];
}
