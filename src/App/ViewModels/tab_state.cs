using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;

namespace App.ViewModels;

/// <summary>
/// Represents the state of a single tab containing a request.
/// </summary>
public partial class tab_state : ObservableObject
{
    [ObservableProperty]
    private string _id = Guid.NewGuid().ToString();

    [ObservableProperty]
    private string _title = "New Request";

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _isActive;

    // Request identification
    [ObservableProperty]
    private string? _collectionId;

    [ObservableProperty]
    private string? _collectionItemId;

    // Request state snapshot
    [ObservableProperty]
    private string _requestName = "New Request";

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private http_method _selectedMethod = http_method.get;

    [ObservableProperty]
    private string _requestBody = string.Empty;

    [ObservableProperty]
    private string _preRequestScript = string.Empty;

    [ObservableProperty]
    private string _postResponseScript = string.Empty;

    [ObservableProperty]
    private List<key_value_pair_model> _headers = new();

    [ObservableProperty]
    private List<key_value_pair_model> _queryParams = new();

    // Response state
    [ObservableProperty]
    private http_response_model? _lastResponse;

    // Original state for change detection
    private string _originalRequestName = string.Empty;
    private string _originalUrl = string.Empty;
    private http_method _originalMethod = http_method.get;
    private string _originalRequestBody = string.Empty;
    private string _originalPreRequestScript = string.Empty;
    private string _originalPostResponseScript = string.Empty;
    private List<key_value_pair_model> _originalHeaders = new();

    /// <summary>
    /// Creates a new tab from an http_request_model.
    /// </summary>
    public static tab_state from_request(http_request_model request, string? collectionId = null, string? collectionItemId = null)
    {
        var tab = new tab_state
        {
            Title = request.name,
            CollectionId = collectionId,
            CollectionItemId = collectionItemId,
            RequestName = request.name,
            Url = request.url,
            SelectedMethod = request.method,
            RequestBody = request.body?.raw_content ?? string.Empty,
            PreRequestScript = request.pre_request_script ?? string.Empty,
            PostResponseScript = request.post_response_script ?? string.Empty,
            Headers = request.headers?.ToList() ?? new List<key_value_pair_model>(),
            QueryParams = request.query_params?.ToList() ?? new List<key_value_pair_model>()
        };

        tab.save_original_state();
        return tab;
    }

    /// <summary>
    /// Creates a new empty tab.
    /// </summary>
    public static tab_state create_new()
    {
        var tab = new tab_state
        {
            Title = "New Request",
            RequestName = "New Request",
            Url = string.Empty,
            SelectedMethod = http_method.get,
            Headers = new List<key_value_pair_model>()
        };

        tab.save_original_state();
        return tab;
    }

    /// <summary>
    /// Saves the current state as the original state for change detection.
    /// </summary>
    public void save_original_state()
    {
        _originalRequestName = RequestName;
        _originalUrl = Url;
        _originalMethod = SelectedMethod;
        _originalRequestBody = RequestBody;
        _originalPreRequestScript = PreRequestScript;
        _originalPostResponseScript = PostResponseScript;
        _originalHeaders = Headers.Select(h => new key_value_pair_model 
        { 
            key = h.key, 
            value = h.value, 
            enabled = h.enabled 
        }).ToList();

        HasUnsavedChanges = false;
    }

    /// <summary>
    /// Checks if the current state differs from the original state.
    /// </summary>
    public void check_for_changes()
    {
        bool hasChanges = 
            RequestName != _originalRequestName ||
            Url != _originalUrl ||
            SelectedMethod != _originalMethod ||
            RequestBody != _originalRequestBody ||
            PreRequestScript != _originalPreRequestScript ||
            PostResponseScript != _originalPostResponseScript ||
            !headers_equal(Headers, _originalHeaders);

        HasUnsavedChanges = hasChanges;
        Title = hasChanges ? $"● {RequestName}" : RequestName;
    }

    private static bool headers_equal(List<key_value_pair_model> a, List<key_value_pair_model> b)
    {
        if (a.Count != b.Count) return false;
        
        for (int i = 0; i < a.Count; i++)
        {
            if (a[i].key != b[i].key || a[i].value != b[i].value || a[i].enabled != b[i].enabled)
                return false;
        }
        
        return true;
    }

    /// <summary>
    /// Converts the tab state to an http_request_model.
    /// </summary>
    public http_request_model to_request_model()
    {
        return new http_request_model
        {
            id = CollectionItemId ?? Guid.NewGuid().ToString(),
            name = RequestName,
            method = SelectedMethod,
            url = Url,
            headers = Headers,
            query_params = QueryParams,
            body = string.IsNullOrWhiteSpace(RequestBody) ? null : new request_body_model
            {
                body_type = request_body_type.raw,
                raw_content = RequestBody
            },
            pre_request_script = PreRequestScript,
            post_response_script = PostResponseScript
        };
    }
}
