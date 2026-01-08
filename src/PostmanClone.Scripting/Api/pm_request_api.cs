using PostmanClone.Core.Models;

namespace PostmanClone.Scripting.Api;

public class pm_request_api
{
    private readonly http_request_model _request;

    public pm_request_api(http_request_model request)
    {
        _request = request;
    }

    public string url => _request.url;
    public string method => _request.method.ToString().ToUpperInvariant();

    public object headers => _request.headers
        .Where(h => h.enabled)
        .ToDictionary(h => h.key, h => h.value);

    public object? body
    {
        get
        {
            if (_request.body is null) return null;
            return new
            {
                mode = _request.body.body_type.ToString(),
                raw = _request.body.raw_content
            };
        }
    }
}
