using System.Reflection;
using System.Windows.Controls;

namespace PDFBlender.App.Pages;

public partial class AboutPage : Page
{
    public AboutPage()
    {
        InitializeComponent();
        RefreshLanguage();
    }

    public void RefreshLanguage()
    {
        ProductName.Text = UiText.Get("appName");
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.4.0";
        VersionText.Text = $"v{version}";
        TechnologyText.Text = UiText.Get("technology");
        LocalProcessingText.Text = UiText.Get("localProcessingAbout");
        StoreText.Text = UiText.Get("storeAbout");
        LegalHeader.Text = UiText.Get("legal");
        LicenseText.Text = UiText.Get("licenseAbout");
        ThirdPartyText.Text = UiText.Get("thirdPartyAbout");
    }
}
