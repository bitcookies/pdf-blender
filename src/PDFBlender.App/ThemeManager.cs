using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace PDFBlender.App;

internal static class ThemeManager
{
    public static bool Apply(Window window, ThemePreference preference)
    {
        var dark = preference switch
        {
            ThemePreference.Dark => true,
            ThemePreference.Light => false,
            _ => !SystemUsesLightTheme()
        };

        var resources = Application.Current.Resources;
        SetBrush(resources, "WindowBackgroundBrush", dark ? "#17181D" : "#F5F6F7");
        SetBrush(resources, "SurfaceBrush", dark ? "#24262D" : "#FFFFFF");
        SetBrush(resources, "SurfaceRaisedBrush", dark ? "#2D3038" : "#F0F2F4");
        SetBrush(resources, "SurfaceMutedBrush", dark ? "#1E2026" : "#F8F9FA");
        SetBrush(resources, "BorderBrush", dark ? "#3C404A" : "#D9DDE3");
        SetBrush(resources, "TextBrush", dark ? "#F7F7F8" : "#202124");
        SetBrush(resources, "MutedTextBrush", dark ? "#AEB1BA" : "#6B7078");
        SetBrush(resources, "AccentBrush", dark ? "#FF756A" : "#E45B59");
        SetBrush(resources, "AccentDarkBrush", dark ? "#D95757" : "#C8484D");
        SetBrush(resources, "AccentTextBrush", dark ? "#24171A" : "#FFFFFF");
        SetBrush(resources, "FocusBrush", dark ? "#FF9A8F" : "#C8484D");
        window.Background = (Brush)resources["WindowBackgroundBrush"];
        return dark;
    }

    private static bool SystemUsesLightTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                "Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize");
            return Convert.ToInt32(key?.GetValue("AppsUseLightTheme", 1)) != 0;
        }
        catch (Exception)
        {
            return true;
        }
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }
}
