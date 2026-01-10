using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace App.ViewModels;

public partial class console_logs_view_model : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<console_log_item_view_model> _logs = new();

    [ObservableProperty]
    private bool _hasLogs;

    public console_logs_view_model()
    {
        // Start with empty logs
    }

    public void add_log(string message, string level = "log")
    {
        Logs.Add(new console_log_item_view_model
        {
            Message = message,
            Level = level,
            Timestamp = DateTime.Now
        });
        HasLogs = Logs.Count > 0;
    }

    public void add_logs(IEnumerable<string> messages)
    {
        foreach (var message in messages)
        {
            add_log(message);
        }
    }

    [RelayCommand]
    public void ClearLogs()
    {
        Logs.Clear();
        HasLogs = false;
    }

    public void clear_logs() => ClearLogs();
}

public partial class console_log_item_view_model : ObservableObject
{
    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private string _level = "log";

    [ObservableProperty]
    private DateTime _timestamp;

    public string LevelIcon => Level switch
    {
        "error" => "✗",
        "warn" => "⚠",
        "info" => "ℹ",
        _ => "›"
    };

    public string LevelColor => Level switch
    {
        "error" => "#F44336",
        "warn" => "#FFC107",
        "info" => "#2196F3",
        _ => "#888888"
    };
}
