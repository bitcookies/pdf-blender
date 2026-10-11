using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System.Security.Cryptography;
using PDFtoImage;
using PigDocument = UglyToad.PdfPig.PdfDocument;

namespace PDFBlender.Pdf;

public sealed class PdfBlendProcessor : IBlendProcessor
{
    public async Task<BlendResult> ProcessAsync(BlendRequest request, IProgress<BlendProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var errors = SettingsValidator.Validate(request.Settings);
        if (errors.Count > 0) throw new PdfProcessingException(errors[0]);
        if (!File.Exists(request.InputPath)) throw new FileNotFoundException("pdfPath", request.InputPath);
        var input = Path.GetFullPath(request.InputPath);
        var requested = Path.GetFullPath(request.OutputPath);
        if (string.Equals(input, requested, StringComparison.OrdinalIgnoreCase)) throw new PdfProcessingException("pdfPath");
        var directory = Path.GetDirectoryName(requested) ?? throw new PdfProcessingException("pdfPath");
        Directory.CreateDirectory(directory);
        var final = requested;
        var temporary = Path.Combine(directory, $".{Path.GetFileName(final)}.{Guid.NewGuid():N}.tmp");
        try
        {
            var result = await Task.Run(() => Process(input, final, temporary, request.Settings, progress, cancellationToken), cancellationToken).ConfigureAwait(false);
            return result;
        }
        catch (PdfProcessingException) { throw; }
        catch (OperationCanceledException) { throw; }
        catch (UnauthorizedAccessException ex) { throw new PdfProcessingException("pdfPath", ex); }
        catch (OutOfMemoryException ex) { throw new PdfProcessingException("pdfMemory", ex); }
        catch (IOException ex) { throw new PdfProcessingException("pdfPath", ex); }
        catch (PdfReaderException ex) { throw new PdfProcessingException(ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ? "pdfPassword" : "pdfInvalid", ex); }
        catch (InvalidDataException ex) { throw new PdfProcessingException("pdfInvalid", ex); }
        catch (Exception ex) { throw new PdfProcessingException("pdfFailed", ex); }
        finally { TryDelete(temporary); }
    }

    private static BlendResult Process(string input, string final, string temporary, BlendSettings settings, IProgress<BlendProgress>? progress, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        var bytes = File.ReadAllBytes(input);
        int pageCount;
        try { pageCount = Conversion.GetPageCount(bytes, null); }
        catch (Exception ex) when (ex is not OperationCanceledException and not OutOfMemoryException)
        {
            throw new PdfProcessingException(ex.Message.Contains("password", StringComparison.OrdinalIgnoreCase) ? "pdfPassword" : "pdfInvalid", ex);
        }
        if (pageCount <= 0) throw new PdfProcessingException("pdfInvalid");
        var hasChanges = BlendFeatureCatalog.All.Any(feature => BlendFeatureCatalog.IsEnabled(settings, feature));
        if (!hasChanges)
        {
            File.Copy(input, temporary, overwrite: false);
            token.ThrowIfCancellationRequested();
            var copiedPath = Commit(temporary, final);
            progress?.Report(new BlendProgress(pageCount, pageCount, "Copied"));
            return new BlendResult(copiedPath, pageCount);
        }
        using (var inspection = PdfReader.Open(input, PdfDocumentOpenMode.Import))
        {
            if (inspection.Internals.GetAllObjects().OfType<PdfDictionary>().Any(dictionary =>
                dictionary.Elements.GetName("/Type") == "/Sig" || dictionary.Elements.ContainsKey("/ByteRange")))
                throw new PdfProcessingException("pdfSigned");
        }
        if (settings.Protection == ProtectionMethod.Flatten)
            BuildRaster(bytes, input, temporary, pageCount, settings, progress, token);
        else
            BuildNative(input, temporary, settings, pageCount, progress, token);
        token.ThrowIfCancellationRequested();
        var committed = Commit(temporary, final);
        return new BlendResult(committed, pageCount);
    }

    private static void BuildNative(string input, string output, BlendSettings settings, int pageCount, IProgress<BlendProgress>? progress, CancellationToken token)
    {
        PdfFontResolver.EnsureInitialized();
        using var document = PdfReader.Open(input, PdfDocumentOpenMode.Modify);
        if (document.PageCount != pageCount) throw new PdfProcessingException("pdfInvalid");
        for (var i = 0; i < document.PageCount; i++)
        {
            token.ThrowIfCancellationRequested();
            var page = document.Pages[i];
            var box = page.CropBox;
            if (box.Width <= 0 || box.Height <= 0) box = page.MediaBox;
            var rotated = Math.Abs(page.Rotate % 180) == 90;
            var layout = PageLayout.Create(rotated ? box.Height : box.Width, rotated ? box.Width : box.Height, settings);
            if (settings.NormalizeContentWidth || settings.NormalizePageSize) NativePageLayout.Apply(page, layout);
            PdfOverlays.Watermark(page, settings);
            PdfOverlays.HiddenStatements(page, settings);
            progress?.Report(new BlendProgress(i + 1, pageCount, "Native"));
        }
        ApplyMetadata(document, settings);
        ApplyPermissions(document, settings);
        token.ThrowIfCancellationRequested();
        document.Save(output);
    }

    private static void BuildRaster(byte[] bytes, string input, string output, int pageCount, BlendSettings settings, IProgress<BlendProgress>? progress, CancellationToken token)
    {
        PdfFontResolver.EnsureInitialized();
        using var source = PdfReader.Open(input, PdfDocumentOpenMode.Import);
        using var document = new PdfDocument();
        CopyMetadata(source, document, settings);
        using var pig = settings.CopyLayer == CopyLayerMode.Preserve ? null : PigDocument.Open(input);
        var sourceSizes = PDFtoImage.Conversion.GetPageSizes(bytes, null);
        var totalLetters = 0;
        for (var i = 0; i < pageCount; i++)
        {
            token.ThrowIfCancellationRequested();
            var size = sourceSizes[i];
            var layout = PageLayout.Create(size.Width, size.Height, settings);
            _ = layout.Pixels(settings.Dpi);
            var page = document.AddPage();
            page.Width = XUnit.FromPoint(layout.Width);
            page.Height = XUnit.FromPoint(layout.Height);
            RasterPageBuilder.Build(page, bytes, i, layout, settings, token);
            PdfOverlays.Watermark(page, settings);
            PdfOverlays.HiddenStatements(page, settings);
            if (settings.CopyLayer != CopyLayerMode.Preserve)
            {
                var letters = pig!.GetPage(i + 1);
                var count = PdfOverlays.CopyLayer(page, letters, layout, settings.CopyLayer, token);
                totalLetters += count;
            }
            progress?.Report(new BlendProgress(i + 1, pageCount, "Raster"));
        }
        if (pig is not null && totalLetters == 0) throw new PdfProcessingException("pdfNoText");
        ApplyPermissions(document, settings);
        token.ThrowIfCancellationRequested();
        document.Save(output);
    }

    private static void CopyMetadata(PdfDocument source, PdfDocument destination, BlendSettings settings)
    {
        destination.Info.Title = source.Info.Title;
        destination.Info.Author = source.Info.Author;
        destination.Info.Subject = source.Info.Subject;
        destination.Info.Keywords = source.Info.Keywords;
        destination.Info.Creator = source.Info.Creator;
        destination.Info.CreationDate = source.Info.CreationDate;
        ApplyMetadata(destination, settings);
    }

    private static void ApplyMetadata(PdfDocument document, BlendSettings settings)
    {
        if (settings.MetadataNormalization)
        {
            document.Info.Creator = "PDF Blender";
            document.Info.Elements.SetString("/Producer", "PDF Blender 0.10.0.0");
            document.Info.ModificationDate = DateTime.UtcNow;
        }
        if (settings.ProtectionDeclaration)
            document.Info.Subject = string.IsNullOrWhiteSpace(document.Info.Subject) ? "PDF Blender protection output" : document.Info.Subject + " | PDF Blender protection output";
    }

    private static void ApplyPermissions(PdfDocument document, BlendSettings settings)
    {
        if (!settings.PermissionRestriction) return;
        var security = document.SecuritySettings;
        security.UserPassword = string.Empty;
        security.OwnerPassword = Convert.ToBase64String(RandomNumberGenerator.GetBytes(24));
        security.PermitPrint = true;
        security.PermitFullQualityPrint = true;
        // Accessibility extraction remains permitted by PDFsharp's default permission bit 10.
        security.PermitExtractContent = false;
        security.PermitModifyDocument = false;
        security.PermitAnnotations = false;
        security.PermitFormsFill = false;
        security.PermitAssembleDocument = false;
    }

    private static string NextOutputPath(string requested)
    {
        if (!File.Exists(requested)) return requested;
        var directory = Path.GetDirectoryName(requested)!;
        var stem = Path.GetFileNameWithoutExtension(requested);
        var ext = Path.GetExtension(requested);
        for (var i = 2; i < 10000; i++)
        {
            var candidate = Path.Combine(directory, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate)) return candidate;
        }
        throw new PdfProcessingException("pdfPath");
    }

    private static string Commit(string temporary, string final)
    {
        for (var i = 0; i < 100; i++)
        {
            var candidate = NextOutputPath(final);
            try { File.Move(temporary, candidate, overwrite: false); return candidate; }
            catch (IOException) when (File.Exists(candidate)) { }
        }
        throw new PdfProcessingException("pdfPath");
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
