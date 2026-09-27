using System.Windows;
using System.Windows.Controls;

namespace PDFBlender.App;

public partial class UiNotificationHost : UserControl
{
    private const int MaximumVisibleNotifications = 3;
    private readonly Dictionary<Guid, UiNotificationCard> _cards = [];

    public UiNotificationHost() => InitializeComponent();

    public void Show(Guid id, UiNotificationKind kind, string title, string message, bool autoDismiss)
    {
        if (_cards.TryGetValue(id, out var existing))
        {
            existing.Present(id, kind, title, message, autoDismiss);
            return;
        }
        MakeRoom();
        var card = new UiNotificationCard { Margin = new Thickness(0, 0, 0, 8) };
        card.DismissRequested += CardDismissRequested;
        _cards[id] = card;
        NotificationPanel.Children.Insert(0, card);
        card.Present(id, kind, title, message, autoDismiss);
    }

    public void UpdateProgress(Guid id, string message, double fraction)
    {
        if (_cards.TryGetValue(id, out var card)) card.UpdateProgress(message, fraction);
    }

    public void Dismiss(Guid id)
    {
        if (_cards.TryGetValue(id, out var card)) card.Dismiss();
    }

    public void Clear()
    {
        foreach (var card in _cards.Values) { card.DismissRequested -= CardDismissRequested; card.Dispose(); }
        _cards.Clear();
        NotificationPanel.Children.Clear();
    }

    private void MakeRoom()
    {
        if (_cards.Count < MaximumVisibleNotifications) return;
        var card = NotificationPanel.Children.OfType<UiNotificationCard>().LastOrDefault(candidate => !candidate.IsPersistent)
                   ?? NotificationPanel.Children.OfType<UiNotificationCard>().LastOrDefault();
        if (card is not null) RemoveCard(card);
    }

    private void CardDismissRequested(object? sender, EventArgs e)
    {
        if (sender is UiNotificationCard card) RemoveCard(card);
    }

    private void RemoveCard(UiNotificationCard card)
    {
        card.DismissRequested -= CardDismissRequested;
        _cards.Remove(card.Id);
        NotificationPanel.Children.Remove(card);
        card.Dispose();
    }
}
