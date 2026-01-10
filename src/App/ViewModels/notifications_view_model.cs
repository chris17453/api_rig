using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels;

public partial class notifications_view_model : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<notification_item> _notifications = new();

    [ObservableProperty]
    private bool _isOpen;

    public int UnreadCount => Notifications.Count(n => !n.IsRead);
    public bool HasUnread => UnreadCount > 0;

    [RelayCommand]
    private void Toggle()
    {
        IsOpen = !IsOpen;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    [RelayCommand]
    private void MarkAsRead(notification_item? notification)
    {
        if (notification != null)
        {
            notification.IsRead = true;
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnread));
        }
    }

    [RelayCommand]
    private void MarkAllAsRead()
    {
        foreach (var notification in Notifications)
        {
            notification.IsRead = true;
        }
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasUnread));
    }

    [RelayCommand]
    private void Dismiss(notification_item? notification)
    {
        if (notification != null)
        {
            Notifications.Remove(notification);
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnread));
        }
    }

    [RelayCommand]
    private void ClearAll()
    {
        Notifications.Clear();
        OnPropertyChanged(nameof(UnreadCount));
        OnPropertyChanged(nameof(HasUnread));
    }

    public void AddNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        var notification = new notification_item
        {
            Title = title,
            Message = message,
            Type = type,
            Timestamp = DateTime.Now
        };

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            Notifications.Insert(0, notification);
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnread));
        });
    }

    public void NotifyRequestSuccess(string requestName, int statusCode, long elapsedMs)
    {
        AddNotification(
            "Request Completed",
            $"{requestName}: {statusCode} ({elapsedMs}ms)",
            NotificationType.Success);
    }

    public void NotifyRequestError(string requestName, string error)
    {
        AddNotification(
            "Request Failed",
            $"{requestName}: {error}",
            NotificationType.Error);
    }

    public void NotifyScriptError(string scriptType, string error)
    {
        AddNotification(
            $"{scriptType} Script Error",
            error,
            NotificationType.Error);
    }

    public void NotifyTestFailure(string testName, string error)
    {
        AddNotification(
            "Test Failed",
            $"{testName}: {error}",
            NotificationType.Warning);
    }
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

public partial class notification_item : ObservableObject
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private NotificationType _type = NotificationType.Info;

    [ObservableProperty]
    private DateTime _timestamp = DateTime.Now;

    [ObservableProperty]
    private bool _isRead;

    public string TypeIcon => Type switch
    {
        NotificationType.Info => "i",
        NotificationType.Success => "check",
        NotificationType.Warning => "!",
        NotificationType.Error => "x",
        _ => "i"
    };

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.Now - Timestamp;
            if (diff.TotalSeconds < 60) return "just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return Timestamp.ToString("MMM d");
        }
    }
}
