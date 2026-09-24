namespace PDFBlender.Core.Processing;

using PDFBlender.Core.Models;

public sealed record BlendRequest(string InputPath, string OutputPath, BlendSettings Settings);

public sealed record BlendProgress(int CompletedPages, int TotalPages, string Stage)
{
    public double Fraction => TotalPages <= 0 ? 0 : (double)CompletedPages / TotalPages;
}

public sealed record BlendResult(string OutputPath, int PageCount);

public interface IBlendProcessor
{
    Task<BlendResult> ProcessAsync(
        BlendRequest request,
        IProgress<BlendProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
