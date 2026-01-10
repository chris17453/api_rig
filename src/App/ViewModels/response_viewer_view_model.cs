using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace App.ViewModels;

public enum BodyViewMode
{
    Pretty,
    Raw,
    Preview
}

public enum BodyFormat
{
    Auto,
    JSON,
    XML,
    HTML,
    Text
}

/// <summary>
/// View model for displaying key-value pairs in tables
/// </summary>
public partial class ResponseHeaderViewModel : ObservableObject
{
    [ObservableProperty]
    private string _key = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    public ResponseHeaderViewModel(string key, string value)
    {
        Key = key;
        Value = value;
    }

    [RelayCommand]
    private async Task CopyKey() => await CopyToClipboard(Key);

    [RelayCommand]
    private async Task CopyValue() => await CopyToClipboard(Value);

    [RelayCommand]
    private async Task CopyPair() => await CopyToClipboard($"{Key}: {Value}");

    private static async Task CopyToClipboard(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
    }
}

/// <summary>
/// View model for displaying cookies
/// </summary>
public partial class ResponseCookieViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _value = string.Empty;

    [ObservableProperty]
    private string _domain = string.Empty;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _expires = string.Empty;

    [ObservableProperty]
    private bool _httpOnly;

    [ObservableProperty]
    private bool _secure;

    [RelayCommand]
    private async Task CopyName() => await CopyToClipboard(Name);

    [RelayCommand]
    private async Task CopyValue() => await CopyToClipboard(Value);

    [RelayCommand]
    private async Task CopyCookie() => await CopyToClipboard($"{Name}={Value}");

    private static async Task CopyToClipboard(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
    }
}

public partial class response_viewer_view_model : ObservableObject
{
    [ObservableProperty]
    private int? _statusCode;

    [ObservableProperty]
    private string _statusDescription = string.Empty;

    [ObservableProperty]
    private string _body = string.Empty;

    [ObservableProperty]
    private string _displayBody = string.Empty;

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

    [ObservableProperty]
    private BodyViewMode _selectedViewMode = BodyViewMode.Pretty;

    [ObservableProperty]
    private BodyFormat _selectedFormat = BodyFormat.Auto;

    [ObservableProperty]
    private BodyFormat _detectedFormat = BodyFormat.Text;

    [ObservableProperty]
    private string _cookiesText = string.Empty;

    [ObservableProperty]
    private bool _hasCookies;

    [ObservableProperty]
    private int _cookieCount;

    [ObservableProperty]
    private bool _isPreviewMode;

    [ObservableProperty]
    private bool _canPreview;

    // Collections for table display
    public ObservableCollection<ResponseHeaderViewModel> HeadersList { get; } = new();
    public ObservableCollection<ResponseCookieViewModel> CookiesList { get; } = new();

    public IReadOnlyList<BodyViewMode> ViewModes { get; } = Enum.GetValues<BodyViewMode>();
    public IReadOnlyList<BodyFormat> Formats { get; } = Enum.GetValues<BodyFormat>();

    partial void OnSelectedViewModeChanged(BodyViewMode value)
    {
        IsPreviewMode = value == BodyViewMode.Preview;
        UpdateDisplayBody();
    }

    partial void OnSelectedFormatChanged(BodyFormat value)
    {
        UpdateDisplayBody();
    }

    [RelayCommand]
    private void SetViewMode(string mode)
    {
        SelectedViewMode = mode switch
        {
            "Pretty" => BodyViewMode.Pretty,
            "Raw" => BodyViewMode.Raw,
            "Preview" => BodyViewMode.Preview,
            _ => BodyViewMode.Pretty
        };
    }

    [RelayCommand]
    private async Task CopyBody()
    {
        await CopyToClipboard(DisplayBody);
    }

    [RelayCommand]
    private async Task CopyAllHeaders()
    {
        var sb = new StringBuilder();
        foreach (var header in HeadersList)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }
        await CopyToClipboard(sb.ToString().TrimEnd());
    }

    [RelayCommand]
    private async Task CopyAllCookies()
    {
        var sb = new StringBuilder();
        foreach (var cookie in CookiesList)
        {
            sb.AppendLine($"{cookie.Name}={cookie.Value}");
        }
        await CopyToClipboard(sb.ToString().TrimEnd());
    }

    [RelayCommand]
    private async Task CopyFullResponse()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"HTTP {StatusCode} {StatusDescription}");
        sb.AppendLine();
        sb.AppendLine("=== Headers ===");
        foreach (var header in HeadersList)
        {
            sb.AppendLine($"{header.Key}: {header.Value}");
        }
        if (HasCookies)
        {
            sb.AppendLine();
            sb.AppendLine("=== Cookies ===");
            foreach (var cookie in CookiesList)
            {
                sb.AppendLine($"{cookie.Name}={cookie.Value}");
            }
        }
        sb.AppendLine();
        sb.AppendLine("=== Body ===");
        sb.AppendLine(DisplayBody);
        await CopyToClipboard(sb.ToString());
    }

    private static async Task CopyToClipboard(string text)
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(text);
        }
    }

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

        // Detect format from content type
        DetectedFormat = DetectFormat(ContentType, Body);
        SelectedFormat = BodyFormat.Auto;
        CanPreview = DetectedFormat == BodyFormat.HTML;

        // Populate headers list for table display
        HeadersList.Clear();
        foreach (var header in response.headers)
        {
            HeadersList.Add(new ResponseHeaderViewModel(header.key, header.value));
        }

        // Format headers text (for fallback)
        HeadersText = string.Join(Environment.NewLine,
            response.headers.Select(h => $"{h.key}: {h.value}"));

        // Parse cookies from headers
        ParseCookies(response.headers);

        // Update display
        UpdateDisplayBody();
    }

    public void clear()
    {
        StatusCode = null;
        StatusDescription = string.Empty;
        Body = string.Empty;
        DisplayBody = string.Empty;
        ElapsedMs = 0;
        SizeBytes = 0;
        ContentType = string.Empty;
        ErrorMessage = null;
        HasResponse = false;
        IsSuccess = false;
        HeadersText = string.Empty;
        HeadersList.Clear();
        CookiesText = string.Empty;
        CookiesList.Clear();
        HasCookies = false;
        CookieCount = 0;
        IsPreviewMode = false;
        CanPreview = false;
        SelectedViewMode = BodyViewMode.Pretty;
        SelectedFormat = BodyFormat.Auto;
    }

    private void UpdateDisplayBody()
    {
        var format = SelectedFormat == BodyFormat.Auto ? DetectedFormat : SelectedFormat;
        IsPreviewMode = SelectedViewMode == BodyViewMode.Preview;

        DisplayBody = SelectedViewMode switch
        {
            BodyViewMode.Raw => Body,
            BodyViewMode.Pretty => FormatBody(Body, format),
            BodyViewMode.Preview => Body, // Preview uses the raw HTML for WebView
            _ => Body
        };
    }

    private static BodyFormat DetectFormat(string contentType, string body)
    {
        var ct = contentType.ToLowerInvariant();

        if (ct.Contains("json"))
            return BodyFormat.JSON;
        if (ct.Contains("xml"))
            return BodyFormat.XML;
        if (ct.Contains("html"))
            return BodyFormat.HTML;

        // Try to detect from content
        var trimmed = body.TrimStart();
        if (trimmed.StartsWith('{') || trimmed.StartsWith('['))
            return BodyFormat.JSON;
        if (trimmed.StartsWith('<'))
        {
            if (trimmed.Contains("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Contains("<html", StringComparison.OrdinalIgnoreCase))
                return BodyFormat.HTML;
            return BodyFormat.XML;
        }

        return BodyFormat.Text;
    }

    private static string FormatBody(string body, BodyFormat format)
    {
        if (string.IsNullOrWhiteSpace(body))
            return body;

        try
        {
            return format switch
            {
                BodyFormat.JSON => FormatJson(body),
                BodyFormat.XML => FormatXml(body),
                _ => body
            };
        }
        catch
        {
            return body;
        }
    }

    private static string FormatJson(string input)
    {
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

    private static string FormatXml(string input)
    {
        try
        {
            var doc = XDocument.Parse(input);
            return doc.ToString();
        }
        catch
        {
            return input;
        }
    }

    private void ParseCookies(IEnumerable<key_value_pair_model> headers)
    {
        CookiesList.Clear();

        var cookies = headers
            .Where(h => h.key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            .Select(h => h.value)
            .ToList();

        if (cookies.Any())
        {
            HasCookies = true;
            CookieCount = cookies.Count;

            foreach (var cookie in cookies)
            {
                var vm = ParseCookieToViewModel(cookie);
                CookiesList.Add(vm);
            }

            CookiesText = string.Join(Environment.NewLine + Environment.NewLine, cookies.Select(ParseCookieDetails));
        }
        else
        {
            HasCookies = false;
            CookieCount = 0;
            CookiesText = "No cookies in response";
        }
    }

    private static ResponseCookieViewModel ParseCookieToViewModel(string cookieHeader)
    {
        var vm = new ResponseCookieViewModel();
        var parts = cookieHeader.Split(';').Select(p => p.Trim()).ToList();

        if (parts.Count > 0)
        {
            var nameValue = parts[0].Split('=', 2);
            vm.Name = nameValue[0];
            vm.Value = nameValue.Length > 1 ? nameValue[1] : "";

            foreach (var attr in parts.Skip(1))
            {
                var kv = attr.Split('=', 2);
                var key = kv[0].ToLowerInvariant();
                var value = kv.Length > 1 ? kv[1] : "";

                switch (key)
                {
                    case "domain":
                        vm.Domain = value;
                        break;
                    case "path":
                        vm.Path = value;
                        break;
                    case "expires":
                        vm.Expires = value;
                        break;
                    case "httponly":
                        vm.HttpOnly = true;
                        break;
                    case "secure":
                        vm.Secure = true;
                        break;
                }
            }
        }

        return vm;
    }

    private static string ParseCookieDetails(string cookieHeader)
    {
        var parts = cookieHeader.Split(';').Select(p => p.Trim()).ToList();
        if (parts.Count == 0) return cookieHeader;

        var nameValue = parts[0];
        var attributes = parts.Skip(1).ToList();

        var result = $"• {nameValue}";
        if (attributes.Any())
        {
            result += Environment.NewLine + "  " + string.Join(", ", attributes);
        }
        return result;
    }
}
