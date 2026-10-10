using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFtoImage;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using SkiaSharp;

namespace PDFBlender.Pdf;

internal static class RasterPageBuilder
{
    public static void Build(PdfPage page, byte[] input, int index, PageLayout layout, BlendSettings settings, CancellationToken token)
    {
        var pixels = layout.Pixels(settings.Dpi);
        // Rendering uses source dimensions. Bound this allocation independently of output.
        _ = PageLayout.Create(layout.SourceWidth, layout.SourceHeight, BlendSettings.Default).Pixels(settings.Dpi);
        using var png = new MemoryStream();
        Conversion.SavePng(png, input, index, null, new RenderOptions(settings.Dpi, UseTiling: true));
        token.ThrowIfCancellationRequested();
        png.Position = 0;
        using var source = SKBitmap.Decode(png) ?? throw new PdfProcessingException("pdfInvalid");
        using var bitmap = new SKBitmap(new SKImageInfo(pixels.Width, pixels.Height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(SKColors.White);
            var pixelScale = settings.Dpi / 72d;
            var rect = new SKRect((float)(layout.OffsetX * pixelScale), (float)(layout.OffsetY * pixelScale),
                (float)((layout.OffsetX + layout.SourceWidth * layout.Scale) * pixelScale), (float)((layout.OffsetY + layout.SourceHeight * layout.Scale) * pixelScale));
            canvas.DrawBitmap(source, rect, new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear));
        }
        Effects(bitmap, settings, token);
        token.ThrowIfCancellationRequested();
        using var image = SKImage.FromBitmap(bitmap);
        var format = settings.ImageQuality == ImageQuality.Lossless ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg;
        var quality = settings.ImageQuality switch { ImageQuality.Compact => 65, ImageQuality.Standard => 85, _ => 95 };
        using var encoded = image.Encode(format, quality) ?? throw new PdfProcessingException("pdfFailed");
        using var imageStream = new MemoryStream(encoded.ToArray());
        using var pdfImage = XImage.FromStream(imageStream);
        using var graphics = XGraphics.FromPdfPage(page);
        graphics.DrawImage(pdfImage, 0, 0, layout.Width, layout.Height);
    }

    private static void Effects(SKBitmap bitmap, BlendSettings settings, CancellationToken token)
    {
        if (settings.Noise == NoiseStrength.Off && settings.Texture == TextureStrength.Off && !settings.GeometryPerturbation) return;
        var pixels = bitmap.Pixels;
        var amplitude = settings.Noise switch { NoiseStrength.Low => 2, NoiseStrength.Medium => 4, NoiseStrength.High => 6, _ => 0 };
        var random = new Random(System.Security.Cryptography.RandomNumberGenerator.GetInt32(int.MaxValue));
        for (var y = 0; y < bitmap.Height; y++)
        {
            token.ThrowIfCancellationRequested();
            for (var x = 0; x < bitmap.Width; x++)
            {
                var i = y * bitmap.Width + x;
                var delta = amplitude == 0 ? 0 : random.Next(-amplitude, amplitude + 1);
                if (settings.Texture != TextureStrength.Off)
                    delta += (int)Math.Round(Math.Sin(x * .11) * Math.Sin(y * .09) * (settings.Texture == TextureStrength.Medium ? 3 : 1.5));
                var p = pixels[i];
                pixels[i] = new SKColor((byte)Math.Clamp(p.Red + delta, 0, 255), (byte)Math.Clamp(p.Green + delta, 0, 255), (byte)Math.Clamp(p.Blue + delta, 0, 255));
            }
        }
        if (settings.GeometryPerturbation)
        {
            var row = new SKColor[bitmap.Width];
            // One-pixel local shift; edge pixels stay in place and no page crop occurs.
            for (var y = 2; y < bitmap.Height - 2; y++)
            {
                token.ThrowIfCancellationRequested();
                var shift = (int)Math.Round(Math.Sin(y * .035));
                if (shift == 0) continue;
                Array.Copy(pixels, y * bitmap.Width, row, 0, bitmap.Width);
                for (var x = 2; x < bitmap.Width - 2; x++) pixels[y * bitmap.Width + x] = row[x + shift];
            }
        }
        bitmap.Pixels = pixels;
    }
}
