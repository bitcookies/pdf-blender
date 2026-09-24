using System.Text;
using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using Xunit;

namespace PDFBlender.Core.Tests;

public sealed class PdfBlendProcessorTests
{
    [Fact]
    public async Task ProcessesMinimalPdfAndReportsPages()
    {
        var root = Path.Combine(Path.GetTempPath(), "pdf-blender-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var input = Path.Combine(root, "input.pdf");
        var output = Path.Combine(root, "output.pdf");
        await File.WriteAllBytesAsync(input, CreateMinimalPdf());

        try
        {
            var reports = new List<BlendProgress>();
            var result = await new PdfBlendProcessor().ProcessAsync(
                new BlendRequest(input, output, new BlendSettings
                {
                    Protection = ProtectionMethod.Flatten,
                    Watermark = WatermarkStyle.None,
                    Noise = NoiseStrength.Off,
                    Dpi = 72,
                    TargetWidthMillimeters = 100
                }), new Progress<BlendProgress>(reports.Add));

            Assert.Equal(output, result.OutputPath);
            Assert.Equal(1, result.PageCount);
            Assert.True(new FileInfo(output).Length > 500);
            Assert.Equal(1, reports[^1].CompletedPages);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static byte[] CreateMinimalPdf()
    {
        var objects = new[]
        {
            "1 0 obj<< /Type /Catalog /Pages 2 0 R >>endobj\n",
            "2 0 obj<< /Type /Pages /Kids [3 0 R] /Count 1 >>endobj\n",
            "3 0 obj<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>endobj\n",
            "4 0 obj<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>endobj\n",
            "5 0 obj<< /Length 44 >>stream\nBT /F1 18 Tf 24 100 Td (PDF Blender) Tj ET\nendstream\nendobj\n"
        };

        using var stream = new MemoryStream();
        using var writer = new StreamWriter(stream, new UTF8Encoding(false), leaveOpen: true);
        writer.Write("%PDF-1.4\n");
        writer.Flush();
        var offsets = new List<long> { 0 };
        foreach (var value in objects)
        {
            offsets.Add(stream.Position);
            writer.Write(value);
            writer.Flush();
        }
        var xref = stream.Position;
        writer.Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) writer.Write($"{offset:0000000000} 00000 n \n");
        writer.Write($"trailer<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        writer.Flush();
        return stream.ToArray();
    }
}
