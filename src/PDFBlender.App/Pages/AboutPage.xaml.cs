using System.Reflection;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows;

namespace PDFBlender.App.Pages;

public partial class AboutPage : Page
{
    private const string ProjectUrl = "https://github.com/bitcookies/pdf-blender";

    public AboutPage()
    {
        InitializeComponent();
        RefreshLanguage();
    }

    public void RefreshLanguage()
    {
        ProductName.Text = UiText.Get("appName");
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3) ?? "0.5.0";
        VersionText.Text = $"v{version}";
        TechnologyText.Text = UiText.Get("technology");
        ProjectHeader.Text = UiText.Get("projectHeader");
        ProjectDescriptionText.Text = UiText.Get("projectDescriptionAbout");
        GithubButtonText.Text = UiText.Get("githubProject");
        FeedbackButtonText.Text = UiText.Get("feedback");
        FeedbackHintText.Text = UiText.Get("feedbackHint");
        PrivacyHeader.Text = UiText.Get("privacyHeader");
        LocalProcessingText.Text = UiText.Get("localProcessingAbout");
        StoreText.Text = UiText.Get("storeAbout");
        LegalHeader.Text = UiText.Get("legal");
        CopyrightText.Text = UiText.Get("copyrightAbout");
        LicenseText.Text = UiText.Get("licenseAbout");
        ThirdPartyHeader.Text = UiText.Get("thirdPartyHeader");
        ThirdPartyText.Text = UiText.Get("thirdPartyAbout");
        FeedbackButton.IsEnabled = false;
    }

    private void GithubButton_Click(object sender, RoutedEventArgs e) => OpenLink(ProjectUrl);

    private void FeedbackButton_Click(object sender, RoutedEventArgs e) => OpenLink(ProjectUrl);

    private static void OpenLink(string url)
    {
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }
}
