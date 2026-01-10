using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace App.ViewModels;

public partial class console_panel_view_model : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<console_log_entry> _logEntries = new();

    [ObservableProperty]
    private bool _showAll = true;

    [ObservableProperty]
    private bool _showInfo = true;

    [ObservableProperty]
    private bool _showWarnings = true;

    [ObservableProperty]
    private bool _showErrors = true;

    [ObservableProperty]
    private bool _showNetwork = true;

    [ObservableProperty]
    private string _searchFilter = string.Empty;

    public IEnumerable<console_log_entry> FilteredEntries => LogEntries
        .Where(e => MatchesFilter(e))
        .Where(e => string.IsNullOrEmpty(SearchFilter) ||
                    e.Message.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));

    private bool MatchesFilter(console_log_entry entry)
    {
        if (ShowAll) return true;

        return entry.LogType switch
        {
            ConsoleLogType.Info => ShowInfo,
            ConsoleLogType.Warning => ShowWarnings,
            ConsoleLogType.Error => ShowErrors,
            ConsoleLogType.Network => ShowNetwork,
            ConsoleLogType.Request => ShowNetwork,
            ConsoleLogType.Response => ShowNetwork,
            _ => true
        };
    }

    partial void OnShowAllChanged(bool value) => OnPropertyChanged(nameof(FilteredEntries));
    partial void OnShowInfoChanged(bool value) => OnPropertyChanged(nameof(FilteredEntries));
    partial void OnShowWarningsChanged(bool value) => OnPropertyChanged(nameof(FilteredEntries));
    partial void OnShowErrorsChanged(bool value) => OnPropertyChanged(nameof(FilteredEntries));
    partial void OnShowNetworkChanged(bool value) => OnPropertyChanged(nameof(FilteredEntries));
    partial void OnSearchFilterChanged(string value) => OnPropertyChanged(nameof(FilteredEntries));

    [RelayCommand]
    private void ClearLogs()
    {
        LogEntries.Clear();
        OnPropertyChanged(nameof(FilteredEntries));
    }

    public void LogInfo(string message, string? source = null)
    {
        AddEntry(ConsoleLogType.Info, message, source);
    }

    public void LogWarning(string message, string? source = null)
    {
        AddEntry(ConsoleLogType.Warning, message, source);
    }

    public void LogError(string message, string? source = null, string? details = null)
    {
        AddEntry(ConsoleLogType.Error, message, source, details);
    }

    public void LogRequest(string method, string url, Dictionary<string, string>? headers = null, string? body = null)
    {
        var headerText = headers != null
            ? string.Join("\n", headers.Select(h => $"  {h.Key}: {h.Value}"))
            : null;

        var details = new List<string>();
        if (!string.IsNullOrEmpty(headerText)) details.Add($"Headers:\n{headerText}");
        if (!string.IsNullOrEmpty(body)) details.Add($"Body:\n{body}");

        AddEntry(ConsoleLogType.Request, $"{method} {url}", "Request",
            details.Count > 0 ? string.Join("\n\n", details) : null);
    }

    public void LogResponse(int statusCode, string statusText, long elapsedMs,
        Dictionary<string, string>? headers = null, string? body = null, long? size = null)
    {
        var sizeText = size.HasValue ? FormatSize(size.Value) : "";
        var message = $"{statusCode} {statusText} - {elapsedMs}ms{(string.IsNullOrEmpty(sizeText) ? "" : $" - {sizeText}")}";

        var headerText = headers != null
            ? string.Join("\n", headers.Select(h => $"  {h.Key}: {h.Value}"))
            : null;

        var details = new List<string>();
        if (!string.IsNullOrEmpty(headerText)) details.Add($"Headers:\n{headerText}");
        if (!string.IsNullOrEmpty(body))
        {
            var truncatedBody = body.Length > 1000 ? body[..1000] + "..." : body;
            details.Add($"Body:\n{truncatedBody}");
        }

        AddEntry(ConsoleLogType.Response, message, "Response",
            details.Count > 0 ? string.Join("\n\n", details) : null);
    }

    public void LogNetwork(string message, string? details = null)
    {
        AddEntry(ConsoleLogType.Network, message, "Network", details);
    }

    public void LogScriptOutput(string message, string scriptType = "Script")
    {
        AddEntry(ConsoleLogType.Info, message, scriptType);
    }

    public void LogScriptError(string message, string scriptType = "Script", string? stackTrace = null)
    {
        AddEntry(ConsoleLogType.Error, message, scriptType, stackTrace);
    }

    private void AddEntry(ConsoleLogType logType, string message, string? source = null, string? details = null)
    {
        var entry = new console_log_entry
        {
            Timestamp = DateTime.Now,
            LogType = logType,
            Message = message,
            Source = source,
            Details = details
        };

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LogEntries.Add(entry);
            OnPropertyChanged(nameof(FilteredEntries));
        });
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB"];
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}

public enum ConsoleLogType
{
    Info,
    Warning,
    Error,
    Network,
    Request,
    Response
}

public class console_log_entry
{
    public DateTime Timestamp { get; set; }
    public ConsoleLogType LogType { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Source { get; set; }
    public string? Details { get; set; }
    public bool IsExpanded { get; set; }
}
