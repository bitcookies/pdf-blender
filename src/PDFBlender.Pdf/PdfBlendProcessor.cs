using PDFtoImage;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using SkiaSharp;

namespace PDFBlender.Pdf;

public sealed class PdfBlendProcessor : IBlendProcessor
{
    private const double PointsPerMillimeter = 72d / 25.4d;

    public async Task<BlendResult> ProcessAsync(
        BlendRequest request,
        IProgress<BlendProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var errors = SettingsValidator.Validate(request.Settings);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), nameof(request));
        }

        if (!File.Exists(request.InputPath))
        {
            throw new FileNotFoundException("The selected PDF could not be found.", request.InputPath);
        }

        var inputFullPath = Path.GetFullPath(request.InputPath);
        var outputFullPath = Path.GetFullPath(request.OutputPath);
        if (string.Equals(inputFullPath, outputFullPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The output path must be different from the source PDF.");
        }

        var outputDirectory = Path.GetDirectoryName(outputFullPath)
            ?? throw new InvalidOperationException("The output folder is invalid.");
        Directory.CreateDirectory(outputDirectory);

        var inputBytes = await File.ReadAllBytesAsync(inputFullPath, cancellationToken).ConfigureAwait(false);
        var pageCount = Conversion.GetPageCount(inputBytes, null);
        if (pageCount <= 0)
        {
            throw new InvalidDataException("The PDF does not contain any pages.");
        }

        var temporaryOutput = Path.Combine(outputDirectory, $".{Path.GetFileName(outputFullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await Task.Run(() => ProcessDocument(inputBytes, pageCount, inputFullPath, temporaryOutput, request.Settings, progress, cancellationToken), cancellationToken)
                .ConfigureAwait(false);
            File.Move(temporaryOutput, outputFullPath, overwrite: true);
            return new BlendResult(outputFullPath, pageCount);
        }
        finally
        {
            TryDelete(temporaryOutput);
        }
    }

    private static void ProcessDocument(
        byte[] inputBytes,
        int pageCount,
        string inputPath,
        string outputPath,
        BlendSettings settings,
        IProgress<BlendProgress>? progress,
        CancellationToken cancellationToken)
    {
        var settingsRequireRaster = settings.NormalizeContentWidth
            || settings.Protection == ProtectionMethod.Flatten
            || settings.Watermark != WatermarkStyle.None
            || settings.Noise != NoiseStrength.Off;
        if (!settingsRequireRaster)
        {
            File.Copy(inputPath, outputPath, overwrite: true);
            progress?.Report(new BlendProgress(pageCount, pageCount, "Copied"));
            return;
        }

        using var output = File.Create(outputPath);
        using var document = SKDocument.CreatePdf(output);
        var pageSizes = Conversion.GetPageSizes(inputBytes, null);

        for (var pageIndex = 0; pageIndex < pageCount; pageIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            progress?.Report(new BlendProgress(pageIndex, pageCount, "Rendering"));

            var sourceSize = pageSizes[pageIndex];
            var renderOptions = new RenderOptions(settings.Dpi, UseTiling: true);
            using var png = new MemoryStream();
            Conversion.SavePng(png, inputBytes, pageIndex, null, renderOptions);
            png.Position = 0;
            using var sourceBitmap = SKBitmap.Decode(png)
                ?? throw new InvalidDataException($"Page {pageIndex + 1} could not be rendered.");

            var targetWidthPoints = settings.NormalizeContentWidth
                ? settings.TargetWidthMillimeters * PointsPerMillimeter
                : sourceSize.Width;
            var targetHeightPoints = settings.NormalizeContentWidth
                ? targetWidthPoints * sourceSize.Height / sourceSize.Width
                : sourceSize.Height;
            var targetWidthPixels = Math.Max(1, (int)Math.Round(targetWidthPoints * settings.Dpi / 72d));
            var targetHeightPixels = Math.Max(1, (int)Math.Round(targetHeightPoints * settings.Dpi / 72d));

            using var pageBitmap = Resize(sourceBitmap, targetWidthPixels, targetHeightPixels);
            ApplyNoise(pageBitmap, settings.Noise, pageIndex);
            using var canvas = document.BeginPage((float)targetWidthPoints, (float)targetHeightPoints);
            DrawPage(canvas, pageBitmap, targetWidthPoints, targetHeightPoints, settings);
            document.EndPage();
            progress?.Report(new BlendProgress(pageIndex + 1, pageCount, "Writing"));
        }

        document.Close();
        output.Flush(true);
    }

    private static SKBitmap Resize(SKBitmap source, int width, int height)
    {
        var result = new SKBitmap(new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Opaque));
        using var canvas = new SKCanvas(result);
        canvas.Clear(SKColors.White);
        using var paint = new SKPaint();
        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        canvas.DrawBitmap(source, new SKRect(0, 0, width, height), sampling, paint);
        return result;
    }

    private static void DrawPage(SKCanvas canvas, SKBitmap bitmap, double widthPoints, double heightPoints, BlendSettings settings)
    {
        using var image = SKImage.FromBitmap(bitmap);
        canvas.DrawImage(image, new SKRect(0, 0, (float)widthPoints, (float)heightPoints),
            new SKSamplingOptions(SKFilterMode.Linear), null);

        if (settings.Watermark == WatermarkStyle.None || string.IsNullOrWhiteSpace(settings.WatermarkText))
        {
            return;
        }

        using var paint = new SKPaint
        {
            Color = new SKColor(190, 65, 65, settings.Watermark == WatermarkStyle.FullPage ? (byte)48 : (byte)72),
            IsAntialias = true
        };
        using var font = new SKFont(SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold),
            Math.Max(10, (float)(widthPoints / 22)));

        if (settings.Watermark == WatermarkStyle.Text)
        {
            canvas.DrawText(settings.WatermarkText, 24, (float)(heightPoints - 24), SKTextAlign.Left, font, paint);
            return;
        }

        canvas.Save();
        canvas.RotateDegrees(-28, (float)(widthPoints / 2), (float)(heightPoints / 2));
        var spacingX = Math.Max(160, widthPoints / 2.3f);
        var spacingY = Math.Max(90, heightPoints / 6f);
        for (var y = -heightPoints; y < heightPoints * 2; y += spacingY)
        {
            for (var x = -widthPoints; x < widthPoints * 2; x += spacingX)
            {
                canvas.DrawText(settings.WatermarkText, (float)x, (float)y, SKTextAlign.Left, font, paint);
            }
        }
        canvas.Restore();
    }

    private static void ApplyNoise(SKBitmap bitmap, NoiseStrength strength, int pageIndex)
    {
        if (strength == NoiseStrength.Off)
        {
            return;
        }

        var pixels = bitmap.Pixels;
        var interval = strength switch
        {
            NoiseStrength.Low => 137,
            NoiseStrength.Medium => 83,
            NoiseStrength.High => 47,
            _ => int.MaxValue
        };
        var amplitude = strength == NoiseStrength.High ? 2 : 1;
        var random = new Random(unchecked(0x5EED + pageIndex * 7919));
        for (var index = 0; index < pixels.Length; index += interval)
        {
            var pixel = pixels[index];
            var delta = random.Next(-amplitude, amplitude + 1);
            pixels[index] = new SKColor(
                Clamp(pixel.Red + delta),
                Clamp(pixel.Green + delta),
                Clamp(pixel.Blue + delta),
                pixel.Alpha);
        }
        bitmap.Pixels = pixels;
    }

    private static byte Clamp(int value) => (byte)Math.Clamp(value, 0, 255);

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
