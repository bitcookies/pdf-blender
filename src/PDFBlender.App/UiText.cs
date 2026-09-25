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
        ["appName"] = "PDF Blender",
        ["home"] = "Home",
        ["configuration"] = "Configuration",
        ["about"] = "About",
        ["subtitle"] = "Make reuse harder. Keep reading easy.",
        ["dropTitle"] = "Drop a PDF into the blender",
        ["dropHint"] = "or browse for a file on this PC",
        ["browse"] = "Browse PDF",
        ["blend"] = "Blend PDF",
        ["ready"] = "Ready for a PDF",
        ["settings"] = "Settings",
        ["theme"] = "Theme",
        ["language"] = "Language",
        ["system"] = "System",
        ["light"] = "Light",
        ["dark"] = "Dark",
        ["themeHint"] = "Choose how the interface follows your desktop.",
        ["languageHint"] = "Choose the language used by this app.",
        ["profileDefault"] = "Default",
        ["profileResearch"] = "Research report",
        ["profilePublic"] = "Public handout",
        ["selected"] = "Selected",
        ["cancel"] = "Cancel",
        ["processing"] = "Blending your PDF...",
        ["completed"] = "Completed",
        ["cancelled"] = "Cancelled",
        ["processingError"] = "Could not blend the PDF",
        ["settingsSubtitle"] = "Choose the language and theme used by PDF Blender.",
        ["configurationSubtitle"] = "Customize the blend settings used by each level.",
        ["configurationName"] = "Configuration",
        ["configurationNameHint"] = "Keep separate settings for different document types.",
        ["configurationLevelHint"] = "Select the level whose settings you want to edit.",
        ["appearance"] = "Appearance",
        ["format"] = "Format",
        ["image"] = "Image",
        ["blendLevel"] = "Blend level",
        ["stirSummary"] = "Light formatting touch",
        ["blendSummary"] = "Balanced everyday blend",
        ["pulseSummary"] = "Stronger visual signature",
        ["normalizeWidth"] = "Normalize content width",
        ["normalizeHint"] = "Keep page widths consistent while preserving height ratio.",
        ["targetWidth"] = "Target width (mm)",
        ["targetWidthHint"] = "Scales visual content proportionally; it does not force A4 or equal page heights.",
        ["protection"] = "Primary protection",
        ["protectionMethod"] = "Protection method",
        ["protectionHint"] = "Flattening turns pages into visual surfaces for easier sharing.",
        ["watermark"] = "Watermark",
        ["watermarkType"] = "Watermark type",
        ["watermarkTypeHint"] = "Add a visible ownership signal to the output.",
        ["watermarkText"] = "Watermark text",
        ["noise"] = "Pixel noise",
        ["noiseHint"] = "Subtle image variation raises automated matching cost.",
        ["invalidSettings"] = "Please enter a valid width and watermark text.",
        ["none"] = "None",
        ["flatten"] = "Flatten",
        ["text"] = "Text",
        ["fullPage"] = "Full page",
        ["off"] = "Off",
        ["low"] = "Low",
        ["medium"] = "Medium",
        ["high"] = "High",
        ["technology"] = ".NET 10 · WPF · WPF-UI",
        ["localProcessingAbout"] = "PDF Blender processes files locally. Your document content is not uploaded or sent to a service.",
        ["storeAbout"] = "Distributed through Microsoft Store. Updates are delivered by the Store.",
        ["legal"] = "Legal and acknowledgements",
        ["licenseAbout"] = "PDF Blender is provided under the project license included with this installation.",
        ["thirdPartyAbout"] = "Third-party notices are included in THIRD-PARTY-NOTICES.md."
    };

    private static readonly IReadOnlyDictionary<string, string> Chinese = new Dictionary<string, string>
    {
        ["appName"] = "PDF Blender",
        ["home"] = "主页",
        ["configuration"] = "配置",
        ["about"] = "关于",
        ["subtitle"] = "提高复用成本，让阅读保持轻松。",
        ["dropTitle"] = "把 PDF 放进搅拌机",
        ["dropHint"] = "或从这台电脑中选择文件",
        ["browse"] = "选择 PDF",
        ["blend"] = "搅拌 PDF",
        ["ready"] = "等待 PDF",
        ["settings"] = "设置",
        ["theme"] = "主题",
        ["language"] = "语言",
        ["system"] = "跟随系统",
        ["light"] = "浅色",
        ["dark"] = "深色",
        ["themeHint"] = "选择界面如何跟随系统桌面。",
        ["languageHint"] = "选择此应用使用的语言。",
        ["profileDefault"] = "默认",
        ["profileResearch"] = "研究报告",
        ["profilePublic"] = "公开资料",
        ["selected"] = "已选择",
        ["cancel"] = "取消",
        ["processing"] = "正在搅拌 PDF...",
        ["completed"] = "已完成",
        ["cancelled"] = "已取消",
        ["processingError"] = "PDF 搅拌失败",
        ["settingsSubtitle"] = "选择 PDF Blender 使用的语言和主题。",
        ["configurationSubtitle"] = "自定义每个挡位使用的搅拌设置。",
        ["configurationName"] = "配置",
        ["configurationNameHint"] = "为不同类型的文档保留不同设置。",
        ["configurationLevelHint"] = "选择要编辑的搅拌挡位。",
        ["appearance"] = "外观",
        ["format"] = "格式",
        ["image"] = "图像",
        ["blendLevel"] = "搅拌挡位",
        ["stirSummary"] = "轻量格式处理",
        ["blendSummary"] = "日常均衡搅拌",
        ["pulseSummary"] = "更强的视觉标记",
        ["normalizeWidth"] = "统一内容宽度",
        ["normalizeHint"] = "保持页面宽度一致，同时保留高度比例。",
        ["targetWidth"] = "目标宽度（毫米）",
        ["targetWidthHint"] = "按比例缩放视觉内容，不会强制 A4 或统一页面高度。",
        ["protection"] = "主要保护方式",
        ["protectionMethod"] = "保护方式",
        ["protectionHint"] = "扁平化会将页面转换为视觉表面，便于公开分享。",
        ["watermark"] = "水印",
        ["watermarkType"] = "水印类型",
        ["watermarkTypeHint"] = "为输出添加可见的归属提示。",
        ["watermarkText"] = "水印文字",
        ["noise"] = "像素噪点",
        ["noiseHint"] = "细微图像变化可以提高自动匹配的成本。",
        ["invalidSettings"] = "请输入有效的宽度和水印文字。",
        ["none"] = "关闭",
        ["flatten"] = "扁平化",
        ["text"] = "文字",
        ["fullPage"] = "整页",
        ["off"] = "关闭",
        ["low"] = "低",
        ["medium"] = "中",
        ["high"] = "高",
        ["technology"] = ".NET 10 · WPF · WPF-UI",
        ["localProcessingAbout"] = "PDF Blender 在本机处理文件，不会上传或发送文档内容。",
        ["storeAbout"] = "通过 Microsoft Store 发布，更新由 Store 自动提供。",
        ["legal"] = "法律与致谢",
        ["licenseAbout"] = "PDF Blender 遵循项目中随附的许可证发布。",
        ["thirdPartyAbout"] = "第三方声明位于 THIRD-PARTY-NOTICES.md。"
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
