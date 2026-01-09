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
using System.Text;

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

    // Body type support
    [ObservableProperty]
    private request_body_type _selectedBodyType = request_body_type.none;

    [ObservableProperty]
    private ObservableCollection<key_value_pair_view_model> _formDataParams = new();

    // Auth support
    [ObservableProperty]
    private auth_type _selectedAuthType = auth_type.none;

    [ObservableProperty]
    private string _authUsername = string.Empty;

    [ObservableProperty]
    private string _authPassword = string.Empty;

    [ObservableProperty]
    private string _authToken = string.Empty;

    [ObservableProperty]
    private string _authApiKey = string.Empty;

    [ObservableProperty]
    private string _authApiKeyName = "X-API-Key";

    [ObservableProperty]
    private api_key_location _authApiKeyLocation = api_key_location.header;

    [ObservableProperty]
    private string _authOAuth2ClientId = string.Empty;

    [ObservableProperty]
    private string _authOAuth2ClientSecret = string.Empty;

    [ObservableProperty]
    private string _authOAuth2TokenUrl = string.Empty;

    [ObservableProperty]
    private string _authOAuth2Scope = string.Empty;

    // Scripts
    [ObservableProperty]
    private string _preRequestScript = string.Empty;

    [ObservableProperty]
    private string _postResponseScript = string.Empty;

    // Generated cURL command
    [ObservableProperty]
    private string _generatedCurlCommand = string.Empty;

    public IReadOnlyList<http_method> available_methods { get; } = Enum.GetValues<http_method>();
    public IReadOnlyList<request_body_type> available_body_types { get; } = Enum.GetValues<request_body_type>();
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
        // Add default empty query param row
        _queryParams.Add(new key_value_pair_view_model());
        // Add default empty form data row
        _formDataParams.Add(new key_value_pair_view_model());
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
                name = RequestName,
                method = SelectedMethod,
                url = Url,
                headers = Headers
                    .Where(h => !string.IsNullOrWhiteSpace(h.Key))
                    .Select(h => new key_value_pair_model { key = h.Key, value = h.Value, enabled = h.IsEnabled })
                    .ToList(),
                query_params = QueryParams
                    .Where(q => !string.IsNullOrWhiteSpace(q.Key))
                    .Select(q => new key_value_pair_model { key = q.Key, value = q.Value, enabled = q.IsEnabled })
                    .ToList(),
                body = CreateRequestBody(),
                auth = CreateAuthConfig(),
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
                    url = request.url,
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
            body = CreateRequestBody(),
            auth = CreateAuthConfig(),
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

    [RelayCommand]
    private void AddQueryParam()
    {
        QueryParams.Add(new key_value_pair_view_model());
    }

    [RelayCommand]
    private void RemoveQueryParam(key_value_pair_view_model param)
    {
        QueryParams.Remove(param);
    }

    [RelayCommand]
    private void AddFormDataParam()
    {
        FormDataParams.Add(new key_value_pair_view_model());
    }

    [RelayCommand]
    private void RemoveFormDataParam(key_value_pair_view_model param)
    {
        FormDataParams.Remove(param);
    }

    [RelayCommand]
    private void GenerateCurl()
    {
        GeneratedCurlCommand = BuildCurlString();
    }

    [RelayCommand]
    private async Task CopyCurlToClipboard()
    {
        var curl = BuildCurlString();
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
            {
                await clipboard.SetTextAsync(curl);
                StatusMessage = "✓ cURL command copied to clipboard";
            }
        }
    }

    private request_body_model? CreateRequestBody()
    {
        if (SelectedBodyType == request_body_type.none)
            return null;

        if (SelectedBodyType == request_body_type.form_data)
        {
            var formDict = FormDataParams
                .Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key))
                .ToDictionary(f => f.Key, f => f.Value);
            
            return new request_body_model
            {
                body_type = SelectedBodyType,
                form_data = formDict
            };
        }

        if (SelectedBodyType == request_body_type.x_www_form_urlencoded)
        {
            var formDict = FormDataParams
                .Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key))
                .ToDictionary(f => f.Key, f => f.Value);
            
            return new request_body_model
            {
                body_type = SelectedBodyType,
                form_urlencoded = formDict
            };
        }

        if (string.IsNullOrWhiteSpace(RequestBody))
            return null;

        return new request_body_model
        {
            body_type = SelectedBodyType,
            raw_content = RequestBody
        };
    }

    private auth_config_model? CreateAuthConfig()
    {
        if (SelectedAuthType == auth_type.none)
            return null;

        return new auth_config_model
        {
            type = SelectedAuthType,
            basic = SelectedAuthType == auth_type.basic ? new basic_auth_model
            {
                username = AuthUsername,
                password = AuthPassword
            } : null,
            bearer = SelectedAuthType == auth_type.bearer ? new bearer_auth_model
            {
                token = AuthToken
            } : null,
            api_key = SelectedAuthType == auth_type.api_key ? new api_key_auth_model
            {
                key = AuthApiKeyName,
                value = AuthApiKey,
                location = AuthApiKeyLocation
            } : null,
            oauth2_client_credentials = SelectedAuthType == auth_type.oauth2_client_credentials ? new oauth2_client_credentials_model
            {
                client_id = AuthOAuth2ClientId,
                client_secret = AuthOAuth2ClientSecret,
                token_url = AuthOAuth2TokenUrl,
                scope = AuthOAuth2Scope
            } : null
        };
    }

    private string BuildCurlString()
    {
        var sb = new StringBuilder();
        sb.Append("curl");

        // Method
        if (SelectedMethod != http_method.get)
        {
            sb.Append($" -X {SelectedMethod.ToString().ToUpperInvariant()}");
        }

        // URL with query params
        var url = Url;
        var queryString = string.Join("&", QueryParams
            .Where(q => q.IsEnabled && !string.IsNullOrWhiteSpace(q.Key))
            .Select(q => $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}"));
        if (!string.IsNullOrEmpty(queryString))
        {
            url += (url.Contains("?") ? "&" : "?") + queryString;
        }
        sb.Append($" \"{url}\"");

        // Headers
        foreach (var header in Headers.Where(h => h.IsEnabled && !string.IsNullOrWhiteSpace(h.Key)))
        {
            sb.Append($" \\\n  -H \"{header.Key}: {header.Value}\"");
        }

        // Auth
        if (SelectedAuthType == auth_type.basic)
        {
            sb.Append($" \\\n  -u \"{AuthUsername}:{AuthPassword}\"");
        }
        else if (SelectedAuthType == auth_type.bearer)
        {
            sb.Append($" \\\n  -H \"Authorization: Bearer {AuthToken}\"");
        }
        else if (SelectedAuthType == auth_type.api_key && AuthApiKeyLocation == api_key_location.header)
        {
            sb.Append($" \\\n  -H \"{AuthApiKeyName}: {AuthApiKey}\"");
        }

        // Body
        if (SelectedBodyType == request_body_type.json || SelectedBodyType == request_body_type.raw)
        {
            if (!string.IsNullOrWhiteSpace(RequestBody))
            {
                var escapedBody = RequestBody.Replace("\"", "\\\"").Replace("\n", "\\n");
                sb.Append($" \\\n  -H \"Content-Type: application/json\"");
                sb.Append($" \\\n  -d \"{escapedBody}\"");
            }
        }
        else if (SelectedBodyType == request_body_type.x_www_form_urlencoded)
        {
            var formData = string.Join("&", FormDataParams
                .Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key))
                .Select(f => $"{Uri.EscapeDataString(f.Key)}={Uri.EscapeDataString(f.Value)}"));
            if (!string.IsNullOrEmpty(formData))
            {
                sb.Append($" \\\n  -H \"Content-Type: application/x-www-form-urlencoded\"");
                sb.Append($" \\\n  -d \"{formData}\"");
            }
        }
        else if (SelectedBodyType == request_body_type.form_data)
        {
            foreach (var param in FormDataParams.Where(f => f.IsEnabled && !string.IsNullOrWhiteSpace(f.Key)))
            {
                sb.Append($" \\\n  -F \"{param.Key}={param.Value}\"");
            }
        }

        return sb.ToString();
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
        CurrentRequestId = collectionItemId ?? request.id;
        CurrentCollectionId = collectionId;
        
        _logger.LogInformation("Set CurrentCollectionId to '{CurrentCollectionId}'", CurrentCollectionId ?? "null");
        
        Url = request.url;
        SelectedMethod = request.method;
        RequestBody = request.body?.raw_content ?? string.Empty;
        SelectedBodyType = request.body?.body_type ?? request_body_type.none;
        PreRequestScript = request.pre_request_script ?? string.Empty;
        PostResponseScript = request.post_response_script ?? string.Empty;
        
        // Load headers
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

        // Load query params
        QueryParams.Clear();
        foreach (var q in request.query_params)
        {
            QueryParams.Add(new key_value_pair_view_model
            {
                Key = q.key,
                Value = q.value,
                IsEnabled = q.enabled
            });
        }
        if (QueryParams.Count == 0)
            QueryParams.Add(new key_value_pair_view_model());

        // Load form data from dictionary
        FormDataParams.Clear();
        var formData = request.body?.form_data ?? request.body?.form_urlencoded;
        if (formData != null)
        {
            foreach (var kvp in formData)
            {
                FormDataParams.Add(new key_value_pair_view_model
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    IsEnabled = true
                });
            }
        }
        if (FormDataParams.Count == 0)
            FormDataParams.Add(new key_value_pair_view_model());

        // Load auth
        if (request.auth != null)
        {
            SelectedAuthType = request.auth.type;
            if (request.auth.basic != null)
            {
                AuthUsername = request.auth.basic.username;
                AuthPassword = request.auth.basic.password;
            }
            if (request.auth.bearer != null)
            {
                AuthToken = request.auth.bearer.token;
            }
            if (request.auth.api_key != null)
            {
                AuthApiKeyName = request.auth.api_key.key;
                AuthApiKey = request.auth.api_key.value;
                AuthApiKeyLocation = request.auth.api_key.location;
            }
            if (request.auth.oauth2_client_credentials != null)
            {
                AuthOAuth2ClientId = request.auth.oauth2_client_credentials.client_id;
                AuthOAuth2ClientSecret = request.auth.oauth2_client_credentials.client_secret;
                AuthOAuth2TokenUrl = request.auth.oauth2_client_credentials.token_url;
                AuthOAuth2Scope = request.auth.oauth2_client_credentials.scope ?? string.Empty;
            }
        }
        else
        {
            SelectedAuthType = auth_type.none;
        }
    }
}
