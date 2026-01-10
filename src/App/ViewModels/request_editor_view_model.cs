using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Core.Interfaces;
using Core.Models;
using Data.Services;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace App.ViewModels;

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
    private bool _isSyncingParamsFromUrl;
    private bool _isSyncingUrlFromParams;

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
    private string? _currentTabId;

    [ObservableProperty]
    private ObservableCollection<collection_picker_item> _availableCollections = new();

    [ObservableProperty]
    private collection_picker_item? _selectedCollection;

    [ObservableProperty]
    private ObservableCollection<key_value_pair_view_model> _headers = new();

    [ObservableProperty]
    private ObservableCollection<key_value_pair_view_model> _queryParams = new();

    // Scripts
    [ObservableProperty]
    private string _preRequestScript = string.Empty;

    [ObservableProperty]
    private string _postResponseScript = string.Empty;

    // Authorization
    [ObservableProperty]
    private auth_type _selectedAuthType = auth_type.none;

    [ObservableProperty]
    private string _basicAuthUsername = string.Empty;

    [ObservableProperty]
    private string _basicAuthPassword = string.Empty;

    [ObservableProperty]
    private string _bearerToken = string.Empty;

    [ObservableProperty]
    private string _apiKeyName = string.Empty;

    [ObservableProperty]
    private string _apiKeyValue = string.Empty;

    [ObservableProperty]
    private api_key_location _apiKeyLocation = api_key_location.header;

    [ObservableProperty]
    private string _oauth2TokenUrl = string.Empty;

    [ObservableProperty]
    private string _oauth2ClientId = string.Empty;

    [ObservableProperty]
    private string _oauth2ClientSecret = string.Empty;

    [ObservableProperty]
    private string _oauth2Scope = string.Empty;

    public IReadOnlyList<http_method> available_methods { get; } = Enum.GetValues<http_method>();
    public IReadOnlyList<auth_type> available_auth_types { get; } = Enum.GetValues<auth_type>();
    public IReadOnlyList<api_key_location> available_api_key_locations { get; } = Enum.GetValues<api_key_location>();

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
        EnsureParamsRow();
        AttachParamHandlers(QueryParams);
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
                    body_type = request_body_type.json,
                    raw_content = RequestBody
                },
                auth = CreateAuthConfig(),
                pre_request_script = PreRequestScript,
                post_response_script = PostResponseScript
            };

            var result = await _request_orchestrator.execute_request_async(request, cancellation_token);

            if (result.response != null)
            {
                // Add to history with collection context and tab ID for tab reuse
                Console.WriteLine($"[HISTORY] Creating entry: CurrentTabId={CurrentTabId ?? "null"}, CurrentCollectionId={CurrentCollectionId ?? "null"}, CurrentRequestId={CurrentRequestId ?? "null"}");
                var history_entry = new history_entry_model
                {
                    request_name = request.name,
                    method = request.method,
                    url = request.url, // Note: This stores the original URL with variables, not the resolved one
                    status_code = result.response.status_code,
                    status_description = result.response.status_description,
                    elapsed_ms = result.response.elapsed_ms,
                    response_size_bytes = result.response.size_bytes,
                    collection_id = CurrentCollectionId,
                    collection_item_id = CurrentRequestId,
                    source_tab_id = CurrentTabId,
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
            auth = CreateAuthConfig(),
            pre_request_script = PreRequestScript,
            post_response_script = PostResponseScript
        };
    }

    private auth_config_model? CreateAuthConfig()
    {
        return SelectedAuthType switch
        {
            auth_type.none => null,
            auth_type.basic => new auth_config_model
            {
                type = auth_type.basic,
                basic = new basic_auth_model
                {
                    username = BasicAuthUsername,
                    password = BasicAuthPassword
                }
            },
            auth_type.bearer => new auth_config_model
            {
                type = auth_type.bearer,
                bearer = new bearer_auth_model
                {
                    token = BearerToken
                }
            },
            auth_type.api_key => new auth_config_model
            {
                type = auth_type.api_key,
                api_key = new api_key_auth_model
                {
                    key = ApiKeyName,
                    value = ApiKeyValue,
                    location = ApiKeyLocation
                }
            },
            auth_type.oauth2_client_credentials => new auth_config_model
            {
                type = auth_type.oauth2_client_credentials,
                oauth2_client_credentials = new oauth2_client_credentials_model
                {
                    token_url = Oauth2TokenUrl,
                    client_id = Oauth2ClientId,
                    client_secret = Oauth2ClientSecret,
                    scope = string.IsNullOrWhiteSpace(Oauth2Scope) ? null : Oauth2Scope
                }
            },
            _ => null
        };
    }

    private void LoadAuthConfig(auth_config_model? auth)
    {
        // Reset all auth fields
        SelectedAuthType = auth_type.none;
        BasicAuthUsername = string.Empty;
        BasicAuthPassword = string.Empty;
        BearerToken = string.Empty;
        ApiKeyName = string.Empty;
        ApiKeyValue = string.Empty;
        ApiKeyLocation = api_key_location.header;
        Oauth2TokenUrl = string.Empty;
        Oauth2ClientId = string.Empty;
        Oauth2ClientSecret = string.Empty;
        Oauth2Scope = string.Empty;

        if (auth == null)
            return;

        SelectedAuthType = auth.type;

        switch (auth.type)
        {
            case auth_type.basic when auth.basic != null:
                BasicAuthUsername = auth.basic.username;
                BasicAuthPassword = auth.basic.password;
                break;
            case auth_type.bearer when auth.bearer != null:
                BearerToken = auth.bearer.token;
                break;
            case auth_type.api_key when auth.api_key != null:
                ApiKeyName = auth.api_key.key;
                ApiKeyValue = auth.api_key.value;
                ApiKeyLocation = auth.api_key.location;
                break;
            case auth_type.oauth2_client_credentials when auth.oauth2_client_credentials != null:
                Oauth2TokenUrl = auth.oauth2_client_credentials.token_url;
                Oauth2ClientId = auth.oauth2_client_credentials.client_id;
                Oauth2ClientSecret = auth.oauth2_client_credentials.client_secret;
                Oauth2Scope = auth.oauth2_client_credentials.scope ?? string.Empty;
                break;
        }
    }

    private async Task<string> GetTargetCollectionIdAsync(CancellationToken cancellation_token)
    {
        // First priority: use the selected collection from the picker
        if (SelectedCollection != null)
            return SelectedCollection.Id;

        // Second priority: use the current collection ID (if request came from a collection)
        if (!string.IsNullOrWhiteSpace(CurrentCollectionId))
            return CurrentCollectionId;

        // Last resort: create a default collection
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
        if (!_isSyncingUrlFromParams)
        {
            SyncQueryParamsFromUrl();
        }
        SendRequestCommand.NotifyCanExecuteChanged();
        SaveRequestCommand.NotifyCanExecuteChanged();
    }

    partial void OnRequestBodyChanged(string value)
    {
        _bodyChangedSubject.OnNext(value);
    }

    partial void OnQueryParamsChanged(ObservableCollection<key_value_pair_view_model> value)
    {
        AttachParamHandlers(value);
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
    private void AddParam()
    {
        QueryParams.Add(new key_value_pair_view_model());
    }

    [RelayCommand]
    private void RemoveParam(key_value_pair_view_model param)
    {
        QueryParams.Remove(param);
        EnsureParamsRow();
        UpdateUrlFromParams();
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

    public void load_request(http_request_model request, string? collectionId = null, string? collectionItemId = null, string? tabId = null)
    {
        _isSyncingParamsFromUrl = true;
        try
        {
            _logger.LogInformation("Loading request '{RequestName}' with TabId='{TabId}', CollectionId='{CollectionId}', CollectionItemId='{CollectionItemId}'",
                request.name, tabId ?? "null", collectionId ?? "null", collectionItemId ?? "null");

            RequestName = request.name;
            // Use the collection item ID for tracking, not the request ID
            CurrentRequestId = collectionItemId ?? request.id;
            CurrentCollectionId = collectionId;
            CurrentTabId = tabId;

            // Update the collection picker to match
            if (!string.IsNullOrEmpty(collectionId))
            {
                SelectedCollection = AvailableCollections.FirstOrDefault(c => c.Id == collectionId);
            }

            _logger.LogInformation("Set CurrentTabId='{CurrentTabId}', CurrentCollectionId='{CurrentCollectionId}'", CurrentTabId ?? "null", CurrentCollectionId ?? "null");
            
            Url = request.url;
            SelectedMethod = request.method;
            RequestBody = request.body?.raw_content ?? string.Empty;
            PreRequestScript = request.pre_request_script ?? string.Empty;
            PostResponseScript = request.post_response_script ?? string.Empty;

            // Load auth settings
            LoadAuthConfig(request.auth);

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

            QueryParams.Clear();
            if (request.query_params != null && request.query_params.Count > 0)
            {
                foreach (var q in request.query_params)
                {
                    QueryParams.Add(new key_value_pair_view_model
                    {
                        Key = q.key,
                        Value = q.value,
                        IsEnabled = q.enabled
                    });
                }
            }
            else
            {
                SyncQueryParamsFromUrl();
            }
        }
        finally
        {
            _isSyncingParamsFromUrl = false;
        }

        EnsureParamsRow();
    }

    private void EnsureParamsRow()
    {
        if (QueryParams.Count == 0)
        {
            QueryParams.Add(new key_value_pair_view_model());
        }
    }

    private void AttachParamHandlers(ObservableCollection<key_value_pair_view_model> collection)
    {
        collection.CollectionChanged -= OnParamsCollectionChanged;
        collection.CollectionChanged += OnParamsCollectionChanged;
        foreach (var item in collection)
        {
            item.PropertyChanged -= OnParamPropertyChanged;
            item.PropertyChanged += OnParamPropertyChanged;
        }
    }

    private void OnParamsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (key_value_pair_view_model item in e.NewItems)
            {
                item.PropertyChanged += OnParamPropertyChanged;
            }
        }
        if (e.OldItems != null)
        {
            foreach (key_value_pair_view_model item in e.OldItems)
            {
                item.PropertyChanged -= OnParamPropertyChanged;
            }
        }

        if (_isSyncingParamsFromUrl || _isSyncingUrlFromParams) return;

        EnsureParamsRow();
        UpdateUrlFromParams();
    }

    private void OnParamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(key_value_pair_view_model.Key) or nameof(key_value_pair_view_model.Value) or nameof(key_value_pair_view_model.IsEnabled))
        {
            UpdateUrlFromParams();
        }
    }

    private void SyncQueryParamsFromUrl()
    {
        if (_isSyncingParamsFromUrl || _isSyncingUrlFromParams) return;
        _isSyncingParamsFromUrl = true;

        try
        {
            QueryParams.Clear();

            var queryStart = Url.IndexOf('?', StringComparison.Ordinal);
            if (queryStart >= 0 && queryStart < Url.Length - 1)
            {
                var query = Url[(queryStart + 1)..];
                foreach (var segment in query.Split('&', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = segment.Split('=', 2);
                    var key = WebUtility.UrlDecode(parts[0]);
                    var value = parts.Length > 1 ? (WebUtility.UrlDecode(parts[1]) ?? string.Empty) : string.Empty;
                    QueryParams.Add(new key_value_pair_view_model { Key = key ?? string.Empty, Value = value, IsEnabled = true });
                }
            }

            EnsureParamsRow();
        }
        finally
        {
            _isSyncingParamsFromUrl = false;
        }
    }

    private void UpdateUrlFromParams()
    {
        if (_isSyncingParamsFromUrl || _isSyncingUrlFromParams) return;
        _isSyncingUrlFromParams = true;

        try
        {
            var baseUrl = Url;
            var queryIndex = baseUrl.IndexOf('?', StringComparison.Ordinal);
            if (queryIndex >= 0)
            {
                baseUrl = baseUrl[..queryIndex];
            }

            var enabledParams = QueryParams
                .Where(p => p.IsEnabled && !string.IsNullOrWhiteSpace(p.Key))
                .Select(p => $"{WebUtility.UrlEncode(p.Key)}={WebUtility.UrlEncode(p.Value ?? string.Empty)}")
                .ToList();

            var newUrl = enabledParams.Count > 0
                ? $"{baseUrl}?{string.Join("&", enabledParams)}"
                : baseUrl;

            Url = newUrl;
        }
        finally
        {
            _isSyncingUrlFromParams = false;
        }
    }

    [RelayCommand]
    public async Task LoadCollectionsAsync(CancellationToken cancellation_token = default)
    {
        var collections = await _collection_repository.list_all_async(cancellation_token);

        AvailableCollections.Clear();
        foreach (var col in collections)
        {
            AvailableCollections.Add(new collection_picker_item { Id = col.id, Name = col.name });
        }

        // If we have a current collection, select it
        if (!string.IsNullOrEmpty(CurrentCollectionId))
        {
            SelectedCollection = AvailableCollections.FirstOrDefault(c => c.Id == CurrentCollectionId);
        }
        // Otherwise select the first one if available
        else if (AvailableCollections.Count > 0 && SelectedCollection == null)
        {
            SelectedCollection = AvailableCollections.First();
        }
    }
}

public class collection_picker_item
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public override string ToString() => Name;
}
