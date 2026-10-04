using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;

namespace PDFBlender.App;

internal static class ThemeManager
{
    public static bool IsDark { get; private set; }
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
        IsDark = dark;
        var resources = Application.Current.Resources;
        if (dark)
        {
            SetBrush(resources, "WindowBackgroundBrush", "#272727");
            SetBrush(resources, "ShellChromeBrush", "#202020");
            SetBrush(resources, "SidebarBrush", "#202020");
            SetBrush(resources, "TitleBarBrush", "#202020");
            SetBrush(resources, "SurfaceBrush", "#262626");
            SetBrush(resources, "CardBrush", "#323232");
            SetBrush(resources, "SettingItemBrush", "#323232");
            SetBrush(resources, "LogoTileBrush", "#3B252B");
            SetBrush(resources, "SurfaceRaisedBrush", "#343434");
            SetBrush(resources, "SurfaceMutedBrush", "#292929");
            SetBrush(resources, "BorderBrush", "#353535");
            SetBrush(resources, "TextBrush", "#F2F2F2");
            SetBrush(resources, "TitleTextBrush", "#D8D8D8");
            SetBrush(resources, "MutedTextBrush", "#B8B8B8");
            SetBrush(resources, "AccentBrush", "#C9021E");
            SetBrush(resources, "AccentDarkBrush", "#E45F60");
            SetBrush(resources, "AccentSoftBrush", "#35C9021E");
            SetToggleSwitchPalette(resources, "#C9021E", "#A80119", "#980018", "#7A333D");
            SetBrush(resources, "NavHoverBrush", "#18FFFFFF");
            SetBrush(resources, "NavSelectedBrush", "#2D2D2D");
            SetBrush(resources, "NavTextBrush", "#9A9A9A");
            SetBrush(resources, "NavigationViewItemForeground", "#9A9A9A");
            SetBrush(resources, "NavigationViewItemForegroundPointerOver", "#9A9A9A");
            SetBrush(resources, "NavigationViewItemForegroundPressed", "#9A9A9A");
            SetBrush(resources, "NavigationViewItemForegroundLeftFluent", "#C9021E");
            SetBrush(resources, "NavigationViewItemForegroundPointerOverLeftFluent", "#9A9A9A");
            SetBrush(resources, "NavigationViewItemBackgroundPointerOver", "#2D2D2D");
            SetBrush(resources, "NavigationViewItemBackgroundPointerOverLeftFluent", "#2D2D2D");
            SetBrush(resources, "NavigationViewItemBackgroundSelected", "#454545");
            SetBrush(resources, "NavigationViewItemBackgroundSelectedLeftFluent", "#454545");
            SetBrush(resources, "NavigationViewItemBackgroundPressed", "#383838");
            SetBrush(resources, "PillBrush", "#343434");
            SetBrush(resources, "PillTextBrush", "#F0F0F0");
            SetBrush(resources, "NotificationBrush", "#2D2D2D");
            SetBrush(resources, "NotificationBorderBrush", "#454545");
            SetBrush(resources, "NotificationMessageBrush", "#C8C8C8");
        }
        else
        {
            SetBrush(resources, "WindowBackgroundBrush", "#F9F9F9");
            SetBrush(resources, "ShellChromeBrush", "#F3F3F3");
            SetBrush(resources, "SidebarBrush", "#F3F3F3");
            SetBrush(resources, "TitleBarBrush", "#F3F3F3");
            SetBrush(resources, "SurfaceBrush", "#FFFFFF");
            SetBrush(resources, "CardBrush", "#FDFDFD");
            SetBrush(resources, "SettingItemBrush", "#FDFDFD");
            SetBrush(resources, "LogoTileBrush", "#FFF1F3");
            SetBrush(resources, "SurfaceRaisedBrush", "#F6F6F6");
            SetBrush(resources, "SurfaceMutedBrush", "#F8F8F8");
            SetBrush(resources, "BorderBrush", "#E6E6E6");
            SetBrush(resources, "TextBrush", "#202020");
            SetBrush(resources, "TitleTextBrush", "#868686");
            SetBrush(resources, "MutedTextBrush", "#666666");
            SetBrush(resources, "AccentBrush", "#C9021E");
            SetBrush(resources, "AccentDarkBrush", "#E45F60");
            SetBrush(resources, "AccentSoftBrush", "#25C9021E");
            SetToggleSwitchPalette(resources, "#C9021E", "#A80119", "#980018", "#E6B4BA");
            SetBrush(resources, "NavHoverBrush", "#0F000000");
            SetBrush(resources, "NavSelectedBrush", "#EAEAEA");
            SetBrush(resources, "NavTextBrush", "#868686");
            SetBrush(resources, "NavigationViewItemForeground", "#868686");
            SetBrush(resources, "NavigationViewItemForegroundPointerOver", "#868686");
            SetBrush(resources, "NavigationViewItemForegroundPressed", "#868686");
            SetBrush(resources, "NavigationViewItemForegroundLeftFluent", "#C9021E");
            SetBrush(resources, "NavigationViewItemForegroundPointerOverLeftFluent", "#868686");
            SetBrush(resources, "NavigationViewItemBackgroundPointerOver", "#EAEAEA");
            SetBrush(resources, "NavigationViewItemBackgroundPointerOverLeftFluent", "#EAEAEA");
            SetBrush(resources, "NavigationViewItemBackgroundSelected", "#FFFFFF");
            SetBrush(resources, "NavigationViewItemBackgroundSelectedLeftFluent", "#FFFFFF");
            SetBrush(resources, "NavigationViewItemBackgroundPressed", "#DEDEDE");
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

    private static void SetToggleSwitchPalette(ResourceDictionary resources, string normal, string pointerOver, string pressed, string disabled)
    {
        SetBrush(resources, "ToggleSwitchFillOn", normal);
        SetBrush(resources, "ToggleSwitchFillOnPointerOver", pointerOver);
        SetBrush(resources, "ToggleSwitchFillOnPressed", pressed);
        SetBrush(resources, "ToggleSwitchFillOnDisabled", disabled);
        SetBrush(resources, "ToggleSwitchStrokeOn", normal);
        SetBrush(resources, "ToggleSwitchStrokeOnPointerOver", pointerOver);
        SetBrush(resources, "ToggleSwitchStrokeOnPressed", pressed);
        SetBrush(resources, "ToggleSwitchStrokeOnDisabled", disabled);
        SetBrush(resources, "ToggleSwitchKnobFillOn", "#FFFFFF");
        SetBrush(resources, "ToggleSwitchKnobFillOnPointerOver", "#FFFFFF");
        SetBrush(resources, "ToggleSwitchKnobFillOnPressed", "#FFFFFF");
        SetBrush(resources, "ToggleSwitchKnobFillOnDisabled", "#F7F7F7");
    }

}
