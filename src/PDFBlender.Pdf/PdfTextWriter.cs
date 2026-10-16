using System.Globalization;
using System.Text;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;

namespace PDFBlender.Pdf;

// PDFsharp 6.2.4 writes supplementary ToUnicode destinations as UTF-32 hex.
// Collect only text we generate and replace affected maps with PDF UTF-16BE maps.
internal sealed class PdfTextWriter(PdfDocument document)
{
    private readonly Dictionary<(string Family, XFontStyleEx Style), FontMap> _fonts = new();

    public void DrawString(PdfPage page, XGraphics graphics, string text, XFont font, XBrush brush, XPoint point)
    {
        graphics.DrawString(text, font, brush, point);
        if (text.Length == 0) return;
        var key = (font.FontFamily.Name, font.Style);
        if (!_fonts.TryGetValue(key, out var map))
        {
            // Read the actual resource selected by DrawString; font subset names are not stable.
            var content = graphics.Internals.ContentStringBuilder ?? throw new InvalidDataException("Missing generated text stream.");
            var end = content.Length - 1;
            while (end >= 2 && !(content[end] == 'f' && content[end - 1] == 'T' && char.IsWhiteSpace(content[end - 2]))) end--;
            if (end < 2) throw new InvalidDataException("Missing generated font selection.");
            var start = end - 2;
            while (start >= 0 && content[start] != '/') start--;
            if (start < 0) throw new InvalidDataException("Missing generated font resource.");
            var nameEnd = start + 1;
            while (nameEnd < end && !char.IsWhiteSpace(content[nameEnd])) nameEnd++;
            var name = content.ToString(start, nameEnd - start);
            var resources = page.Resources.Elements.GetDictionary("/Font");
            var dictionary = resources?.Elements.GetDictionary(name) ?? throw new InvalidDataException("Missing generated font dictionary.");
            map = new FontMap(dictionary);
            _fonts.Add(key, map);
        }
        foreach (var pair in GlyphHelper.GlyphIndicesFromString(text, font))
        {
            map.Characters.TryAdd(pair.GlyphIndex, pair.CodePoint);
            map.HasSupplementaryCharacters |= pair.CodePoint > char.MaxValue;
        }
    }

    public void Complete()
    {
        foreach (var map in _fonts.Values.Where(map => map.HasSupplementaryCharacters))
        {
            var content = new StringBuilder("/CIDInit /ProcSet findresource begin\n12 dict begin\nbegincmap\n/CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def\n/CMapName /Blender-UCS def\n/CMapType 2 def\n1 begincodespacerange\n<0000> <FFFF>\nendcodespacerange\n");
            foreach (var batch in map.Characters.OrderBy(pair => pair.Key).Chunk(100))
            {
                content.Append(batch.Length.ToString(CultureInfo.InvariantCulture)).Append(" beginbfchar\n");
                foreach (var pair in batch)
                {
                    var unicode = Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(char.ConvertFromUtf32(pair.Value)));
                    content.Append('<').Append(pair.Key.ToString("X4", CultureInfo.InvariantCulture)).Append("> <").Append(unicode).Append(">\n");
                }
                content.Append("endbfchar\n");
            }
            content.Append("endcmap\nCMapName currentdict /CMap defineresource pop\nend\nend\n");
            var unicodeMap = new PdfDictionary(document);
            unicodeMap.CreateStream(Encoding.ASCII.GetBytes(content.ToString()));
            document.Internals.AddObject(unicodeMap);
            map.Font.Elements["/ToUnicode"] = unicodeMap.Reference!;
        }
    }

    private sealed class FontMap(PdfDictionary font)
    {
        public PdfDictionary Font { get; } = font;
        public Dictionary<ushort, int> Characters { get; } = new();
        public bool HasSupplementaryCharacters { get; set; }
    }
}
