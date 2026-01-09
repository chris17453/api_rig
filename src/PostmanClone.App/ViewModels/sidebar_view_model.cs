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

    // Stores the original collection models for export
    private List<postman_collection_model> _collectionModels = new();

    /// <summary>
    /// Gets the currently selected collection model for export.
    /// </summary>
    public postman_collection_model? SelectedCollection
    {
        get
        {
            // Find the root collection of the selected item
            var selectedRoot = SelectedCollectionItem;
            while (selectedRoot != null && !selectedRoot.IsCollectionRoot)
            {
                // Walk up to find the root (not possible in tree, so use collections list)
                break;
            }

            if (selectedRoot?.IsCollectionRoot == true)
            {
                // Convert to postman_collection_model for export
                return ConvertToCollectionModel(selectedRoot);
            }

            // Return first collection if nothing selected
            return Collections.FirstOrDefault() is { } first 
                ? ConvertToCollectionModel(first)
                : null;
        }
    }

    private postman_collection_model ConvertToCollectionModel(collection_tree_item_view_model tree_item)
    {
        var items = new List<collection_item_model>();
        
        // Collect all items from the tree
        foreach (var child in tree_item.Children)
        {
            items.Add(ConvertToCollectionItem(child));
        }

        return new postman_collection_model
        {
            id = tree_item.Id.ToString(),
            name = tree_item.Name ?? "Unnamed Collection",
            items = items
        };
    }

    private collection_item_model ConvertToCollectionItem(collection_tree_item_view_model item)
    {
        var children = new List<collection_item_model>();
        foreach (var child in item.Children)
        {
            children.Add(ConvertToCollectionItem(child));
        }

        return new collection_item_model
        {
            id = item.Id.ToString(),
            name = item.Name ?? "Unnamed",
            is_folder = item.IsFolder,
            request = item.request,
            children = children
        };
    }

    public sidebar_view_model(i_collection_repository collection_repository, i_history_repository history_repository)
    {
        _collection_repository = collection_repository;
        _history_repository = history_repository;
    }

    public event EventHandler<http_request_model>? request_selected;
    public event EventHandler<(http_request_model request, string collectionId, string collectionItemId)>? request_with_collection_selected;
    public event EventHandler? collection_changed;

    [RelayCommand]
    public async Task load_data_async(CancellationToken cancellation_token)
    {
        IsLoading = true;

        try
        {
            // Store expanded state before reloading
            var expandedCollectionIds = Collections
                .Where(c => c.IsExpanded)
                .Select(c => c.Id)
                .ToHashSet();

            // Load collections
            var collections = await _collection_repository.list_all_async(cancellation_token);
            Collections.Clear();
            foreach (var col in collections)
            {
                var tree_item = map_collection_to_tree_item(col);
                // Restore expanded state - set to true if it was expanded, false otherwise
                tree_item.IsExpanded = expandedCollectionIds.Count == 0 || expandedCollectionIds.Contains(tree_item.Id);
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

    [RelayCommand]
    public async Task CreateCollection(CancellationToken cancellation_token)
    {
        // Find the highest collection number to increment
        int maxNumber = 0;
        foreach (var col in Collections)
        {
            // Parse collection names like "Collection#1", "Collection#2", etc.
            if (col.Name.StartsWith("Collection#") && 
                int.TryParse(col.Name.Substring("Collection#".Length), out int num))
            {
                maxNumber = Math.Max(maxNumber, num);
            }
        }

        var newCollection = new postman_collection_model
        {
            id = Guid.NewGuid().ToString(),
            name = $"Collection#{maxNumber + 1}",
            description = "New collection",
            items = new List<collection_item_model>()
        };

        await _collection_repository.save_async(newCollection, cancellation_token);
        await load_data_async(cancellation_token);
        collection_changed?.Invoke(this, EventArgs.Empty);
    }

    partial void OnSelectedCollectionItemChanged(collection_tree_item_view_model? value)
    {
        if (value?.request != null)
        {
            // Find the collection that contains this request
            var parentCollection = find_parent_collection(value);
            if (parentCollection != null)
            {
                // Pass the collection item ID (value.Id) not the request ID
                request_with_collection_selected?.Invoke(this, (value.request, parentCollection.Id, value.Id));
            }
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
    public async Task DeleteItem(collection_tree_item_view_model item, CancellationToken cancellation_token = default)
    {
        if (item == null) return;

        if (item.IsCollectionRoot)
        {
            // Delete entire collection
            var collection = await _collection_repository.get_by_id_async(item.Id, cancellation_token);
            if (collection != null)
            {
                await _collection_repository.delete_async(item.Id, cancellation_token);
                await load_data_async(cancellation_token);
                collection_changed?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            // Delete request from its collection - find parent collection
            var parentCollection = find_parent_collection(item);
            if (parentCollection != null)
            {
                var collection = await _collection_repository.get_by_id_async(parentCollection.Id, cancellation_token);
                if (collection != null)
                {
                    // Filter out the item by its ID (collection_item_model.id, not request.id)
                    var items = collection.items.Where(i => i.id != item.Id).ToList();
                    collection = collection with { items = items };
                    await _collection_repository.save_async(collection, cancellation_token);
                    await load_data_async(cancellation_token);
                    collection_changed?.Invoke(this, EventArgs.Empty);
                }
            }
        }
    }

    [RelayCommand]
    public async Task AddRequestToCollection(collection_tree_item_view_model collectionItem, CancellationToken cancellation_token = default)
    {
        if (collectionItem == null || !collectionItem.IsCollectionRoot) return;

        // Create a new empty request
        var newRequest = new http_request_model
        {
            id = Guid.NewGuid().ToString(),
            name = "New Request",
            method = http_method.get,
            url = "https://api.example.com",
            headers = new List<key_value_pair_model>(),
            query_params = new List<key_value_pair_model>()
        };

        var collection = await _collection_repository.get_by_id_async(collectionItem.Id, cancellation_token);
        if (collection != null)
        {
            var items = collection.items.ToList();
            var newCollectionItem = new collection_item_model
            {
                id = Guid.NewGuid().ToString(),
                name = newRequest.name,
                is_folder = false,
                request = newRequest
            };
            items.Add(newCollectionItem);

            collection = collection with { items = items };
            await _collection_repository.save_async(collection, cancellation_token);
            await load_data_async(cancellation_token);
            
            // Trigger request selection to load it in the editor with collection ID and collection item ID
            request_with_collection_selected?.Invoke(this, (newRequest, collection.id, newCollectionItem.id));
            collection_changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private collection_tree_item_view_model? find_parent_collection(collection_tree_item_view_model item)
    {
        // Search through all collections to find the parent
        foreach (var collection in Collections)
        {
            if (contains_item(collection, item))
            {
                return collection;
            }
        }
        return null;
    }

    private bool contains_item(collection_tree_item_view_model parent, collection_tree_item_view_model target)
    {
        if (parent == target) return true;
        
        foreach (var child in parent.Children)
        {
            if (child == target || contains_item(child, target))
                return true;
        }
        
        return false;
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

    [RelayCommand]
    public async Task RenameItem(collection_tree_item_view_model item, CancellationToken cancellation_token = default)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Name)) return;

        if (item.IsCollectionRoot)
        {
            // Rename collection
            var collection = await _collection_repository.get_by_id_async(item.Id, cancellation_token);
            if (collection != null)
            {
                collection = collection with { name = item.Name };
                await _collection_repository.save_async(collection, cancellation_token);
                collection_changed?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            // Rename request - find parent collection and update
            var parentCollection = find_parent_collection(item);
            if (parentCollection != null)
            {
                var collection = await _collection_repository.get_by_id_async(parentCollection.Id, cancellation_token);
                if (collection != null)
                {
                    // Update the item's name in the collection
                    var updatedItems = update_item_name_recursive(collection.items.ToList(), item.Id, item.Name);
                    collection = collection with { items = updatedItems };
                    await _collection_repository.save_async(collection, cancellation_token);
                    collection_changed?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        item.IsEditing = false;
    }

    private List<collection_item_model> update_item_name_recursive(List<collection_item_model> items, string itemId, string newName)
    {
        var result = new List<collection_item_model>();
        foreach (var item in items)
        {
            if (item.id == itemId)
            {
                // Update the name - need to create new instance since it's a record
                result.Add(item with { name = newName });
            }
            else if (item.children != null && item.children.Count > 0)
            {
                var updatedChildren = update_item_name_recursive(item.children.ToList(), itemId, newName);
                result.Add(item with { children = updatedChildren });
            }
            else
            {
                result.Add(item);
            }
        }
        return result;
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
            IsCollectionRoot = false,
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

    [ObservableProperty]
    private bool _isEditing = false;

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
