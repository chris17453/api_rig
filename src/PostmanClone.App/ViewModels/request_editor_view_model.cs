using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Services;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace PostmanClone.App.ViewModels;

public partial class request_editor_view_model : ObservableObject, IDisposable
{
    private readonly request_orchestrator _request_orchestrator;
    private readonly i_history_repository _history_repository;
    private readonly i_collection_repository _collection_repository;
    private readonly ILogger<request_editor_view_model> _logger;

    [ObservableProperty]
    private string _requestName = "New Request";

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private http_method _selectedMethod = http_method.get;

    [ObservableProperty]
    private string _requestBody = string.Empty;

    [ObservableProperty]
    private string _jsonValidationError = string.Empty;

    [ObservableProperty]
    private bool _isJsonValid = true;

    private readonly Subject<string> _bodyChangedSubject = new();
    private IDisposable? _debounceSubscription;

    [ObservableProperty]
    private bool _isSending;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private string? _currentRequestId;

    [ObservableProperty]
    private string? _currentCollectionId;

    [ObservableProperty]
    private ObservableCollection<key_value_pair_view_model> _headers = new();

    [ObservableProperty]
    private ObservableCollection<key_value_pair_view_model> _queryParams = new();

    // Scripts
    [ObservableProperty]
    private string _preRequestScript = string.Empty;

    [ObservableProperty]
    private string _postResponseScript = string.Empty;

    public IReadOnlyList<http_method> available_methods { get; } = Enum.GetValues<http_method>();

    public request_editor_view_model(
        request_orchestrator request_orchestrator,
        i_history_repository history_repository,
        i_collection_repository collection_repository,
        ILogger<request_editor_view_model> logger)
    {
        _request_orchestrator = request_orchestrator;
        _history_repository = history_repository;
        _collection_repository = collection_repository;
        _logger = logger;
        
        _debounceSubscription = _bodyChangedSubject
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(ValidateJson);

        // Add default empty header row
        _headers.Add(new key_value_pair_view_model());
    }

    public event EventHandler? execution_started;
    public event EventHandler<request_execution_result>? execution_completed;
    public event EventHandler? request_saved;

    [RelayCommand(CanExecute = nameof(CanSendRequest))]
    private async Task SendRequest(CancellationToken cancellation_token)
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        IsSending = true;
        
        // Notify that execution is starting
        execution_started?.Invoke(this, EventArgs.Empty);

        try
        {
            var request = new http_request_model
            {
                name = "Request",
                method = SelectedMethod,
                url = Url,
                headers = Headers
                    .Where(h => !string.IsNullOrWhiteSpace(h.Key))
                    .Select(h => new key_value_pair_model { key = h.Key, value = h.Value, enabled = h.IsEnabled })
                    .ToList(),
                body = string.IsNullOrWhiteSpace(RequestBody) ? null : new request_body_model
                {
                    body_type = request_body_type.raw,
                    raw_content = RequestBody
                },
                pre_request_script = PreRequestScript,
                post_response_script = PostResponseScript
            };

            var result = await _request_orchestrator.execute_request_async(request, cancellation_token);

            if (result.response != null)
            {
                // Add to history
                var history_entry = new history_entry_model
                {
                    request_name = request.name,
                    method = request.method,
                    url = request.url, // Note: This stores the original URL with variables, not the resolved one
                    status_code = result.response.status_code,
                    status_description = result.response.status_description,
                    elapsed_ms = result.response.elapsed_ms,
                    response_size_bytes = result.response.size_bytes,
                    executed_at = DateTime.UtcNow,
                    error_message = result.response.error_message,
                    request_snapshot = request,
                    response_snapshot = result.response
                };
                await _history_repository.append_async(history_entry, cancellation_token);
            }

            execution_completed?.Invoke(this, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing request");
        }
        finally
        {
            IsSending = false;
        }
    }

    private bool CanSendRequest() => !IsSending && !string.IsNullOrWhiteSpace(Url);

    partial void OnIsSendingChanged(bool value) => SendRequestCommand.NotifyCanExecuteChanged();

    [RelayCommand(CanExecute = nameof(CanSaveRequest))]
    private async Task SaveRequest(CancellationToken cancellation_token)
    {
        if (string.IsNullOrWhiteSpace(Url) || string.IsNullOrWhiteSpace(RequestName))
            return;

        IsSaving = true;

        try
        {
            var request = CreateRequestModel();
            var targetCollectionId = await GetTargetCollectionIdAsync(cancellation_token);
            await SaveToCollectionAsync(targetCollectionId, request, cancellation_token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving request");
            StatusMessage = $"❌ Error saving request: {ex.Message}";
        }
        finally
        {
            IsSaving = false;
        }
    }

    private http_request_model CreateRequestModel()
    {
        return new http_request_model
        {
            id = CurrentRequestId ?? Guid.NewGuid().ToString(),
            name = RequestName,
            method = SelectedMethod,
            url = Url,
            headers = Headers.Where(h => !string.IsNullOrWhiteSpace(h.Key))
                .Select(h => new key_value_pair_model { key = h.Key, value = h.Value, enabled = h.IsEnabled }).ToList(),
            query_params = QueryParams.Where(q => !string.IsNullOrWhiteSpace(q.Key))
                .Select(q => new key_value_pair_model { key = q.Key, value = q.Value, enabled = q.IsEnabled }).ToList(),
            body = string.IsNullOrWhiteSpace(RequestBody) ? null : new request_body_model
            {
                body_type = request_body_type.raw,
                raw_content = RequestBody
            },
            pre_request_script = PreRequestScript,
            post_response_script = PostResponseScript
        };
    }

    private async Task<string> GetTargetCollectionIdAsync(CancellationToken cancellation_token)
    {
        var targetCollectionId = CurrentCollectionId;

        if (!string.IsNullOrWhiteSpace(targetCollectionId))
            return targetCollectionId;

        var collections = await _collection_repository.list_all_async(cancellation_token);
        var defaultCollection = collections.FirstOrDefault();

        if (defaultCollection == null)
        {
            defaultCollection = new postman_collection_model
            {
                id = Guid.NewGuid().ToString(),
                name = "My Requests",
                description = "Default collection",
                items = new List<collection_item_model>()
            };
            await _collection_repository.save_async(defaultCollection, cancellation_token);
        }

        return defaultCollection.id;
    }

    private async Task SaveToCollectionAsync(string collectionId, http_request_model request, CancellationToken cancellation_token)
    {
        var collection = await _collection_repository.get_by_id_async(collectionId, cancellation_token);
        if (collection == null) return;

        var items = collection.items.ToList();
        var duplicateNameIndex = items.FindIndex(i => i.name.Equals(request.name, StringComparison.OrdinalIgnoreCase));
        var currentItemIndex = string.IsNullOrWhiteSpace(CurrentRequestId) ? -1 : items.FindIndex(i => i.id == CurrentRequestId);

        string itemId;

        if (duplicateNameIndex >= 0)
        {
            itemId = items[duplicateNameIndex].id;
            if (currentItemIndex >= 0 && currentItemIndex != duplicateNameIndex)
            {
                items.RemoveAt(currentItemIndex);
                if (currentItemIndex < duplicateNameIndex) duplicateNameIndex--;
            }
        }
        else if (currentItemIndex >= 0)
        {
            itemId = items[currentItemIndex].id;
        }
        else
        {
            itemId = Guid.NewGuid().ToString();
        }

        var collectionItem = new collection_item_model
        {
            id = itemId,
            name = request.name,
            is_folder = false,
            request = request
        };

        if (duplicateNameIndex >= 0) items[duplicateNameIndex] = collectionItem;
        else if (currentItemIndex >= 0) items[currentItemIndex] = collectionItem;
        else items.Add(collectionItem);

        collection = collection with { items = items };
        await _collection_repository.save_async(collection, cancellation_token);

        CurrentRequestId = itemId;
        CurrentCollectionId = collection.id;

        StatusMessage = $"✓ Request '{RequestName}' saved successfully";
        request_saved?.Invoke(this, EventArgs.Empty);
    }

    private bool CanSaveRequest() => !IsSaving && !string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(RequestName);

    partial void OnRequestNameChanged(string value)
    {
        StatusMessage = string.Empty;
        SaveRequestCommand.NotifyCanExecuteChanged();
    }
    partial void OnIsSavingChanged(bool value) => SaveRequestCommand.NotifyCanExecuteChanged();

    partial void OnUrlChanged(string value)
    {
        SendRequestCommand.NotifyCanExecuteChanged();
        SaveRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnRequestBodyChanged(string value)
    {
        _bodyChangedSubject.OnNext(value);
    }

    private void ValidateJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            IsJsonValid = true;
            JsonValidationError = string.Empty;
            return;
        }

        try
        {
            _ = JToken.Parse(json);
            IsJsonValid = true;
            JsonValidationError = string.Empty;
        }
        catch (JsonReaderException ex)
        {
            IsJsonValid = false;
            JsonValidationError = $"Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            IsJsonValid = false;
            JsonValidationError = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void FormatJson()
    {
        if (string.IsNullOrWhiteSpace(RequestBody)) return;

        try
        {
            var token = JToken.Parse(RequestBody);
            RequestBody = token.ToString(Formatting.Indented);
        }
        catch (Exception ex)
        {
            JsonValidationError = $"Cannot format: {ex.Message}";
        }
    }

    [RelayCommand]
    private void AddHeader()
    {
        Headers.Add(new key_value_pair_view_model());
    }

    [RelayCommand]
    private void RemoveHeader(key_value_pair_view_model header)
    {
        Headers.Remove(header);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _debounceSubscription?.Dispose();
            _debounceSubscription = null;
            _bodyChangedSubject.Dispose();
        }
    }

    public void load_request(http_request_model request, string? collectionId = null, string? collectionItemId = null)
    {
        _logger.LogInformation("Loading request '{RequestName}' with CollectionId='{CollectionId}', CollectionItemId='{CollectionItemId}'", 
            request.name, collectionId ?? "null", collectionItemId ?? "null");
        
        RequestName = request.name;
        // Use the collection item ID for tracking, not the request ID
        CurrentRequestId = collectionItemId ?? request.id;
        CurrentCollectionId = collectionId;
        
        _logger.LogInformation("Set CurrentCollectionId to '{CurrentCollectionId}'", CurrentCollectionId ?? "null");
        
        Url = request.url;
        SelectedMethod = request.method;
        RequestBody = request.body?.raw_content ?? string.Empty;
        PreRequestScript = request.pre_request_script ?? string.Empty;
        PostResponseScript = request.post_response_script ?? string.Empty;
        
        Headers.Clear();
        foreach (var h in request.headers)
        {
            Headers.Add(new key_value_pair_view_model
            {
                Key = h.key,
                Value = h.value,
                IsEnabled = h.enabled
            });
        }
        if (Headers.Count == 0)
            Headers.Add(new key_value_pair_view_model());
    }
}
