using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;
using System.Text.Json;

namespace App.ViewModels;

public partial class response_viewer_view_model : ObservableObject
{
    [ObservableProperty]
    private int? _statusCode;

    [ObservableProperty]
    private string _statusDescription = string.Empty;

    [ObservableProperty]
    private string _body = string.Empty;

    [ObservableProperty]
    private string _formattedBody = string.Empty;

    [ObservableProperty]
    private long _elapsedMs;

    [ObservableProperty]
    private long _sizeBytes;

    [ObservableProperty]
    private string _contentType = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _hasResponse;

    [ObservableProperty]
    private bool _isSuccess;

    [ObservableProperty]
    private string _headersText = string.Empty;

    public void load_response(http_response_model response)
    {
        StatusCode = response.status_code;
        StatusDescription = response.status_description;
        Body = response.body_string ?? string.Empty;
        ElapsedMs = response.elapsed_ms;
        SizeBytes = response.size_bytes;
        ContentType = response.content_type ?? string.Empty;
        ErrorMessage = response.error_message;
        HasResponse = true;
        IsSuccess = response.is_success;

        // Format JSON body if applicable
        FormattedBody = try_format_json(Body);

        // Format headers
        HeadersText = string.Join(Environment.NewLine, 
            response.headers.Select(h => $"{h.key}: {h.value}"));
    }

    public void clear()
    {
        StatusCode = null;
        StatusDescription = string.Empty;
        Body = string.Empty;
        FormattedBody = string.Empty;
        ElapsedMs = 0;
        SizeBytes = 0;
        ContentType = string.Empty;
        ErrorMessage = null;
        HasResponse = false;
        IsSuccess = false;
        HeadersText = string.Empty;
    }

    private static string try_format_json(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        try
        {
            using var doc = JsonDocument.Parse(input);
            return JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        }
        catch
        {
            return input;
        }
    }
}
