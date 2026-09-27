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
        if (dark)
        {
            SetBrush(resources, "WindowBackgroundBrush", "#202020");
            SetBrush(resources, "ShellChromeBrush", "#1A1A1A");
            SetBrush(resources, "SidebarBrush", "#1C1C1C");
            SetBrush(resources, "TitleBarBrush", "#1A1A1A");
            SetBrush(resources, "SurfaceBrush", "#262626");
            SetBrush(resources, "CardBrush", "#262626");
            SetBrush(resources, "SettingItemBrush", "#292929");
            SetBrush(resources, "LogoTileBrush", "#3B252B");
            SetBrush(resources, "SurfaceRaisedBrush", "#343434");
            SetBrush(resources, "SurfaceMutedBrush", "#292929");
            SetBrush(resources, "BorderBrush", "#353535");
            SetBrush(resources, "TextBrush", "#F2F2F2");
            SetBrush(resources, "TitleTextBrush", "#D8D8D8");
            SetBrush(resources, "MutedTextBrush", "#B8B8B8");
            SetBrush(resources, "AccentBrush", "#F06A73");
            SetBrush(resources, "AccentDarkBrush", "#D6535D");
            SetBrush(resources, "AccentSoftBrush", "#35F06A73");
            SetBrush(resources, "NavHoverBrush", "#18FFFFFF");
            SetBrush(resources, "NavSelectedBrush", "#3B252B");
            SetBrush(resources, "NavTextBrush", "#D8D8D8");
            SetBrush(resources, "PillBrush", "#343434");
            SetBrush(resources, "PillTextBrush", "#F0F0F0");
            SetBrush(resources, "NotificationBrush", "#2D2D2D");
            SetBrush(resources, "NotificationBorderBrush", "#454545");
            SetBrush(resources, "NotificationMessageBrush", "#C8C8C8");
        }
        else
        {
            SetBrush(resources, "WindowBackgroundBrush", "#F7F7F7");
            SetBrush(resources, "ShellChromeBrush", "#FDFDFD");
            SetBrush(resources, "SidebarBrush", "#FAFAFA");
            SetBrush(resources, "TitleBarBrush", "#FDFDFD");
            SetBrush(resources, "SurfaceBrush", "#FFFFFF");
            SetBrush(resources, "CardBrush", "#FFFFFF");
            SetBrush(resources, "SettingItemBrush", "#FFFFFF");
            SetBrush(resources, "LogoTileBrush", "#FFF1F3");
            SetBrush(resources, "SurfaceRaisedBrush", "#F6F6F6");
            SetBrush(resources, "SurfaceMutedBrush", "#F8F8F8");
            SetBrush(resources, "BorderBrush", "#E6E6E6");
            SetBrush(resources, "TextBrush", "#202020");
            SetBrush(resources, "TitleTextBrush", "#868686");
            SetBrush(resources, "MutedTextBrush", "#666666");
            SetBrush(resources, "AccentBrush", "#D84F58");
            SetBrush(resources, "AccentDarkBrush", "#B63D48");
            SetBrush(resources, "AccentSoftBrush", "#25D84F58");
            SetBrush(resources, "NavHoverBrush", "#0F000000");
            SetBrush(resources, "NavSelectedBrush", "#FFF0DDE0");
            SetBrush(resources, "NavTextBrush", "#202020");
            SetBrush(resources, "PillBrush", "#F6F6F6");
            SetBrush(resources, "PillTextBrush", "#202020");
            SetBrush(resources, "NotificationBrush", "#FCFCFC");
            SetBrush(resources, "NotificationBorderBrush", "#DADADA");
            SetBrush(resources, "NotificationMessageBrush", "#5E5E5E");
        }
        SetBrush(resources, "AccentTextBrush", "#FFFFFF");
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }
}
