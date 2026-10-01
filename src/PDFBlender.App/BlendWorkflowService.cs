using PDFBlender.Core.Models;
using PDFBlender.Core.Processing;
using PDFBlender.Pdf;
using System.IO;

namespace PDFBlender.App;

internal sealed record BlendBatchProgress(int CompletedFiles, int TotalFiles, BlendProgress PageProgress)
{
    public double Fraction => TotalFiles <= 0
        ? 0
        : Math.Clamp((CompletedFiles + PageProgress.Fraction) / TotalFiles, 0, 1);
}

internal sealed class BlendWorkflowService
{
    private readonly IBlendProcessor _processor;

    public BlendWorkflowService(IBlendProcessor? processor = null) => _processor = processor ?? new PdfBlendProcessor();

    public async Task<IReadOnlyList<BlendResult>> BlendAsync(
        IReadOnlyList<string> inputPaths,
        BlendSettings settings,
        IProgress<BlendBatchProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputPaths);
        var results = new List<BlendResult>(inputPaths.Count);
        for (var index = 0; index < inputPaths.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var inputPath = inputPaths[index];
            var outputPath = Path.Combine(
                Path.GetDirectoryName(inputPath) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(inputPath)}_blended.pdf");
            var pageProgress = new Progress<BlendProgress>(value =>
                progress?.Report(new BlendBatchProgress(index, inputPaths.Count, value)));
            results.Add(await _processor.ProcessAsync(
                new BlendRequest(inputPath, outputPath, settings),
                pageProgress,
                cancellationToken));
            progress?.Report(new BlendBatchProgress(index + 1, inputPaths.Count, new BlendProgress(1, 1, "Completed")));
        }

        return results;
    }
}
