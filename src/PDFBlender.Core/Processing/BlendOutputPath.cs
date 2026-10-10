namespace PDFBlender.Core.Processing;

/// <summary>Names output PDFs after the selected configuration without allowing path components.</summary>
public static class BlendOutputPath
{
    public static string Create(string inputPath, string configurationName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(inputPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationName);

        const string invalidCharacters = "<>:\"/\\|?*";
        var suffix = new string(configurationName.Trim().Select(character =>
            char.IsControl(character) || invalidCharacters.Contains(character) ? '_' : character).ToArray())
            .TrimEnd(' ', '.');
        if (suffix.Length == 0)
            suffix = "configuration";

        return Path.Combine(
            Path.GetDirectoryName(inputPath) ?? string.Empty,
            $"{Path.GetFileNameWithoutExtension(inputPath)}_{suffix}.pdf");
    }
}
