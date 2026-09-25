using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace PDFBlender.App;

internal static class ThemeManager
{
    public static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) != 0;
        }
        catch (Exception)
        {
            return true;
        }
    }

    public static void ApplyPalette(bool dark)
    {
        var resources = Application.Current.Resources;
        SetBrush(resources, "WindowBackgroundBrush", dark ? "#202020" : "#F7F7F7");
        SetBrush(resources, "SidebarBrush", dark ? "#1C1C1C" : "#FAFAFA");
        SetBrush(resources, "TitleBarBrush", dark ? "#1A1A1A" : "#FDFDFD");
        SetBrush(resources, "SurfaceBrush", dark ? "#262626" : "#FFFFFF");
        SetBrush(resources, "SurfaceRaisedBrush", dark ? "#303030" : "#F1F1F1");
        SetBrush(resources, "SurfaceMutedBrush", dark ? "#242424" : "#F8F8F8");
        SetBrush(resources, "BorderBrush", dark ? "#353535" : "#E6E6E6");
        SetBrush(resources, "TextBrush", dark ? "#F2F2F2" : "#202020");
        SetBrush(resources, "MutedTextBrush", dark ? "#B8B8B8" : "#666666");
        SetBrush(resources, "AccentBrush", dark ? "#F06A73" : "#D84F58");
        SetBrush(resources, "AccentDarkBrush", dark ? "#D6535D" : "#B63D48");
        SetBrush(resources, "AccentSoftBrush", dark ? "#35F06A73" : "#25D84F58");
        SetBrush(resources, "AccentTextBrush", "#FFFFFF");
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }
}
