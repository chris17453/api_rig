using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using System.Collections.ObjectModel;

namespace PostmanClone.App.ViewModels;

public partial class sidebar_view_model : ObservableObject
{
    private readonly i_collection_repository _collection_repository;
    private readonly i_history_repository _history_repository;

    [ObservableProperty]
    private ObservableCollection<collection_tree_item_view_model> _collections = new();

    [ObservableProperty]
    private ObservableCollection<history_item_view_model> _history = new();

    [ObservableProperty]
    private collection_tree_item_view_model? _selectedCollectionItem;

    [ObservableProperty]
    private history_item_view_model? _selectedHistoryItem;

    [ObservableProperty]
    private bool _isLoading;

    public sidebar_view_model(i_collection_repository collection_repository, i_history_repository history_repository)
    {
        _collection_repository = collection_repository;
        _history_repository = history_repository;
    }

    public event EventHandler<http_request_model>? request_selected;

    [RelayCommand]
    public async Task load_data_async(CancellationToken cancellation_token)
    {
        IsLoading = true;

        try
        {
            // Load collections
            var collections = await _collection_repository.list_all_async(cancellation_token);
            Collections.Clear();
            foreach (var col in collections)
            {
                var tree_item = map_collection_to_tree_item(col);
                Collections.Add(tree_item);
            }

            // Load history
            var history_entries = await _history_repository.get_recent_async(50, cancellation_token);
            History.Clear();
            foreach (var entry in history_entries)
            {
                History.Add(new history_item_view_model
                {
                    Id = entry.id,
                    RequestName = entry.request_name,
                    Method = entry.method,
                    Url = entry.url,
                    StatusCode = entry.status_code,
                    ExecutedAt = entry.executed_at,
                    request_snapshot = entry.request_snapshot
                });
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedCollectionItemChanged(collection_tree_item_view_model? value)
    {
        if (value?.request != null)
        {
            request_selected?.Invoke(this, value.request);
        }
    }

    partial void OnSelectedHistoryItemChanged(history_item_view_model? value)
    {
        if (value?.request_snapshot != null)
        {
            request_selected?.Invoke(this, value.request_snapshot);
        }
    }

    [RelayCommand]
    public async Task refresh_history_async(CancellationToken cancellation_token)
    {
        var history_entries = await _history_repository.get_recent_async(50, cancellation_token);
        History.Clear();
        foreach (var entry in history_entries)
        {
            History.Add(new history_item_view_model
            {
                Id = entry.id,
                RequestName = entry.request_name,
                Method = entry.method,
                Url = entry.url,
                StatusCode = entry.status_code,
                ExecutedAt = entry.executed_at,
                request_snapshot = entry.request_snapshot
            });
        }
    }

    private static collection_tree_item_view_model map_collection_to_tree_item(postman_collection_model collection)
    {
        var root = new collection_tree_item_view_model
        {
            Id = collection.id,
            Name = collection.name,
            IsFolder = true,
            IsCollectionRoot = true
        };

        if (collection.items != null)
        {
            foreach (var item in collection.items)
            {
                root.Children.Add(map_item_to_tree_item(item));
            }
        }

        return root;
    }

    private static collection_tree_item_view_model map_item_to_tree_item(collection_item_model item)
    {
        var vm = new collection_tree_item_view_model
        {
            Id = item.id,
            Name = item.name,
            IsFolder = item.is_folder,
            request = item.request,
            Method = item.request?.method
        };

        if (item.children != null)
        {
            foreach (var child in item.children)
            {
                vm.Children.Add(map_item_to_tree_item(child));
            }
        }

        return vm;
    }
}

public partial class collection_tree_item_view_model : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private bool _isFolder;

    [ObservableProperty]
    private bool _isCollectionRoot;

    [ObservableProperty]
    private http_method? _method;

    [ObservableProperty]
    private bool _isExpanded = true;

    public http_request_model? request { get; set; }

    public ObservableCollection<collection_tree_item_view_model> Children { get; } = new();
}

public partial class history_item_view_model : ObservableObject
{
    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _requestName = string.Empty;

    [ObservableProperty]
    private http_method _method;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private int? _statusCode;

    [ObservableProperty]
    private DateTime _executedAt;

    public http_request_model? request_snapshot { get; set; }
}
