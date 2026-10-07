using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Wpf.Ui.Controls;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace PDFBlender.App.Pages;

internal static class BreadcrumbBarStyling
{
    private const string NormalResource = "BreadcrumbBarNormalForegroundBrush";
    private const string HoverResource = "BreadcrumbBarHoverForegroundBrush";

    public static void Attach(BreadcrumbBar breadcrumbBar)
    {
        breadcrumbBar.Loaded += (_, _) =>
            breadcrumbBar.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Apply(breadcrumbBar)));
    }

    private static void Apply(BreadcrumbBar breadcrumbBar)
    {
        breadcrumbBar.SetResourceReference(Control.ForegroundProperty, NormalResource);
        var labels = FindVisualChildren<WpfTextBlock>(breadcrumbBar)
            .Where(label => !string.IsNullOrWhiteSpace(label.Text) && FindVisualParent<ButtonBase>(label) is not null)
            .ToList();

        foreach (var label in labels)
        {
            label.SetResourceReference(WpfTextBlock.ForegroundProperty, NormalResource);
            label.MouseEnter -= Label_MouseEnter;
            label.MouseLeave -= Label_MouseLeave;
            label.MouseEnter += Label_MouseEnter;
            label.MouseLeave += Label_MouseLeave;
        }

        if (labels.Count != 0)
            return;

        foreach (var button in FindVisualChildren<ButtonBase>(breadcrumbBar))
        {
            button.SetResourceReference(Control.ForegroundProperty, NormalResource);
            button.MouseEnter -= Button_MouseEnter;
            button.MouseLeave -= Button_MouseLeave;
            button.MouseEnter += Button_MouseEnter;
            button.MouseLeave += Button_MouseLeave;
        }
    }

    private static void Label_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is WpfTextBlock label)
            label.SetResourceReference(WpfTextBlock.ForegroundProperty, HoverResource);
    }

    private static void Label_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is WpfTextBlock label)
            label.SetResourceReference(WpfTextBlock.ForegroundProperty, NormalResource);
    }

    private static void Button_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Control button)
            button.SetResourceReference(Control.ForegroundProperty, HoverResource);
    }

    private static void Button_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Control button)
            button.SetResourceReference(Control.ForegroundProperty, NormalResource);
    }

    private static T? FindVisualParent<T>(DependencyObject node) where T : DependencyObject
    {
        var parent = VisualTreeHelper.GetParent(node);
        while (parent is not null)
        {
            if (parent is T match)
                return match;
            parent = VisualTreeHelper.GetParent(parent);
        }

        return null;
    }

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
        {
            var child = VisualTreeHelper.GetChild(root, index);
            if (child is T match)
                yield return match;

            foreach (var nested in FindVisualChildren<T>(child))
                yield return nested;
        }
    }
}