using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace PDFBlender.App;

internal static class ThemeManager
{
    private static bool _monitoring;
    private static bool? _lastSystemDark;

    public static bool IsDark { get; private set; }

    public static event EventHandler? SystemThemeChanged;
    public static event EventHandler? SystemPreferencesChanged;

    public static void StartSystemMonitoring()
    {
        if (_monitoring)
            return;

        _monitoring = true;
        _lastSystemDark = !SystemUsesLightTheme();
        SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
    }

    public static void StopSystemMonitoring()
    {
        if (!_monitoring)
            return;

        SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
        SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        _monitoring = false;
        _lastSystemDark = null;
    }

    private static void SystemEvents_UserPreferenceChanged(object? sender, UserPreferenceChangedEventArgs e)
    {
        PublishSystemPreferencesChanged();
        PublishSystemThemeIfChanged();
    }

    private static void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
    {
        PublishSystemPreferencesChanged();
        PublishSystemThemeIfChanged();
    }

    private static void PublishSystemPreferencesChanged()
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        if (dispatcher.CheckAccess())
            SystemPreferencesChanged?.Invoke(null, EventArgs.Empty);
        else
            dispatcher.BeginInvoke(new Action(() => SystemPreferencesChanged?.Invoke(null, EventArgs.Empty)));
    }

    private static void PublishSystemThemeIfChanged()
    {
        var isDark = !SystemUsesLightTheme();
        if (_lastSystemDark == isDark)
            return;

        _lastSystemDark = isDark;
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
            return;

        if (dispatcher.CheckAccess())
            SystemThemeChanged?.Invoke(null, EventArgs.Empty);
        else
            dispatcher.BeginInvoke(new Action(() => SystemThemeChanged?.Invoke(null, EventArgs.Empty)));
    }

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
            SetBrush(resources, "ShellChromeBrush", "#1F1F1F");
            SetBrush(resources, "SidebarBrush", "#1F1F1F");
            SetBrush(resources, "TitleBarBrush", "#1F1F1F");
            SetBrush(resources, "SurfaceBrush", "#252525");
            SetBrush(resources, "CardBrush", "#323232");
            SetBrush(resources, "SettingItemBrush", "#323232");
            SetBrush(resources, "LogoTileBrush", "#3B252B");
            SetBrush(resources, "SurfaceRaisedBrush", "#383838");
            SetBrush(resources, "SurfaceMutedBrush", "#242424");
            SetBrush(resources, "BorderBrush", "#454545");
            SetBrush(resources, "TextBrush", "#F2F2F2");
            SetBrush(resources, "IconPickerForegroundBrush", "#FFFFFF");
            SetBrush(resources, "ColorChoiceActiveBorderBrush", "#F2F2F2");
            SetBrush(resources, "ColorChoiceHoverBorderBrush", "#A6A6A6");
            SetBrush(resources, "TitleTextBrush", "#F2F2F2");
            SetBrush(resources, "MutedTextBrush", "#B8B8B8");
            SetBrush(resources, "AccentBrush", "#E74856");
            SetBrush(resources, "AccentDarkBrush", "#F05A67");
            SetBrush(resources, "AccentSoftBrush", "#35E74856");
            SetToggleSwitchPalette(resources, "#E74856", "#F05A67", "#C83A48", "#6A3339");
            SetBrush(resources, "NavHoverBrush", "#2D2D2D");
            SetBrush(resources, "NavSelectedBrush", "#454545");
            SetBrush(resources, "NavTextBrush", "#B8B8B8");
            SetBrush(resources, "NavigationViewItemForeground", "#B8B8B8");
            SetBrush(resources, "NavigationViewItemForegroundPointerOver", "#F2F2F2");
            SetBrush(resources, "NavigationViewItemForegroundPressed", "#F2F2F2");
            SetBrush(resources, "NavigationViewItemForegroundLeftFluent", "#E74856");
            SetBrush(resources, "NavigationViewItemForegroundPointerOverLeftFluent", "#F2F2F2");
            SetBrush(resources, "NavigationViewItemBackgroundPointerOver", "#2D2D2D");
            SetBrush(resources, "NavigationViewItemBackgroundPointerOverLeftFluent", "#2D2D2D");
            SetBrush(resources, "NavigationViewItemBackgroundSelected", "#454545");
            SetBrush(resources, "NavigationViewItemBackgroundSelectedLeftFluent", "#454545");
            SetBrush(resources, "NavigationViewItemBackgroundPressed", "#3A3A3A");
            SetBrush(resources, "NavigationViewContentBackground", "#1F1F1F");
            SetBrush(resources, "NavigationViewContentGridBorderBrush", "#1F1F1F");
            SetBrush(resources, "LeftNavigationViewSeparatorBrush", "#454545");
            SetBrush(resources, "NavigationViewSelectionIndicatorForeground", "#E74856");
            SetBrush(resources, "NavigationViewItemBorderBrush", "#454545");
            SetWpfUiSurfacePalette(resources, "#272727", "#252525", "#2D2D2D", "#2D2D2D", "#383838", "#454545", "#242424", "#F2F2F2", "#B8B8B8", "#8A8A8A");
            SetBrush(resources, "PillBrush", "#3A3A3A");
            SetBrush(resources, "PillTextBrush", "#F2F2F2");
            SetBrush(resources, "NotificationBrush", "#2D2D2D");
            SetBrush(resources, "NotificationBorderBrush", "#454545");
            SetBrush(resources, "NotificationMessageBrush", "#D8D8D8");
            SetBrush(resources, "BreadcrumbBarNormalForegroundBrush", "#F2F2F2");
            SetBrush(resources, "BreadcrumbBarHoverForegroundBrush", "#B8B8B8");
            SetBrush(resources, "BreadcrumbBarCurrentNormalForegroundBrush", "#F2F2F2");
            SetBrush(resources, "DisabledTextBrush", "#8A8A8A");
            SetBrush(resources, "EditorSaveDisabledTextBrush", "#B8B8B8");
            SetBrush(resources, "SliderThumbBrush", "#1F1F1F");
            SetBrush(resources, "SliderThumbBorderBrush", "#6A6A6A");
            SetBrush(resources, "TitleBarButtonsForegroundBrush", "#FFFFFF");
            SetBrush(resources, "TitleBarButtonsHoverBrush", "#191919");
            SetBrush(resources, "AboutCardHoverBrush", "#393939");
            SetColor(resources, "AccentButtonStartColor", "#E74856");
            SetColor(resources, "AccentButtonEndColor", "#D33D4D");
            SetColor(resources, "AccentButtonHoverStartColor", "#F05A67");
            SetColor(resources, "AccentButtonHoverEndColor", "#D93D4D");
            SetColor(resources, "EditorSaveDisabledStartColor", "#454545");
            SetColor(resources, "EditorSaveDisabledEndColor", "#3A3A3A");
            SetShadow(resources, dark: true);
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
            SetBrush(resources, "IconPickerForegroundBrush", "#202020");
            SetBrush(resources, "ColorChoiceActiveBorderBrush", "#666666");
            SetBrush(resources, "ColorChoiceHoverBorderBrush", "#B3B3B3");
            SetBrush(resources, "TitleTextBrush", "#5E5E5E");
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
            SetBrush(resources, "NavigationViewContentBackground", "#F3F3F3");
            SetBrush(resources, "NavigationViewContentGridBorderBrush", "#E6E6E6");
            SetBrush(resources, "LeftNavigationViewSeparatorBrush", "#E6E6E6");
            SetBrush(resources, "NavigationViewSelectionIndicatorForeground", "#C9021E");
            SetBrush(resources, "NavigationViewItemBorderBrush", "#E6E6E6");
            SetWpfUiSurfacePalette(resources, "#F9F9F9", "#F3F3F3", "#E6E6E6", "#FFFFFF", "#F6F6F6", "#EAEAEA", "#F8F8F8", "#202020", "#5E5E5E", "#777777");
            SetBrush(resources, "PillBrush", "#F6F6F6");
            SetBrush(resources, "PillTextBrush", "#202020");
            SetBrush(resources, "NotificationBrush", "#FCFCFC");
            SetBrush(resources, "NotificationBorderBrush", "#DADADA");
            SetBrush(resources, "NotificationMessageBrush", "#5E5E5E");
            SetBrush(resources, "BreadcrumbBarNormalForegroundBrush", "#333333");
            SetBrush(resources, "BreadcrumbBarHoverForegroundBrush", "#909090");
            SetBrush(resources, "BreadcrumbBarCurrentNormalForegroundBrush", "#333333");
            SetBrush(resources, "DisabledTextBrush", "#777777");
            SetBrush(resources, "EditorSaveDisabledTextBrush", "#666666");
            SetBrush(resources, "SliderThumbBrush", "#FFFFFF");
            SetBrush(resources, "SliderThumbBorderBrush", "#E6E6E6");
            SetBrush(resources, "TitleBarButtonsForegroundBrush", "#202020");
            SetBrush(resources, "TitleBarButtonsHoverBrush", "#E6E6E6");
            SetBrush(resources, "AboutCardHoverBrush", "#F6F6F6");
            SetColor(resources, "AccentButtonStartColor", "#EA7663");
            SetColor(resources, "AccentButtonEndColor", "#D84D31");
            SetColor(resources, "AccentButtonHoverStartColor", "#FB536D");
            SetColor(resources, "AccentButtonHoverEndColor", "#D23040");
            SetColor(resources, "EditorSaveDisabledStartColor", "#D6D6D6");
            SetColor(resources, "EditorSaveDisabledEndColor", "#C8C8C8");
            SetShadow(resources, dark: false);
        }

        SetBrush(resources, "AccentTextBrush", "#FFFFFF");
        SetBrush(resources, "ContentDialogBackground", dark ? "#272727" : "#F3F3F3");
        SetBrush(resources, "ContentDialogTopOverlay", dark ? "#323232" : "#FFFFFF");
        SetBrush(resources, "ContentDialogForeground", dark ? "#F2F2F2" : "#202020");
        SetBrush(resources, "ContentDialogBorderBrush", dark ? "#454545" : "#E6E6E6");
        SetBrush(resources, "ContentDialogSeparatorBorderBrush", dark ? "#454545" : "#E6E6E6");
        SetColor(resources, "AccentButtonDisabledStartColor", dark ? "#3A3A3A" : "#C0C0C0");
        SetColor(resources, "AccentButtonDisabledEndColor", dark ? "#303030" : "#B7B7B7");
    }

    private static void SetBrush(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }

    private static void SetColor(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = (Color)ColorConverter.ConvertFromString(hex)!;
    }

    private static void SetShadow(ResourceDictionary resources, bool dark)
    {
        resources["FlyoutShadowEffect"] = new DropShadowEffect
        {
            BlurRadius = 20,
            ShadowDepth = 8,
            Direction = 270,
            Opacity = dark ? 0.38 : 0.14,
            Color = dark ? Colors.Black : Color.FromRgb(0x20, 0x20, 0x20)
        };
    }

    private static void SetWpfUiSurfacePalette(
        ResourceDictionary resources,
        string baseSurface,
        string secondarySurface,
        string tertiarySurface,
        string controlDefault,
        string controlSecondary,
        string controlTertiary,
        string controlDisabled,
        string primaryText,
        string secondaryText,
        string tertiaryText)
    {
        SetBrush(resources, "SolidBackgroundFillColorBaseBrush", baseSurface);
        SetBrush(resources, "SolidBackgroundFillColorSecondaryBrush", secondarySurface);
        SetBrush(resources, "SolidBackgroundFillColorTertiaryBrush", tertiarySurface);
        SetBrush(resources, "ControlFillColorDefaultBrush", controlDefault);
        SetBrush(resources, "ControlFillColorSecondaryBrush", controlSecondary);
        SetBrush(resources, "ControlFillColorTertiaryBrush", controlTertiary);
        SetBrush(resources, "ControlFillColorDisabledBrush", controlDisabled);
        SetBrush(resources, "TextFillColorPrimaryBrush", primaryText);
        SetBrush(resources, "TextFillColorSecondaryBrush", secondaryText);
        SetBrush(resources, "TextFillColorTertiaryBrush", tertiaryText);
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
