using System.Text;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace PDFBlender.Pdf;

internal readonly record struct PageLayout(double SourceWidth, double SourceHeight, double Width, double Height, double Scale, double OffsetX, double OffsetY)
{
    public const double PointsPerMillimeter = 72d / 25.4;

    public static PageLayout Create(double width, double height, BlendSettings settings)
    {
        if (!double.IsFinite(width) || !double.IsFinite(height) || width <= 0 || height <= 0)
            throw new PdfProcessingException("pdfInvalid");
        var targetWidth = width;
        var targetHeight = height;
        if (settings.NormalizeContentWidth)
        {
            targetWidth = settings.TargetWidthMillimeters * PointsPerMillimeter;
            targetHeight = height * targetWidth / width;
        }
        else if (settings.NormalizePageSize)
        {
            var mm = settings.PageSize switch
            {
                PageSizePreset.A5 => (148d, 210d),
                PageSizePreset.A4 => (210d, 297d),
                PageSizePreset.A3 => (297d, 420d),
                PageSizePreset.A2 => (420d, 594d),
                PageSizePreset.Letter => (215.9d, 279.4d),
                PageSizePreset.Legal => (215.9d, 355.6d),
                _ => throw new PdfProcessingException("validationPaper")
            };
            if (width > height) mm = (mm.Item2, mm.Item1);
            targetWidth = mm.Item1 * PointsPerMillimeter;
            targetHeight = mm.Item2 * PointsPerMillimeter;
        }
        var scale = Math.Min(targetWidth / width, targetHeight / height);
        return new(width, height, targetWidth, targetHeight, scale, (targetWidth - width * scale) / 2, (targetHeight - height * scale) / 2);
    }

    public (int Width, int Height) Pixels(int dpi)
    {
        var w = Math.Ceiling(Width * dpi / 72d);
        var h = Math.Ceiling(Height * dpi / 72d);
        if (!double.IsFinite(w * h) || w * h > 40_000_000 || w > int.MaxValue || h > int.MaxValue)
            throw new PdfProcessingException("pdfPageSize");
        return (Math.Max(1, (int)w), Math.Max(1, (int)h));
    }
}

internal readonly record struct PageTransform(double A, double B, double C, double D, double E, double F)
{
    public XPoint Map(double x, double y) => new(A * x + C * y + E, B * x + D * y + F);
    public string Operator => FormattableString.Invariant($"{A:0.########} {B:0.########} {C:0.########} {D:0.########} {E:0.########} {F:0.########} cm\n");
}

internal static class NativePageLayout
{
    public static void Apply(PdfPage page, PageLayout layout)
    {
        var box = page.CropBox;
        if (box.Width <= 0 || box.Height <= 0) box = page.MediaBox;
        var rotation = ((page.Rotate % 360) + 360) % 360;
        var s = layout.Scale;
        var x = layout.OffsetX;
        var y = layout.OffsetY;
        var transform = rotation switch
        {
            0 => new PageTransform(s, 0, 0, s, x - box.X1 * s, y - box.Y1 * s),
            90 => new PageTransform(0, -s, s, 0, x - box.Y1 * s, y + box.X2 * s),
            180 => new PageTransform(-s, 0, 0, -s, x + box.X2 * s, y + box.Y2 * s),
            270 => new PageTransform(0, s, -s, 0, x + box.Y2 * s, y - box.X1 * s),
            _ => throw new PdfProcessingException("pdfAnnotations")
        };
        if (page.Elements.GetReal("/UserUnit", false) is var unit && unit != 0 && unit != 1)
            throw new PdfProcessingException("pdfAnnotations");
        TransformAnnotations(page, transform, s);
        var prefix = FormattableString.Invariant($"q\n{x:0.########} {y:0.########} {layout.SourceWidth * s:0.########} {layout.SourceHeight * s:0.########} re W n\n") + transform.Operator;
        page.Contents.PrependContent().CreateStream(Encoding.ASCII.GetBytes(prefix));
        page.Contents.AppendContent().CreateStream("\nQ\n"u8.ToArray());
        page.Rotate = 0;
        page.MediaBox = new PdfRectangle(new XRect(0, 0, layout.Width, layout.Height));
        page.CropBox = page.MediaBox;
        foreach (var key in new[] { "/TrimBox", "/BleedBox", "/ArtBox" })
            if (page.Elements.ContainsKey(key)) page.Elements.SetRectangle(key, page.MediaBox);
    }



    private static void TransformAnnotations(PdfPage page, PageTransform transform, double scale)
    {
        for (var index = 0; index < page.Annotations.Count; index++)
        {
            var annotation = page.Annotations[index];
            var kind = annotation.Elements.GetName("/Subtype");
            var action = annotation.Elements.GetDictionary("/A");
            if (kind is not ("/Link" or "/Highlight" or "/Underline" or "/StrikeOut" or "/Squiggly")
                || annotation.Elements.ContainsKey("/AP") || annotation.Elements.ContainsKey("/Dest")
                || action is not null && action.Elements.GetName("/S") != "/URI")
                throw new PdfProcessingException("pdfAnnotations");
            var rectangle = annotation.Rectangle;
            var points = new[] { transform.Map(rectangle.X1, rectangle.Y1), transform.Map(rectangle.X1, rectangle.Y2), transform.Map(rectangle.X2, rectangle.Y1), transform.Map(rectangle.X2, rectangle.Y2) };
            annotation.Rectangle = new PdfRectangle(new XPoint(points.Min(p => p.X), points.Min(p => p.Y)), new XPoint(points.Max(p => p.X), points.Max(p => p.Y)));
            var quads = annotation.Elements.GetArray("/QuadPoints");
            if (quads is not null)
            {
                if (quads.Elements.Count % 2 != 0) throw new PdfProcessingException("pdfAnnotations");
                for (var i = 0; i < quads.Elements.Count; i += 2)
                {
                    var point = transform.Map(quads.Elements.GetReal(i), quads.Elements.GetReal(i + 1));
                    quads.Elements[i] = new PdfReal(point.X);
                    quads.Elements[i + 1] = new PdfReal(point.Y);
                }
            }
            var border = annotation.Elements.GetArray("/Border");
            if (border is not null && border.Elements.Count >= 3)
                border.Elements[2] = new PdfReal(border.Elements.GetReal(2) * scale);
        }
    }
}
