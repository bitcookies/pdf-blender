using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Wpf.Ui.Controls;

namespace PDFBlender.App;

public partial class UiNotificationCard : UserControl, IDisposable
{
    private readonly DispatcherTimer _dismissTimer = new();
    private TimeSpan _remaining;
    private Stopwatch _visibleTime = new();
    private bool _dismissing;
    private UiNotificationKind _kind;
    private Guid _id;

    public UiNotificationCard()
    {
        InitializeComponent();
        _dismissTimer.Tick += (_, _) => { _dismissTimer.Stop(); Dismiss(); };
    }

    public event EventHandler? DismissRequested;
    public Guid Id => _id;
    public bool IsPersistent => _kind == UiNotificationKind.Progress || _remaining == TimeSpan.Zero;

    public void Present(Guid id, UiNotificationKind kind, string title, string message, bool autoDismiss)
    {
        _id = id;
        _kind = kind;
        _dismissing = false;
        TitleText.Text = title;
        MessageText.Text = message;
        MessageText.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
        CloseButton.Visibility = kind == UiNotificationKind.Progress ? Visibility.Collapsed : Visibility.Visible;
        NotificationProgress.Visibility = kind == UiNotificationKind.Progress ? Visibility.Visible : Visibility.Collapsed;
        NotificationProgress.Value = 0;
        var visual = Visuals(kind);
        IntentIcon.Symbol = visual.Icon;
        IntentIcon.SetResourceReference(Control.ForegroundProperty, visual.BrushKey);
        IntentBar.SetResourceReference(Border.BackgroundProperty, visual.BrushKey);
        _remaining = autoDismiss ? Duration(kind) : TimeSpan.Zero;
        Opacity = 0;
        var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(140)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
        fadeIn.Completed += (_, _) => StartTimer();
        BeginAnimation(OpacityProperty, fadeIn);
    }

    public void UpdateProgress(string message, double fraction)
    {
        MessageText.Text = message;
        NotificationProgress.Value = Math.Clamp(fraction, 0, 1) * 100;
        NotificationProgress.Visibility = Visibility.Visible;
    }

    public void Dismiss()
    {
        if (_dismissing) return;
        _dismissing = true;
        _dismissTimer.Stop();
        var fadeOut = new DoubleAnimation(Opacity, 0, TimeSpan.FromMilliseconds(120));
        fadeOut.Completed += (_, _) => DismissRequested?.Invoke(this, EventArgs.Empty);
        BeginAnimation(OpacityProperty, fadeOut);
    }

    public void Dispose()
    {
        _dismissTimer.Stop();
        BeginAnimation(OpacityProperty, null);
    }

    private void StartTimer()
    {
        if (IsPersistent || IsMouseOver) return;
        _visibleTime.Restart();
        _dismissTimer.Interval = _remaining;
        _dismissTimer.Start();
    }

    private void CardMouseEnter(object sender, MouseEventArgs e)
    {
        if (_dismissTimer.IsEnabled)
        {
            _dismissTimer.Stop();
            _remaining = _remaining > _visibleTime.Elapsed ? _remaining - _visibleTime.Elapsed : TimeSpan.Zero;
        }
    }

    private void CardMouseLeave(object sender, MouseEventArgs e) => StartTimer();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Dismiss();

    private static TimeSpan Duration(UiNotificationKind kind) => kind switch
    {
        UiNotificationKind.Success => TimeSpan.FromSeconds(2.4),
        UiNotificationKind.Info => TimeSpan.FromSeconds(3.5),
        UiNotificationKind.Warning => TimeSpan.FromSeconds(5),
        UiNotificationKind.Error => TimeSpan.FromSeconds(6),
        _ => TimeSpan.Zero
    };

    private static (string BrushKey, SymbolRegular Icon) Visuals(UiNotificationKind kind) => kind switch
    {
        UiNotificationKind.Success => ("NotificationSuccessBrush", SymbolRegular.CheckmarkCircle24),
        UiNotificationKind.Info => ("NotificationInfoBrush", SymbolRegular.Info24),
        UiNotificationKind.Warning => ("NotificationWarningBrush", SymbolRegular.Warning24),
        UiNotificationKind.Error => ("NotificationErrorBrush", SymbolRegular.ErrorCircle24),
        UiNotificationKind.Progress => ("AccentBrush", SymbolRegular.ArrowSync24),
        _ => ("NotificationMessageBrush", SymbolRegular.Chat24)
    };
}
