using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;
using System.Collections.ObjectModel;

namespace PostmanClone.App.ViewModels;

public partial class request_editor_view_model : ObservableObject
{
    private readonly i_request_executor _request_executor;
    private readonly i_history_repository _history_repository;

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

    public IReadOnlyList<http_method> available_methods { get; } = Enum.GetValues<http_method>();

    public request_editor_view_model(i_request_executor request_executor, i_history_repository history_repository)
    {
        _request_executor = request_executor;
        _history_repository = history_repository;
        
        // Add default empty header row
        _headers.Add(new key_value_pair_view_model());
    }

    public event EventHandler<http_response_model>? response_received;

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
                }
            };

            var response = await _request_executor.execute_async(request, cancellation_token);

            // Add to history
            var history_entry = new history_entry_model
            {
                request_name = request.name,
                method = request.method,
                url = request.url,
                status_code = response.status_code,
                status_description = response.status_description,
                elapsed_ms = response.elapsed_ms,
                response_size_bytes = response.size_bytes,
                executed_at = DateTime.UtcNow,
                error_message = response.error_message,
                request_snapshot = request,
                response_snapshot = response
            };
            await _history_repository.append_async(history_entry, cancellation_token);

            response_received?.Invoke(this, response);
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
