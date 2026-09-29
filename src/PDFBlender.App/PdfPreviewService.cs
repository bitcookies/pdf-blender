using System.Windows.Media.Imaging;
using PDFtoImage;
using System.IO;

namespace PDFBlender.App;

internal static class PdfPreviewService
{
    public static async Task<BitmapSource> RenderFirstPageAsync(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var png = new MemoryStream();
            var options = new RenderOptions(72, Width: 420, Height: null, WithAspectRatio: true, UseTiling: true);
            Conversion.SavePng(png, bytes, 0, null, options);
            cancellationToken.ThrowIfCancellationRequested();
            png.Position = 0;
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = png;
            bitmap.EndInit();
            bitmap.Freeze();
            return (BitmapSource)bitmap;
        }, cancellationToken).ConfigureAwait(false);
    }
}
