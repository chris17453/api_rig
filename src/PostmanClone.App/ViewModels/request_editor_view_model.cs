using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using PostmanClone.Data.Services;
using System.Collections.ObjectModel;

namespace PostmanClone.App.ViewModels;

public partial class request_editor_view_model : ObservableObject
{
    private readonly request_orchestrator _request_orchestrator;
    private readonly i_history_repository _history_repository;
    private readonly ILogger<request_editor_view_model> _logger;

    [ObservableProperty]
    private string _url = string.Empty;

    [ObservableProperty]
    private http_method _selectedMethod = http_method.get;

    [ObservableProperty]
    private string _requestBody = string.Empty;

    [ObservableProperty]
    private bool _isSending;

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
        ILogger<request_editor_view_model> logger)
    {
        _request_orchestrator = request_orchestrator;
        _history_repository = history_repository;
        _logger = logger;
        
        // Add default empty header row
        _headers.Add(new key_value_pair_view_model());
    }

    public event EventHandler<request_execution_result>? execution_completed;

    [RelayCommand(CanExecute = nameof(CanSendRequest))]
    private async Task SendRequest(CancellationToken cancellation_token)
    {
        if (string.IsNullOrWhiteSpace(Url))
            return;

        IsSending = true;

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

    partial void OnUrlChanged(string value) => SendRequestCommand.NotifyCanExecuteChanged();
    partial void OnIsSendingChanged(bool value) => SendRequestCommand.NotifyCanExecuteChanged();

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

    public void load_request(http_request_model request)
    {
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

public partial class key_value_pair_view_model : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private bool _isEnabled = true;
}
