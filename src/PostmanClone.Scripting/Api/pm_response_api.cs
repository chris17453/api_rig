using System.Text.Json;
using PostmanClone.Core.Models;

namespace PostmanClone.Scripting.Api;

public class pm_response_api
{
    private readonly http_response_model _response;

    public pm_response_api(http_response_model response)
    {
        _response = response;
    }

    public int code => _response.status_code;
    public string status => _response.status_description;
    public long responseTime => _response.elapsed_ms;
    public long responseSize => _response.size_bytes;

    public object headers => _response.headers
        .ToDictionary(h => h.key, h => h.value);

    public string text()
    {
        return _response.body_string ?? string.Empty;
    }

    public object? json()
    {
        var body = _response.body_string;
        if (string.IsNullOrEmpty(body)) return null;

        try
        {
            return JsonSerializer.Deserialize<object>(body);
        }
        catch
        {
            return null;
        }
    }

    public pm_response_to_api to => new pm_response_to_api(_response);
}

public class pm_response_to_api
{
    private readonly http_response_model _response;

    public pm_response_to_api(http_response_model response)
    {
        _response = response;
    }

    public pm_response_to_api be => this;
    public pm_response_to_api have => this;

    public void ok()
    {
        if (_response.status_code < 200 || _response.status_code >= 300)
        {
            throw new Exception($"Expected successful response but got {_response.status_code}");
        }
    }

    public void status(int expected_code)
    {
        if (_response.status_code != expected_code)
        {
            throw new Exception($"Expected status {expected_code} but got {_response.status_code}");
        }
    }

    public void header(string name)
    {
        if (!_response.headers.Any(h => h.key.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            throw new Exception($"Expected response to have header '{name}'");
        }
    }

    public void body(string content)
    {
        var body = _response.body_string ?? string.Empty;
        if (!body.Contains(content))
        {
            throw new Exception($"Expected response body to contain '{content}'");
        }
    }

    public void jsonBody(string path)
    {
        var body = _response.body_string;
        if (string.IsNullOrEmpty(body))
        {
            throw new Exception("Expected JSON body but response body is empty");
        }
    }
}
