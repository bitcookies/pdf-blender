using System.Text;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using PDFtoImage;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using SkiaSharp;
using Xunit;
using PigDocument = UglyToad.PdfPig.PdfDocument;

namespace PDFBlender.Core.Tests;

[CollectionDefinition("PDF", DisableParallelization = true)]
public sealed class PdfCollection;

[Collection("PDF")]
public sealed class PdfBlendProcessorTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "pdf-blender-tests", Guid.NewGuid().ToString("N"));
    private readonly PdfBlendProcessor _processor = new();
    private static readonly object FontGate = new();

    public PdfBlendProcessorTests()
    {
        Directory.CreateDirectory(_root);
        lock (FontGate) GlobalFontSettings.FontResolver ??= new FixtureFontResolver();
    }
    public void Dispose() => Directory.Delete(_root, recursive: true);
    private string PathFor(string name) => Path.Combine(_root, name);
    private Task<BlendResult> Run(string input, BlendSettings settings, string name = "output.pdf", IProgress<BlendProgress>? progress = null, CancellationToken token = default) =>
        _processor.ProcessAsync(new BlendRequest(input, PathFor(name), settings), progress, token);
    private string Minimal()
    {
        var path = PathFor("input.pdf");
        File.WriteAllBytes(path, CreateMinimalPdf());
        return path;
    }
    private static string Text(string path)
    {
        using var pdf = PigDocument.Open(path);
        return string.Join("", pdf.GetPages().Select(page => page.Text));
    }
    private static SKBitmap Render(string path) => Conversion.ToImage(File.ReadAllBytes(path), 0, null, new RenderOptions(72));
    private static PdfDictionary[] Images(PdfDocument pdf) => pdf.Internals.GetAllObjects().OfType<PdfDictionary>().Where(item => item.Elements.GetName("/Subtype") == "/Image").ToArray();

    [Fact]
    public async Task DisabledSettingsCopyBytesExactlyAndDoNotOverwrite()
    {
        var input = Minimal();
        var original = File.ReadAllBytes(input);
        var first = await Run(input, BlendSettings.Default);
        var second = await Run(input, BlendSettings.Default);
        Assert.Equal(original, File.ReadAllBytes(first.OutputPath));
        Assert.EndsWith("output (2).pdf", second.OutputPath);
        Assert.Equal(original, File.ReadAllBytes(input));
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task NativeWidthPreservesTextAndVectorObjects()
    {
        var result = await Run(Minimal(), new BlendSettings { NormalizeContentWidth = true });
        using var pdf = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Equal(210 * 72 / 25.4, pdf.Pages[0].Width.Point, 2);
        Assert.Equal(210 * 72 / 25.4 * 200 / 300, pdf.Pages[0].Height.Point, 2);
        Assert.Empty(Images(pdf));
        Assert.Contains("PDF Blender", Text(result.OutputPath));
        using var image = Render(result.OutputPath);
        Assert.Contains(image.Pixels, pixel => pixel.Red < 100);
    }

    [Theory]
    [InlineData(0)] [InlineData(90)] [InlineData(180)] [InlineData(270)]
    public async Task NativeCropRotationMatchesVisibleSource(int rotation)
    {
        var input = Minimal();
        using (var pdf = PdfReader.Open(input, PdfDocumentOpenMode.Modify))
        {
            var page = pdf.Pages[0];
            page.CropBox = new PdfRectangle(new XRect(20, 20, 200, 150));
            page.Rotate = rotation;
            pdf.Save(PathFor("cropped.pdf"));
        }
        input = PathFor("cropped.pdf");
        using var source = Render(input);
        var result = await Run(input, new BlendSettings { NormalizeContentWidth = true, ContentWidthPreset = PageSizePreset.Custom, TargetWidthMillimeters = source.Width * 25.4 / 72 });
        using var output = Render(result.OutputPath);
        Assert.Equal(source.Width, output.Width);
        Assert.Equal(source.Height, output.Height);
        var differences = source.Pixels.Zip(output.Pixels, (a,b) => Math.Abs(a.Red-b.Red)).Average();
        Assert.True(differences < 3, $"Pixel difference {differences:0.###}");
    }

    [Theory]
    [InlineData(0)] [InlineData(90)] [InlineData(180)] [InlineData(270)]
    public async Task RasterCropRotationAndCopyCoordinatesMatchVisiblePage(int rotation)
    {
        var input = Minimal();
        using (var pdf = PdfReader.Open(input, PdfDocumentOpenMode.Modify))
        {
            pdf.Pages[0].CropBox = new PdfRectangle(new XRect(20, 20, 200, 150));
            pdf.Pages[0].Rotate = rotation;
            pdf.Save(PathFor("raster-crop.pdf"));
        }
        input = PathFor("raster-crop.pdf");
        using var before = Render(input);
        var settings = new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 72, ImageQuality = ImageQuality.Lossless, CopyLayer = CopyLayerMode.TextLayoutPerturbation };
        var result = await Run(input, settings);
        using var after = Render(result.OutputPath);
        Assert.Equal(before.Width, after.Width);
        Assert.Equal(before.Height, after.Height);
        Assert.Equal(before.Pixels, after.Pixels);
        using var sourceText = PigDocument.Open(input);
        using var outputText = PigDocument.Open(result.OutputPath);
        var original = sourceText.GetPage(1).Letters.OrderBy(letter => letter.Value).ThenBy(letter => letter.StartBaseLine.X).ThenBy(letter => letter.StartBaseLine.Y).ToArray();
        var restored = outputText.GetPage(1).Letters.OrderBy(letter => letter.Value).ThenBy(letter => letter.StartBaseLine.X).ThenBy(letter => letter.StartBaseLine.Y).ToArray();
        Assert.Equal(original.Length, restored.Length);
        for (var i = 0; i < original.Length; i++)
        {
            Assert.InRange(Math.Abs(original[i].StartBaseLine.X - restored[i].StartBaseLine.X), 0, .1);
            Assert.InRange(Math.Abs(original[i].StartBaseLine.Y - restored[i].StartBaseLine.Y), 0, .1);
        }
    }

    [Theory]
    [InlineData(ImageQuality.Compact, "/DCTDecode")]
    [InlineData(ImageQuality.Standard, "/DCTDecode")]
    [InlineData(ImageQuality.High, "/DCTDecode")]
    [InlineData(ImageQuality.Lossless, "/FlateDecode")]
    public async Task FlattenUsesSelectedEncodingAndDpiWithoutOriginalText(ImageQuality quality, string filter)
    {
        var reports = new List<BlendProgress>();
        var result = await Run(Minimal(), new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 150, ImageQuality = quality }, progress: new InlineProgress<BlendProgress>(reports.Add));
        using var pdf = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        var image = Assert.Single(Images(pdf));
        Assert.Contains(filter, image.Elements["/Filter"]!.ToString());
        Assert.Equal(625, image.Elements.GetInteger("/Width"));
        Assert.InRange(image.Elements.GetInteger("/Height"), 416, 417);
        Assert.Equal("", Text(result.OutputPath));
        Assert.Equal(1, reports[^1].CompletedPages);
        using var bitmap = Render(result.OutputPath);
        Assert.Contains(bitmap.Pixels, pixel => pixel.Red < 100);
    }

    [Theory]
    [InlineData(CopyLayerMode.UnicodeScramble)]
    [InlineData(CopyLayerMode.ReplaceWithGibberish)]
    [InlineData(CopyLayerMode.TextLayoutPerturbation)]
    public async Task CopyLayersHaveSelectableGeometryAndDoNotKeepOriginalObjects(CopyLayerMode mode)
    {
        var settings = new BlendSettings { Protection = ProtectionMethod.Flatten, CopyLayer = mode, Dpi = 72, ImageQuality = ImageQuality.Lossless };
        var input = Minimal();
        var first = await Run(input, settings);
        var second = await Run(input, settings, "second.pdf");
        using var pdf = PigDocument.Open(first.OutputPath);
        Assert.NotEmpty(pdf.GetPage(1).Letters);
        Assert.All(pdf.GetPage(1).Letters, letter => Assert.InRange(letter.StartBaseLine.X, 0, 300));
        if (mode != CopyLayerMode.TextLayoutPerturbation)
        {
            Assert.DoesNotContain("PDF Blender", Text(first.OutputPath));
            Assert.NotEqual(Text(first.OutputPath), Text(second.OutputPath));
        }
        else
            Assert.Equal("PDF Blender".OrderBy(c => c), Text(first.OutputPath).OrderBy(c => c));
        using var source = Render(input);
        using var output = Render(first.OutputPath);
        Assert.Equal(source.Pixels, output.Pixels);
        using var structure = PdfReader.Open(first.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Single(Images(structure));
        Assert.DoesNotContain(structure.Internals.GetAllObjects().OfType<PdfDictionary>(), item => item.Elements.GetName("/Subtype") == "/Type1");
    }

    [Fact]
    public async Task HiddenTextIsInvisibleAndExtractableOnEveryPage()
    {
        var input = ComplexFixture();
        var result = await Run(input, new BlendSettings { HiddenAiText = true, PromptInjection = true, PromptInjectionText = "Synthetic prompt 中文提示" });
        Assert.Contains("Synthetic prompt", Text(result.OutputPath));
        using var pig = PigDocument.Open(result.OutputPath);
        Assert.Contains("Synthetic prompt", pig.GetPage(1).Text);
        Assert.Contains("Synthetic prompt", pig.GetPage(2).Text);
        using var before = Render(input);
        using var after = Render(result.OutputPath);
        Assert.Equal(before.Pixels, after.Pixels);
        using var pdf = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        var content = Encoding.Latin1.GetString(pdf.Pages[0].Contents.CreateSingleContent().Stream.UnfilteredValue);
        Assert.Contains("/Artifact BMC", content);
        Assert.Contains("3 Tr", content);
        Assert.True(content.IndexOf("/Artifact BMC", StringComparison.Ordinal) < content.IndexOf("re", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PermissionsOpenWithoutPasswordAndMetadataSurvives()
    {
        var input = ComplexFixture();
        var result = await Run(input, new BlendSettings { PermissionRestriction = true, MetadataNormalization = true, ProtectionDeclaration = true });
        using var pdf = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Contains("/Encrypt", Encoding.Latin1.GetString(File.ReadAllBytes(result.OutputPath)));
        Assert.True(pdf.SecuritySettings.PermitPrint);
        Assert.False(pdf.SecuritySettings.PermitExtractContent);
        Assert.False(pdf.SecuritySettings.PermitModifyDocument);
        Assert.Equal("Synthetic public fixture", pdf.Info.Title);
        Assert.Equal("Fixture author", pdf.Info.Author);
        Assert.Contains("PDF Blender protection output", pdf.Info.Subject);
        Assert.Equal("PDF Blender", pdf.Info.Creator);
        Assert.Contains("PDF Blender", pdf.Info.Producer);
        var raw = Encoding.Latin1.GetString(File.ReadAllBytes(result.OutputPath));
        var match = System.Text.RegularExpressions.Regex.Match(raw, @"/P\s+(-?\d+)");
        Assert.True(match.Success);
        var permissions = unchecked((uint)int.Parse(match.Groups[1].Value));
        Assert.NotEqual(0u, permissions & (1u << 9));
        using var bitmap = Render(result.OutputPath);
        Assert.True(bitmap.Width > 0);
    }

    [Fact]
    public async Task CustomCopyWarningIsExtractable()
    {
        var result = await Run(Minimal(), new BlendSettings
        {
            Protection = ProtectionMethod.Flatten,
            Dpi = 72,
            ImageQuality = ImageQuality.Lossless,
            CopyLayer = CopyLayerMode.CustomWarning,
            CopyLayerCustomText = "DO NOT COPY"
        });

        Assert.Contains("DO NOT COPY", Text(result.OutputPath));
        using var pig = PigDocument.Open(result.OutputPath);
        Assert.Contains("DO NOT COPY", pig.GetPage(1).Text);
    }
    [Fact]
    public async Task CopyLayerRequiresTextButSupportsMixedImageAndTextPages()
    {
        var imageOnly = PathFor("image.pdf");
        using (var pdf = new PdfDocument()) { pdf.AddPage(); pdf.Save(imageOnly); }
        var settings = new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 72, CopyLayer = CopyLayerMode.ReplaceWithGibberish };
        var error = await Assert.ThrowsAsync<PdfProcessingException>(() => Run(imageOnly, settings));
        Assert.Equal("pdfNoText", error.ErrorKey);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
        using (var mixed = PdfReader.Open(Minimal(), PdfDocumentOpenMode.Modify))
        {
            mixed.AddPage();
            mixed.Save(PathFor("mixed.pdf"));
        }
        var result = await Run(PathFor("mixed.pdf"), settings);
        Assert.Equal(2, result.PageCount);
    }

    [Fact]
    public async Task PixelCapAndCancellationLeaveNoPartialOutput()
    {
        var error = await Assert.ThrowsAsync<PdfProcessingException>(() => Run(Minimal(), new BlendSettings { Protection = ProtectionMethod.Flatten, NormalizeContentWidth = true, ContentWidthPreset = PageSizePreset.Custom, TargetWidthMillimeters = 2000, Dpi = 600 }));
        Assert.Equal("pdfPageSize", error.ErrorKey);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
        using var cts = new CancellationTokenSource();
        var progress = new InlineProgress<BlendProgress>(_ => cts.Cancel());
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Run(ComplexFixture(), new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 72 }, progress: progress, token: cts.Token));
        Assert.False(File.Exists(PathFor("output.pdf")));
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task ComplexChineseEnglishFormulaTableTransparencyAndLandscapeProcess()
    {
        var input = ComplexFixture();
        foreach (var definition in BuiltInConfigurations.Definitions.Where(definition => !definition.IsExperimental))
        {
            var preset = definition.Configuration;
            var result = await Run(input, preset.Settings, preset.Id + ".pdf");
            Assert.Equal(2, result.PageCount);
            using var output = PigDocument.Open(result.OutputPath);
            Assert.Equal(2, output.NumberOfPages);
            if (preset.Settings.Protection != ProtectionMethod.Flatten)
            {
                Assert.Contains("中文测试", Text(result.OutputPath));
                Assert.Contains("E=mc", Text(result.OutputPath));
            }
            using var bitmap = Render(result.OutputPath);
            Assert.Contains(bitmap.Pixels, pixel => pixel.Red < 100);
            var reviewFolder = Environment.GetEnvironmentVariable("PDFBLENDER_REVIEW_DIR");
            if (!string.IsNullOrWhiteSpace(reviewFolder))
            {
                Directory.CreateDirectory(reviewFolder);
                File.Copy(input, Path.Combine(reviewFolder, "source.pdf"), true);
                File.Copy(result.OutputPath, Path.Combine(reviewFolder, preset.Id + ".pdf"), true);
                using var data = bitmap.Encode(SKEncodedImageFormat.Png, 100);
                File.WriteAllBytes(Path.Combine(reviewFolder, preset.Id + ".png"), data.ToArray());
            }
        }
    }

    [Fact]
    public async Task ConcurrentOutputCollisionsReturnActualCommittedPaths()
    {
        var input = Minimal();
        var results = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => Run(input, BlendSettings.Default)));
        Assert.Equal(4, results.Select(result => result.OutputPath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(results, result => Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(result.OutputPath)));
    }

    [Fact]
    public async Task DamagedPasswordProtectedAndSignedFilesFailClearly()
    {
        var bad = PathFor("bad.pdf");
        File.WriteAllText(bad, "This is not a PDF");
        var damaged = await Assert.ThrowsAsync<PdfProcessingException>(() => Run(bad, new BlendSettings { NormalizeContentWidth = true }));
        Assert.Equal("pdfInvalid", damaged.ErrorKey);
        var input = Minimal();
        using (var pdf = PdfReader.Open(input, PdfDocumentOpenMode.Modify))
        {
            pdf.SecuritySettings.UserPassword = "SyntheticFixturePassword";
            pdf.Save(PathFor("password.pdf"));
        }
        var password = await Assert.ThrowsAsync<PdfProcessingException>(() => Run(PathFor("password.pdf"), new BlendSettings { NormalizeContentWidth = true }));
        Assert.Equal("pdfPassword", password.ErrorKey);
        using (var pdf = PdfReader.Open(input, PdfDocumentOpenMode.Modify))
        {
            var signature = new PdfDictionary(pdf);
            signature.Elements.SetName("/Type", "/Sig");
            signature.Elements["/ByteRange"] = new PdfArray(pdf, new PdfInteger(0), new PdfInteger(1), new PdfInteger(2), new PdfInteger(3));
            pdf.Internals.AddObject(signature);
            pdf.Internals.Catalog.Elements["/FixtureSignature"] = signature.Reference!;
            pdf.Save(PathFor("signed.pdf"));
        }
        var signed = await Assert.ThrowsAsync<PdfProcessingException>(() => Run(PathFor("signed.pdf"), new BlendSettings { NormalizeContentWidth = true }));
        Assert.Equal("pdfSigned", signed.ErrorKey);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task NativePaperFitAndSimpleLinkAnnotationsScaleTogether()
    {
        var input = Minimal();
        using (var pdf = PdfReader.Open(input, PdfDocumentOpenMode.Modify))
        {
            pdf.Pages[0].AddWebLink(new PdfRectangle(new XRect(24, 95, 100, 20)), "https://example.invalid/");
            pdf.Save(PathFor("link.pdf"));
        }
        var result = await Run(PathFor("link.pdf"), new BlendSettings { NormalizePageSize = true, PageSize = PageSizePreset.A4 });
        using var output = PdfReader.Open(result.OutputPath, PdfDocumentOpenMode.Import);
        Assert.Equal(297 * 72 / 25.4, output.Pages[0].Width.Point, 2);
        Assert.Equal(210 * 72 / 25.4, output.Pages[0].Height.Point, 2);
        var rectangle = output.Pages[0].Annotations[0].Rectangle;
        Assert.InRange(rectangle.X1, 0, output.Pages[0].Width.Point);
        Assert.True(rectangle.Width > 100);
        Assert.Contains("PDF Blender", Text(result.OutputPath));
        Assert.Empty(Images(output));
    }

    [Theory]
    [InlineData("noise")] [InlineData("texture")] [InlineData("geometry")] [InlineData("microtext")]
    public async Task EveryRasterEffectChangesPixelsWithoutChangingPageGeometry(string effect)
    {
        var input = Minimal();
        var baseline = new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 150, ImageQuality = ImageQuality.Lossless };
        var plain = await Run(input, baseline, "plain.pdf");
        var settings = effect switch
        {
            "noise" => baseline with { Noise = NoiseStrength.High },
            "texture" => baseline with { Texture = TextureStrength.Medium },
            "geometry" => baseline with { GeometryPerturbation = true },
            _ => baseline with { MicroTextPattern = true }
        };
        var result = await Run(input, settings);
        using var a = Render(plain.OutputPath);
        using var b = Render(result.OutputPath);
        Assert.Equal(a.Width, b.Width);
        Assert.Equal(a.Height, b.Height);
        Assert.NotEqual(a.Pixels, b.Pixels);
        var difference = a.Pixels.Zip(b.Pixels, (x,y) => Math.Abs(x.Red-y.Red)).Average();
        Assert.InRange(difference, .00001, 10);
    }

    [Fact]
    public async Task WatermarkAngleAndImageQualityChangeActualOutput()
    {
        var input = Minimal();
        var watermark = new BlendSettings { Watermark = WatermarkStyle.Text, WatermarkText = "Test 中文" };
        var first = await Run(input, watermark with { WatermarkAngle = 45 }, "positive.pdf");
        var second = await Run(input, watermark with { WatermarkAngle = -45 }, "negative.pdf");
        using var a = Render(first.OutputPath);
        using var b = Render(second.OutputPath);
        Assert.NotEqual(a.Pixels, b.Pixels);
        var compact = await Run(ComplexFixture(), new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 150, ImageQuality = ImageQuality.Compact }, "compact.pdf");
        var high = await Run(PathFor("complex.pdf"), new BlendSettings { Protection = ProtectionMethod.Flatten, Dpi = 150, ImageQuality = ImageQuality.High }, "high.pdf");
        Assert.True(new FileInfo(high.OutputPath).Length > new FileInfo(compact.OutputPath).Length);
    }

    private string ComplexFixture()
    {
        var path = PathFor("complex.pdf");
        using var document = new PdfDocument();
        document.Info.Title = "Synthetic public fixture";
        document.Info.Author = "Fixture author";
        document.Info.Subject = "Visual fidelity";
        document.Info.Keywords = "synthetic, public";
        for (var index = 0; index < 2; index++)
        {
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(index == 0 ? 420 : 600);
            page.Height = XUnit.FromPoint(index == 0 ? 600 : 420);
            using var graphics = XGraphics.FromPdfPage(page);
            var font = new XFont("BlenderCjk", 16, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
            graphics.DrawString("中文测试", font, XBrushes.Black, new XPoint(24, 45));
            var latin = new XFont("BlenderLatin", 16, XFontStyleEx.Regular, new XPdfFontOptions(PdfFontEncoding.Unicode));
            graphics.DrawString("English E=mc² ∫ x dx = x²/2", latin, XBrushes.Black, new XPoint(110, 45));
            graphics.DrawString("Table / chart / transparency", font, XBrushes.Black, new XPoint(24, 75));
            for (var row = 0; row < 4; row++)
            {
                graphics.DrawRectangle(XPens.Gray, 24, 100 + row * 28, 270, 28);
                graphics.DrawString("Row " + row, font, XBrushes.Black, new XPoint(30, 120 + row * 28));
            }
            graphics.DrawEllipse(new XSolidBrush(XColor.FromArgb(90, 210, 20, 40)), 70, 240, 90, 90);
            graphics.DrawEllipse(new XSolidBrush(XColor.FromArgb(100, 20, 80, 210)), 120, 260, 90, 90);
            graphics.DrawLines(XPens.Blue, new[] { new XPoint(24, 370), new XPoint(100, 345), new XPoint(190, 390), new XPoint(270, 360) });
            using var bitmap = new SKBitmap(80, 60);
            using (var canvas = new SKCanvas(bitmap)) { canvas.Clear(SKColors.Transparent); using var paint = new SKPaint { Color = new SKColor(20, 160, 90, 120) }; canvas.DrawCircle(30, 30, 28, paint); }
            using var data = SKImage.FromBitmap(bitmap).Encode(SKEncodedImageFormat.Png, 100);
            using var stream = new MemoryStream(data.ToArray());
            using var image = XImage.FromStream(stream);
            graphics.DrawImage(image, 300, 100, 80, 60);
        }
        document.Save(path);
        return path;
    }

    private sealed class FixtureFontResolver : IFontResolver
    {
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new(familyName == "BlenderCjk" ? "simhei.ttf" : "arial.ttf");
        public byte[] GetFont(string faceName) => File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), faceName));
    }
    private sealed class InlineProgress<T>(Action<T> callback) : IProgress<T> { public void Report(T value) => callback(value); }

    private static byte[] CreateMinimalPdf()
    {
        var content = "BT /F1 18 Tf 24 100 Td (PDF Blender) Tj ET\n";
        var objects = new[]
        {
            "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n",
            "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n",
            "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>endobj\n",
            "4 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n",
            $"5 0 obj<< /Length {content.Length} >>stream\n{content}endstream\nendobj\n"
        };
        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write("%PDF-1.4\n"); writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var value in objects) { offsets.Add(stream.Position); writer.Write(value); writer.Flush(); }
        var xref = stream.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n"); writer.Flush();
        return stream.ToArray();
    }
}
