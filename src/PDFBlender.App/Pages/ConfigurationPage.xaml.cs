using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using PDFBlender.Core.Models;
using UiCard = Wpf.Ui.Controls.CardControl;
using UiButton = Wpf.Ui.Controls.Button;
using ControlAppearance = Wpf.Ui.Controls.ControlAppearance;

namespace PDFBlender.App.Pages;

public partial class ConfigurationPage : Page
{
    private readonly AppSession _session;
    private readonly DialogService _dialogs;
    private Point _dragStart;

    internal ConfigurationPage(AppSession session, DialogService dialogs)
    {
        _session = session;
        _dialogs = dialogs;
        InitializeComponent();
        RefreshFromSession();
    }

    public void RefreshFromSession()
    {
        UiText.SetLanguage(_session.Preferences.Language);
        TitleText.Text = UiText.Get("configuration");
        SubtitleText.Text = UiText.Get("configurationSubtitle");
        SlotsHeader.Text = UiText.Get("blendSlots");
        SlotsHint.Text = UiText.Get("blendSlotsHint");
        ConfigurationBlendLevelRail.SetCaption(UiText.Get("blendLevel"));
        ConfigurationBlendLevelRail.SetAccessibilityName(UiText.Get("blendLevel"));
        ConfigurationBlendLevelRail.SetEditAccessibilityName(UiText.Get("editBlendLevel"));
        ConfigurationBlendLevelRail.SetLevels(BuildLevelItems());
        ConfigurationBlendLevelRail.SelectedIndex = _session.SelectedBlendLevel;
        ConfigurationsHeader.Text = UiText.Get("blendConfigurations");
        AddButtonText.Text = UiText.Get("addConfiguration");
        EmptyText.Text = _session.AllConfigurations.Count == 0 ? UiText.Get("noConfigurations") : string.Empty;
        ErrorText.Text = string.Empty;
        BuildSlots();
        BuildCards(_session.AllConfigurations);
    }

    private void BuildSlots()
    {
        SlotsPanel.Children.Clear();
        SlotsPanel.ColumnDefinitions.Clear();
        for (var index = 0; index < 5; index++) SlotsPanel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        for (var index = 0; index < 5; index++)
        {
            var slot = CreateSlot(_session.SlotConfigurations[index], index);
            Grid.SetColumn(slot, index);
            SlotsPanel.Children.Add(slot);
        }
    }

    private UiCard CreateSlot(BlendConfiguration? configuration, int index)
    {
        var card = new UiCard
        {
            Margin = new Thickness(index == 0 ? 0 : 5, 0, index == 4 ? 0 : 5, 0), Padding = new Thickness(9, 8, 9, 8), MinHeight = 72,
            Background = (Brush)FindResource(configuration is null ? "SurfaceMutedBrush" : "CardBrush"), BorderBrush = (Brush)FindResource(configuration is null ? "BorderBrush" : "AccentBrush"),
            BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(9), AllowDrop = true, Tag = index
        };
        var root = new StackPanel();
        var header = new DockPanel();
        if (configuration is not null)
        {
            var remove = MakeIconAction("Dismiss16", UiText.Get("remove"), true);
            remove.Padding = new Thickness(2);
            remove.Margin = new Thickness(0);
            remove.Click += (_, _) => _session.RemoveFromSlots(configuration.Id);
            DockPanel.SetDock(remove, Dock.Right);
            header.Children.Add(remove);
        }
        header.Children.Add(new TextBlock { Text = $"{index + 1}", Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 10, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        root.Children.Add(header);
        root.Children.Add(new TextBlock { Text = configuration?.Name ?? UiText.Get("emptySlot"), FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource(configuration is null ? "MutedTextBrush" : "TextBrush"), TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(0, 4, 0, 0) });
        if (configuration is not null) root.Children.Add(new TextBlock { Text = $"{UiText.Get("intensity")} {configuration.Intensity}", FontSize = 10, Foreground = IntensityBrush(configuration.Intensity), Margin = new Thickness(0, 3, 0, 0) });
        card.Content = root;
        card.DragOver += Slot_DragOver;
        card.DragLeave += (_, _) => ResetSlotVisual(card);
        card.Drop += Slot_Drop;
        if (configuration is not null)
        {
            card.MouseLeftButtonDown += (_, e) => _dragStart = e.GetPosition(card);
            card.MouseMove += (_, e) =>
            {
                if (e.LeftButton == MouseButtonState.Pressed && (e.GetPosition(card) - _dragStart).Length >= SystemParameters.MinimumHorizontalDragDistance)
                {
                    card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.58, TimeSpan.FromMilliseconds(120)));
                    DragDrop.DoDragDrop(card, configuration.Id, DragDropEffects.Move);
                    card.BeginAnimation(OpacityProperty, null);
                    card.Opacity = 1;
                }
            };
        }
        return card;
    }

    private void BuildCards(IEnumerable<BlendConfiguration> configurations)
    {
        ConfigurationCardsPanel.Children.Clear();
        foreach (var configuration in configurations)
            ConfigurationCardsPanel.Children.Add(CreateCard(configuration));
    }

    private UiCard CreateCard(BlendConfiguration configuration)
    {
        var card = new UiCard { Margin = new Thickness(0, 0, 8, 8), Padding = new Thickness(12, 10, 10, 10), MinHeight = 126, Background = (Brush)FindResource("CardBrush"), BorderBrush = IntensityBrush(configuration.Intensity), BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(8), ToolTip = UiText.Get("dragToSlots") };
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var details = new StackPanel();
        details.Children.Add(new TextBlock { Text = configuration.Name, FontSize = 14, FontWeight = FontWeights.SemiBold });
        details.Children.Add(new TextBlock { Text = FeatureSummary(configuration), Foreground = (Brush)FindResource("MutedTextBrush"), FontSize = 11, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 12, 0) });
        details.Children.Add(CreateIntensityBar(configuration.Intensity));
        Grid.SetColumn(details, 0); grid.Children.Add(details);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        var edit = MakeIconAction("Edit24", UiText.Get("edit"), !configuration.IsLocked); edit.Click += (_, _) => OpenEditor(configuration, false);
        var lockButton = MakeIconAction(configuration.IsLocked ? "LockClosed24" : "LockOpen24", configuration.IsLocked ? UiText.Get("unlock") : UiText.Get("lock"), true); lockButton.Click += (_, _) => _session.SetConfigurationLocked(configuration.Id, !configuration.IsLocked);
        var delete = MakeIconAction("Delete24", UiText.Get("delete"), !configuration.IsLocked);
        delete.Click += async (_, _) =>
        {
            if (configuration.IsLocked) return;
            var confirmed = await _dialogs.ShowConfirmationAsync(
                UiText.Get("delete"),
                UiText.Get("deleteConfigurationConfirm"),
                UiText.Get("delete"),
                UiText.Get("cancel"),
                Wpf.Ui.Controls.SymbolRegular.Delete24);
            if (confirmed) _session.DeleteConfiguration(configuration.Id);
        };
        actions.Children.Add(edit); actions.Children.Add(lockButton); actions.Children.Add(delete); Grid.SetColumn(actions, 1); grid.Children.Add(actions); card.Content = grid;
        card.MouseLeftButtonDown += (_, e) => _dragStart = e.GetPosition(card);
        card.MouseMove += (_, e) =>
        {
            if (e.LeftButton != MouseButtonState.Pressed || (e.GetPosition(card) - _dragStart).Length < SystemParameters.MinimumHorizontalDragDistance) return;
            card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.58, TimeSpan.FromMilliseconds(120)));
            DragDrop.DoDragDrop(card, configuration.Id, DragDropEffects.Move);
            card.BeginAnimation(OpacityProperty, null);
            card.Opacity = 1;
        };
        return card;
    }

    private void OpenEditor(BlendConfiguration? source, bool isNew)
    {
        var initial = source ?? new BlendConfiguration("new", UiText.Get("configurationName"), false, new BlendSettings { NormalizeContentWidth = true });
        var dialog = new ConfigurationEditorWindow(initial) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;
        if (isNew) _session.AddUserConfiguration(dialog.ConfigurationName, dialog.Settings);
        else if (!_session.RenameConfiguration(initial.Id, dialog.ConfigurationName) || !_session.UpdateConfiguration(initial.Id, dialog.Settings))
            ErrorText.Text = UiText.Get("configurationUpdateFailed");
    }

    private void AddButton_Click(object sender, RoutedEventArgs e) => OpenEditor(null, true);
    private void ConfigurationBlendLevelRail_SelectionChanged(object? sender, BlendLevelChangedEventArgs e) => _session.SetBlendLevel(e.SelectedIndex);
    private void Slot_DragOver(object sender, DragEventArgs e) { if (sender is UiCard card) { card.Opacity = 0.82; card.BorderBrush = (Brush)FindResource("AccentDarkBrush"); } e.Effects = e.Data.GetDataPresent(DataFormats.StringFormat) ? DragDropEffects.Move : DragDropEffects.None; e.Handled = true; }
    private void Slot_Drop(object sender, DragEventArgs e) { if (sender is UiCard card && card.Tag is int target && e.Data.GetData(DataFormats.StringFormat) is string id) _session.AddToSlots(id, target); if (sender is UiCard dropped) ResetSlotVisual(dropped); e.Handled = true; }
    private void ResetSlotVisual(UiCard card) { card.Opacity = 1; card.BorderBrush = (Brush)FindResource(_session.SlotConfigurations.ElementAtOrDefault((int)card.Tag!) is null ? "BorderBrush" : "AccentBrush"); }
    private UiButton MakeIconAction(string symbol, string tooltip, bool enabled)
    {
        var button = new UiButton { Content = new Wpf.Ui.Controls.SymbolIcon { Symbol = Enum.Parse<Wpf.Ui.Controls.SymbolRegular>(symbol), FontSize = 16 }, ToolTip = tooltip, IsEnabled = enabled, Appearance = ControlAppearance.Transparent, Padding = new Thickness(7), Margin = new Thickness(2, 0, 0, 0) };
        AutomationProperties.SetName(button, tooltip);
        return button;
    }
    private Border CreateIntensityBar(int intensity) => new() { Height = 4, Background = (Brush)FindResource("BorderBrush"), CornerRadius = new CornerRadius(2), Child = new Border { Width = Math.Max(4, 140 * (intensity + 1) / 10d), HorizontalAlignment = HorizontalAlignment.Left, Background = IntensityBrush(intensity), CornerRadius = new CornerRadius(2) } };
    private Brush IntensityBrush(int intensity) { var stops = new[] { Colors.ForestGreen, Colors.YellowGreen, Colors.Gold, Colors.Orange, Colors.OrangeRed, Colors.Firebrick }; return new SolidColorBrush(stops[(int)Math.Round(Math.Clamp(intensity, 0, 9) / 9d * (stops.Length - 1))]); }
    private string FeatureSummary(BlendConfiguration configuration) => string.Join(" · ", configuration.EnabledFeatures.Select(feature => feature switch { BlendFeature.NormalizeContentWidth => UiText.Get("normalizeWidth"), BlendFeature.Flatten => UiText.Get("flatten"), BlendFeature.Watermark => UiText.Get("watermark"), BlendFeature.PixelNoise => UiText.Get("noise"), _ => feature.ToString() }).DefaultIfEmpty(UiText.Get("noFeatures")));
    private IReadOnlyList<BlendLevelItem> BuildLevelItems() => _session.SlotConfigurations.Select((configuration, index) => configuration is null ? new BlendLevelItem($"{index + 1}", string.Empty, false) : new BlendLevelItem(configuration.Name, $"{UiText.Get("intensity")} {configuration.Intensity}")).ToList();
}
