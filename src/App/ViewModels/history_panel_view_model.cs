using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Interfaces;
using Core.Models;

namespace App.ViewModels;

public partial class history_panel_view_model : ObservableObject
{
    private readonly i_history_repository _historyRepository;

    [ObservableProperty]
    private ObservableCollection<history_group> _historyGroups = new();

    [ObservableProperty]
    private history_entry_model? _selectedEntry;

    public event EventHandler<history_entry_model>? history_entry_selected;
    public event EventHandler? clear_history_requested;

    public history_panel_view_model(i_history_repository historyRepository)
    {
        _historyRepository = historyRepository;
    }

    public async Task LoadHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (_historyRepository == null) return;

        var entries = await _historyRepository.get_recent_async(50, cancellationToken);

        // Group by date
        var grouped = entries
            .OrderByDescending(e => e.executed_at)
            .GroupBy(e => e.executed_at.Date)
            .Select(g => new history_group
            {
                Date = g.Key,
                DateLabel = GetDateLabel(g.Key),
                Entries = new ObservableCollection<history_entry_model>(g)
            });

        HistoryGroups.Clear();
        foreach (var group in grouped)
        {
            HistoryGroups.Add(group);
        }
    }

    private static string GetDateLabel(DateTime date)
    {
        var today = DateTime.Today;
        if (date == today) return "Today";
        if (date == today.AddDays(-1)) return "Yesterday";
        if (date > today.AddDays(-7)) return date.DayOfWeek.ToString();
        return date.ToString("MMM d, yyyy");
    }

    partial void OnSelectedEntryChanged(history_entry_model? value)
    {
        if (value != null)
        {
            history_entry_selected?.Invoke(this, value);
        }
    }

    [RelayCommand]
    private void SelectEntry(history_entry_model? entry)
    {
        SelectedEntry = entry;
    }

    [RelayCommand]
    private void ClearHistory()
    {
        clear_history_requested?.Invoke(this, EventArgs.Empty);
    }
}

public partial class history_group : ObservableObject
{
    public DateTime Date { get; set; }

    [ObservableProperty]
    private string _dateLabel = string.Empty;

    [ObservableProperty]
    private bool _isExpanded = true;

    [ObservableProperty]
    private ObservableCollection<history_entry_model> _entries = new();
}
