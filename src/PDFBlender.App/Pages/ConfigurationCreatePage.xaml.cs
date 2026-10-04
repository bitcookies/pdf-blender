using System.Windows;
using System.Windows.Controls;
using TextBlock = System.Windows.Controls.TextBlock;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace PDFBlender.App.Pages;

public partial class ConfigurationCreatePage : Page
{
    private readonly Action _custom;
    private readonly Action _preset;
    private readonly Action _back;

    internal ConfigurationCreatePage(AppSession session, Action custom, Action preset, Action back)
    {
        _custom = custom;
        _preset = preset;
        _back = back;
        InitializeComponent();
        RefreshText();
    }

    private void RefreshText()
    {
        BreadcrumbBar.Items.Clear();
        BreadcrumbBar.Items.Add(new BreadcrumbBarItem { Content = UiText.Get("configuration"), Tag = "root" });
        BreadcrumbBar.Items.Add(new BreadcrumbBarItem { Content = UiText.Get("newConfiguration"), IsLast = true, Tag = "new" });
        TitleText.Text = UiText.Get("newConfiguration");
        SubtitleText.Text = UiText.Get("newConfigurationHint");
        CustomButton.Content = ChoiceContent("BookAdd24", UiText.Get("newCustomConfiguration"), UiText.Get("customConfigurationDescription"));
        PresetButton.Content = ChoiceContent("BoxMultiple24", UiText.Get("newPresetConfiguration"), UiText.Get("presetConfigurationDescription"));
    }

    private static StackPanel ChoiceContent(string symbol, string title, string description) => new()
    {
        VerticalAlignment = VerticalAlignment.Center,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        Children =
        {
            new SymbolIcon { Symbol = Enum.Parse<SymbolRegular>(symbol), FontSize = 42, Foreground = (Brush)Application.Current.FindResource("AccentBrush"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 9) },
            new TextBlock { Text = title, FontSize = 15, FontWeight = FontWeights.SemiBold, TextAlignment = TextAlignment.Center, Foreground = (Brush)Application.Current.FindResource("TextBrush") },
            new TextBlock { Text = description, FontSize = 11, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, Foreground = (Brush)Application.Current.FindResource("MutedTextBrush"), Margin = new Thickness(12, 7, 12, 0) }
        }
    };

    private void BreadcrumbBar_ItemClicked(object sender, BreadcrumbBarItemClickedEventArgs e)
    {
        if (e.Index == 0) _back();
    }

    private void CustomButton_Click(object sender, RoutedEventArgs e) => _custom();
    private void PresetButton_Click(object sender, RoutedEventArgs e) => _preset();
}
