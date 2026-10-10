using PDFBlender.Core.Processing;
using System.Buffers.Binary;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace PDFBlender.Pdf;

// Resolve installed Windows fonts locally; no font files are redistributed.
internal sealed class PdfFontResolver : IFontResolver
{
    private static readonly object Gate = new();
    public static void EnsureInitialized()
    {
        lock (Gate)
            GlobalFontSettings.FontResolver ??= new PdfFontResolver();
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool bold, bool italic) =>
        new(familyName == "BlenderCjk" ? "simhei.ttf" : "arial.ttf");

    public byte[] GetFont(string faceName)
    {
        var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), faceName);
        if (!File.Exists(path)) throw new PdfProcessingException("pdfFont");
        var bytes = File.ReadAllBytes(path);
        CheckEmbeddingPermissions(bytes);
        return bytes;
    }

    private static void CheckEmbeddingPermissions(byte[] font)
    {
        if (font.Length < 12) throw new PdfProcessingException("pdfFont");
        var count = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(4, 2));
        for (var i = 0; i < count; i++)
        {
            var entry = 12 + i * 16;
            if (entry + 16 > font.Length) throw new PdfProcessingException("pdfFont");
            if (!font.AsSpan(entry, 4).SequenceEqual("OS/2"u8)) continue;
            var offset = BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(entry + 8, 4));
            if (offset > int.MaxValue || offset + 10 > font.Length) throw new PdfProcessingException("pdfFont");
            var restrictions = BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan((int)offset + 8, 2));
            // Restricted embedding, no-subsetting, and bitmap-only fonts cannot be used by this subset writer.
            if ((restrictions & (0x0002 | 0x0100 | 0x0200)) != 0) throw new PdfProcessingException("pdfFont");
            return;
        }
    }

    public static XFont ForText(string text, double size)
    {
        EnsureInitialized();
        var visible = string.Concat(text.Where(c => !char.IsControl(c)));
        var families = text.Any(c => c >= 0x2e80)
            ? new[] { "BlenderCjk", "BlenderLatin" }
            : new[] { "BlenderLatin", "BlenderCjk" };
        try
        {
            foreach (var family in families)
            {
                var font = new XFont(family, size, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
                if (GlyphHelper.GlyphIndicesFromString(visible, font).All(glyph => glyph.GlyphIndex != 0)) return font;
            }
        }
        catch (PdfProcessingException) { throw; }
        catch (Exception ex) when (ex is not OutOfMemoryException and not OperationCanceledException)
        {
            throw new PdfProcessingException("pdfFont", ex);
        }
        throw new PdfProcessingException("pdfFont");
    }
}
