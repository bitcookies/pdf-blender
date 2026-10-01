namespace PDFBlender.App;

internal sealed record ToastSoundOption(string FileName, string TextKey);

internal static class ToastSoundCatalog
{
    public static IReadOnlyList<ToastSoundOption> Options { get; } =
    [
        new("toast-1.mp3", "sound1"),
        new("toast-2.mp3", "sound2"),
        new("toast-3.mp3", "sound3"),
        new("toast-4.mp3", "sound4"),
        new("toast-5.mp3", "sound5")
    ];

    public static bool IsKnown(string? fileName) => Options.Any(option =>
        string.Equals(option.FileName, fileName, StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? fileName) => Options.FirstOrDefault(option =>
        string.Equals(option.FileName, fileName, StringComparison.OrdinalIgnoreCase))?.FileName
        ?? Options[0].FileName;
}
