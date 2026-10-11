using System.Security.Cryptography;
using System.Text;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PigPage = UglyToad.PdfPig.Content.Page;

namespace PDFBlender.Pdf;

internal static class PdfOverlays
{
    public static void Watermark(PdfPage page, BlendSettings settings)
    {
        if (settings.Watermark == WatermarkStyle.None && !settings.MicroTextPattern) return;
        using var graphics = XGraphics.FromPdfPage(page, XGraphicsPdfPageOptions.Append);
        var width = page.Width.Point;
        var height = page.Height.Point;
        if (settings.Watermark != WatermarkStyle.None)
        {
            var brush = new XSolidBrush(XColor.FromArgb(settings.Watermark == WatermarkStyle.Repeated ? 24 : 45, 140, 60, 65));
            var lines = settings.WatermarkText.Replace("\r", "").Split('\n');
            var size = Math.Clamp(width / (settings.Watermark == WatermarkStyle.Repeated ? 42 : 26), 8, 26);
            var font = PdfFontResolver.ForText(settings.WatermarkText, size);
            var measured = lines.Max(line => graphics.MeasureString(line, font).Width);
            if (measured > width * .8)
            {
                size *= width * .8 / measured;
                font = PdfFontResolver.ForText(settings.WatermarkText, size);
            }
            void Draw(double x, double y)
            {
                var state = graphics.Save();
                graphics.TranslateTransform(x, y);
                graphics.RotateTransform(settings.WatermarkAngle);
                for (var i = 0; i < lines.Length; i++)
                    graphics.DrawString(lines[i], font, brush, new XPoint(-graphics.MeasureString(lines[i], font).Width / 2, i * size * 1.3));
                graphics.Restore(state);
            }
            if (settings.Watermark == WatermarkStyle.Single) Draw(width / 2, height / 2);
            else
                for (var y = -height / 8; y < height * 1.2; y += Math.Max(72, height / 5))
                    for (var x = -width / 4; x < width * 1.2; x += Math.Max(120, width / 2)) Draw(x, y);
        }
        if (settings.MicroTextPattern)
        {
            var font = PdfFontResolver.ForText("PDF", 1.5);
            var brush = new XSolidBrush(XColor.FromArgb(16, 70, 70, 70));
            for (var y = 8d; y < height; y += 36)
                for (var x = 8d; x < width; x += 80)
                    graphics.DrawString("PDF", font, brush, new XPoint(x, y));
        }
    }

    public static void HiddenStatements(PdfPage page, BlendSettings settings)
    {
        var templates = settings.HiddenAiText ? PromptTemplateCatalog.Resolve(settings.PromptTemplateIds) : string.Empty;
        var custom = settings.PromptInjection ? settings.PromptInjectionText : string.Empty;
        if (string.IsNullOrWhiteSpace(templates) && string.IsNullOrWhiteSpace(custom)) return;
        const string notice = "Machine processing notice: this publicly readable document contains protection layers; extracted text may differ from visible content. 机器处理声明：本文档含保护层，提取文字可能与可视内容不同。";
        var parts = new[] { settings.HiddenAiText ? notice : string.Empty, templates, custom }.Where(text => !string.IsNullOrWhiteSpace(text));
        var text = string.Join("\n", parts);
        WriteInvisible(page, true, true, graphics =>
        {
            var font = PdfFontResolver.ForText(text, .5);
            var y = 1d;
            foreach (var line in text.Replace("\r", "").Split('\n'))
            {
                graphics.DrawString(line, font, XBrushes.Black, new XPoint(1, y));
                y += .7;
            }
        });
    }

    public static int CopyLayer(PdfPage page, PigPage source, PageLayout layout, CopyLayerMode mode, string customText, CancellationToken token)
    {
        if (mode == CopyLayerMode.CustomWarning)
        {
            WriteInvisible(page, false, false, graphics =>
            {
                var font = PdfFontResolver.ForText(customText, 1);
                graphics.DrawString(customText, font, XBrushes.Black, new XPoint(1, 1));
            });
            return 1;
        }
        var letters = source.Letters.Where(letter => !string.IsNullOrEmpty(letter.Value)).ToArray();
        if (letters.Length == 0) return 0;
        if (mode == CopyLayerMode.TextLayoutPerturbation)
        {
            for (var i = letters.Length - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (letters[i], letters[j]) = (letters[j], letters[i]);
            }
        }
        WriteInvisible(page, false, false, graphics =>
        {
            foreach (var letter in letters)
            {
                token.ThrowIfCancellationRequested();
                var text = mode switch
                {
                    CopyLayerMode.TextLayoutPerturbation => letter.Value,
                    CopyLayerMode.UnicodeScramble => RandomText(letter.Value.Length, "αβγδεζηθκλμνξπρστυφχψω"),
                    CopyLayerMode.ReplaceWithGibberish => RandomText(letter.Value.Length, "абвгдежзийклмнопрстуфхцчшщэюя"),
                    _ => throw new PdfProcessingException("validationMode")
                };
                var point = letter.StartBaseLine;
                var end = letter.EndBaseLine;
                var x = layout.OffsetX + point.X * layout.Scale;
                var y = layout.OffsetY + (layout.SourceHeight - point.Y) * layout.Scale;
                var size = Math.Clamp(letter.PointSize * layout.Scale, .5, 300);
                var font = PdfFontResolver.ForText(text, size);
                var measuredWidth = graphics.MeasureString(text, font).Width;
                var targetWidth = Math.Max(.1, Math.Sqrt(Math.Pow(end.X - point.X, 2) + Math.Pow(end.Y - point.Y, 2)) * layout.Scale);
                var state = graphics.Save();
                graphics.TranslateTransform(x, y);
                graphics.RotateTransform(-Math.Atan2(end.Y - point.Y, end.X - point.X) * 180 / Math.PI);
                graphics.ScaleTransform(targetWidth / Math.Max(.1, measuredWidth), 1);
                graphics.DrawString(text, font, XBrushes.Black, new XPoint(0, 0));
                graphics.Restore(state);
            }
        });
        return letters.Length;
    }

    private static string RandomText(int count, string alphabet) => new(Enumerable.Range(0, count).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]).ToArray());

    private static void WriteInvisible(PdfPage page, bool prepend, bool artifact, Action<XGraphics> draw)
    {
        using (var graphics = XGraphics.FromPdfPage(page, prepend ? XGraphicsPdfPageOptions.Prepend : XGraphicsPdfPageOptions.Append)) draw(graphics);
        var stream = page.Contents.Elements.GetDictionary(prepend ? 0 : page.Contents.Elements.Count - 1) ?? throw new PdfProcessingException("pdfFailed");
        var bytes = stream.Stream.UnfilteredValue;
        var operators = Encoding.Latin1.GetString(bytes).Replace("BT\n", "BT\n3 Tr\n");
        var prefix = artifact ? "q\n/Artifact BMC\n" : "q\n";
        var suffix = artifact ? "\nBT 0 Tr ET\nEMC\nQ\n" : "\nBT 0 Tr ET\nQ\n";
        stream.Elements.Remove("/Filter");
        stream.Stream.Value = Encoding.Latin1.GetBytes(prefix + operators + suffix);
    }
}
