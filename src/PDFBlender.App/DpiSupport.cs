using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PDFBlender.App;

/// <summary>
/// Shared WPF DPI helpers. WPF layout values are device-independent pixels; this
/// class only refreshes rendering when the monitor scale changes.
/// </summary>
internal static class DpiSupport
{
    public static DpiScale GetScale(Visual visual) => VisualTreeHelper.GetDpi(visual);

    public static void RefreshVisuals(FrameworkElement root)
    {
        ApplyImageQuality(root);
        root.InvalidateMeasure();
        root.InvalidateArrange();
        root.InvalidateVisual();
    }

    private static void ApplyImageQuality(DependencyObject node)
    {
        if (node is Image image)
        {
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);

        }

        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(node); index++)
            ApplyImageQuality(VisualTreeHelper.GetChild(node, index));
    }
}


