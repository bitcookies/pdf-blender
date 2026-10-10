namespace PDFBlender.Core.Processing;

public sealed class PdfProcessingException(string errorKey, Exception? innerException = null) : Exception(errorKey, innerException)
{
    public string ErrorKey { get; } = errorKey;
}
