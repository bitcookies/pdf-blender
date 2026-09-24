using System.Globalization;

namespace PDFBlender.App;

public enum UiLanguage
{
    System,
    English,
    SimplifiedChinese
}

internal static class UiText
{
    private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>
    {
        ["subtitle"] = "Make reuse harder. Keep reading easy.",
        ["dropTitle"] = "Drop a PDF into the blender",
        ["dropHint"] = "or browse for a file on this PC",
        ["browse"] = "Browse PDF",
        ["profile"] = "Profile",
        ["output"] = "Output",
        ["outputHint"] = "The original file stays untouched. Processing remains local.",
        ["blend"] = "Blend PDF",
        ["ready"] = "Ready for a PDF",
        ["settings"] = "Settings",
        ["localProcessing"] = "Local processing",
        ["theme"] = "Theme",
        ["language"] = "Language",
        ["system"] = "System",
        ["light"] = "Light",
        ["dark"] = "Dark",
        ["profileDefault"] = "Default",
        ["profileResearch"] = "Research report",
        ["profilePublic"] = "Public handout",
        ["selected"] = "Selected",
        ["cancel"] = "Cancel",
        ["completed"] = "Completed",
        ["cancelled"] = "Cancelled",
        ["processingError"] = "Could not blend the PDF",
        ["outputInvalid"] = "Choose a valid output path.",
        ["settingsSaved"] = "Settings saved for this blend",
        ["advanced"] = "Settings",
        ["settingsSubtitle"] = "Tune appearance and processing preferences.",
        ["appearance"] = "Appearance",
        ["format"] = "Format",
        ["image"] = "Image",
        ["blendLevel"] = "Blend level",
        ["stirSummary"] = "Light formatting touch",
        ["blendSummary"] = "Balanced everyday blend",
        ["pulseSummary"] = "Stronger visual signature",
        ["normalizeWidth"] = "Normalize content width",
        ["targetWidth"] = "Target width (mm)",
        ["targetWidthHint"] = "Scales visual content proportionally; it does not force A4 or equal page heights.",
        ["protection"] = "Primary protection",
        ["watermark"] = "Watermark",
        ["watermarkText"] = "Watermark text",
        ["noise"] = "Pixel noise",
        ["save"] = "Save",
        ["close"] = "Cancel",
        ["invalidSettings"] = "Please enter a valid width and watermark text.",
        ["none"] = "None",
        ["flatten"] = "Flatten",
        ["text"] = "Text",
        ["fullPage"] = "Full page",
        ["off"] = "Off",
        ["low"] = "Low",
        ["medium"] = "Medium",
        ["high"] = "High"
    };

    private static readonly IReadOnlyDictionary<string, string> Chinese = new Dictionary<string, string>
    {
        ["subtitle"] = "提高复用成本，让阅读保持轻松。",
        ["dropTitle"] = "把 PDF 放进搅拌机",
        ["dropHint"] = "或从这台电脑中选择文件",
        ["browse"] = "选择 PDF",
        ["profile"] = "配置档",
        ["output"] = "输出",
        ["outputHint"] = "原文件不会被覆盖，处理始终在本机完成。",
        ["blend"] = "搅拌 PDF",
        ["ready"] = "等待 PDF",
        ["settings"] = "设置",
        ["localProcessing"] = "本地处理",
        ["theme"] = "主题",
        ["language"] = "语言",
        ["system"] = "跟随系统",
        ["light"] = "浅色",
        ["dark"] = "深色",
        ["profileDefault"] = "默认",
        ["profileResearch"] = "研究报告",
        ["profilePublic"] = "公开资料",
        ["selected"] = "已选择",
        ["cancel"] = "取消",
        ["completed"] = "已完成",
        ["cancelled"] = "已取消",
        ["processingError"] = "PDF 搅拌失败",
        ["outputInvalid"] = "请选择有效的输出路径。",
        ["settingsSaved"] = "本次搅拌的设置已保存",
        ["advanced"] = "设置",
        ["settingsSubtitle"] = "调整外观和处理偏好。",
        ["appearance"] = "外观",
        ["format"] = "格式",
        ["image"] = "图像",
        ["blendLevel"] = "搅拌挡位",
        ["stirSummary"] = "轻量格式处理",
        ["blendSummary"] = "日常均衡搅拌",
        ["pulseSummary"] = "更强的视觉标记",
        ["normalizeWidth"] = "统一内容宽度",
        ["targetWidth"] = "目标宽度（毫米）",
        ["targetWidthHint"] = "按比例缩放视觉内容，不会强制 A4 或统一页面高度。",
        ["protection"] = "主要保护方式",
        ["watermark"] = "水印",
        ["watermarkText"] = "水印文字",
        ["noise"] = "像素噪点",
        ["save"] = "保存",
        ["close"] = "取消",
        ["invalidSettings"] = "请输入有效的宽度和水印文字。",
        ["none"] = "关闭",
        ["flatten"] = "扁平化",
        ["text"] = "文字",
        ["fullPage"] = "整页",
        ["off"] = "关闭",
        ["low"] = "低",
        ["medium"] = "中",
        ["high"] = "高"
    };

    public static IReadOnlyDictionary<string, string> Current { get; private set; } = English;

    public static void SetLanguage(UiLanguage language)
    {
        Current = language switch
        {
            UiLanguage.SimplifiedChinese => Chinese,
            UiLanguage.System when CultureInfo.CurrentUICulture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) => Chinese,
            _ => English
        };
    }

    public static string Get(string key) => Current.TryGetValue(key, out var value) ? value : key;
}
