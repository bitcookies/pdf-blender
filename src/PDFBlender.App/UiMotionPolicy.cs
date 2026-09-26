using System.Windows;
using System.Windows.Media;

namespace PDFBlender.App;

internal readonly record struct UiMotionProfile(TimeSpan EnterOpacity, TimeSpan EnterSlide, double Offset);

internal static class UiMotionPolicy
{
    public static UiMotionProfile Resolve()
    {
        var tier = RenderCapability.Tier >> 16;
        if (tier == 0)
            return new UiMotionProfile(TimeSpan.Zero, TimeSpan.Zero, 0);
        if (!SystemParameters.ClientAreaAnimation || tier == 1)
            return new UiMotionProfile(TimeSpan.FromMilliseconds(110), TimeSpan.FromMilliseconds(130), 5);
        return new UiMotionProfile(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(320), 12);
    }
}
