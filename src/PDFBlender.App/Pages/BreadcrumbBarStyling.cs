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
    private static readonly Brush NormalBrush = CreateBrush(0x33, 0x33, 0x33);
    private static readonly Brush HoverBrush = CreateBrush(0x90, 0x90, 0x90);

    public static void Attach(BreadcrumbBar breadcrumbBar)
    {
        breadcrumbBar.Loaded += (_, _) =>
            breadcrumbBar.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => Apply(breadcrumbBar)));
    }

    private static void Apply(BreadcrumbBar breadcrumbBar)
    {
        var labels = FindVisualChildren<WpfTextBlock>(breadcrumbBar)
            .Where(label => !string.IsNullOrWhiteSpace(label.Text) && FindVisualParent<ButtonBase>(label) is not null)
            .ToList();

        foreach (var label in labels)
        {
            label.Foreground = NormalBrush;
            label.MouseEnter -= Label_MouseEnter;
            label.MouseLeave -= Label_MouseLeave;
            label.MouseEnter += Label_MouseEnter;
            label.MouseLeave += Label_MouseLeave;
        }

        if (labels.Count != 0)
            return;

        foreach (var button in FindVisualChildren<ButtonBase>(breadcrumbBar))
        {
            button.Foreground = NormalBrush;
            button.MouseEnter -= Button_MouseEnter;
            button.MouseLeave -= Button_MouseLeave;
            button.MouseEnter += Button_MouseEnter;
            button.MouseLeave += Button_MouseLeave;
        }
    }

    private static void Label_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is WpfTextBlock label)
            label.Foreground = HoverBrush;
    }

    private static void Label_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is WpfTextBlock label)
            label.Foreground = NormalBrush;
    }

    private static void Button_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Control button)
            button.Foreground = HoverBrush;
    }

    private static void Button_MouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is Control button)
            button.Foreground = NormalBrush;
    }

    private static Brush CreateBrush(byte red, byte green, byte blue)
    {
        var brush = new SolidColorBrush(Color.FromRgb(red, green, blue));
        brush.Freeze();
        return brush;
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