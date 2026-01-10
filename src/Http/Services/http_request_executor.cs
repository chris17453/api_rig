using System.Diagnostics;
using System.Text;
using Core.Interfaces;
using Core.Models;
using Http.Handlers;

namespace Http.Services;

public class http_request_executor : i_request_executor
{
    private readonly HttpClient _http_client;
    private readonly Dictionary<auth_type, i_auth_handler> _auth_handlers;

    public http_request_executor(HttpClient http_client)
    {
        _http_client = http_client;
        _auth_handlers = new Dictionary<auth_type, i_auth_handler>
        {
            [auth_type.basic] = new basic_auth_handler(),
            [auth_type.bearer] = new bearer_auth_handler(),
            [auth_type.api_key] = new api_key_auth_handler(),
            [auth_type.oauth2_client_credentials] = new oauth2_client_credentials_handler(http_client)
        };
    }

    public async Task<http_response_model> execute_async(http_request_model request, CancellationToken cancellation_token)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var http_request = await build_http_request_async(request, cancellation_token);

            using var timeout_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation_token);
            timeout_cts.CancelAfter(TimeSpan.FromMilliseconds(request.timeout_ms));

            var response = await _http_client.SendAsync(http_request, HttpCompletionOption.ResponseContentRead, timeout_cts.Token);

            stopwatch.Stop();

            return await build_response_async(response, stopwatch.ElapsedMilliseconds, cancellation_token);
        }
        catch (OperationCanceledException) when (!cancellation_token.IsCancellationRequested)
        {
            stopwatch.Stop();
            return new http_response_model
            {
                status_code = 0,
                status_description = "Timeout",
                elapsed_ms = stopwatch.ElapsedMilliseconds,
                size_bytes = 0,
                error_message = $"Request timed out after {request.timeout_ms}ms"
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new http_response_model
            {
                status_code = 0,
                status_description = "Error",
                elapsed_ms = stopwatch.ElapsedMilliseconds,
                size_bytes = 0,
                error_message = ex.Message
            };
        }
    }

    private async Task<HttpRequestMessage> build_http_request_async(http_request_model request, CancellationToken cancellation_token)
    {
        var url = build_url_with_query_params(request.url, request.query_params);
        var http_method = map_http_method(request.method);
        var http_request = new HttpRequestMessage(http_method, url);

        foreach (var header in request.headers.Where(h => h.enabled))
        {
            http_request.Headers.TryAddWithoutValidation(header.key, header.value);
        }

        if (request.body != null)
        {
            http_request.Content = build_content(request.body);
        }

        if (request.auth != null && request.auth.type != auth_type.none)
        {
            if (_auth_handlers.TryGetValue(request.auth.type, out var handler))
            {
                await handler.apply_auth_async(http_request, request.auth, cancellation_token);
            }
        }

        return http_request;
    }

    private static string build_url_with_query_params(string url, IReadOnlyList<key_value_pair_model> query_params)
    {
        if (query_params.Count == 0)
        {
            return url;
        }

        var enabled_params = query_params.Where(p => p.enabled).ToList();
        if (enabled_params.Count == 0)
        {
            return url;
        }

        var uri_builder = new UriBuilder(url);
        var query = System.Web.HttpUtility.ParseQueryString(uri_builder.Query);

        foreach (var param in enabled_params)
        {
            query[param.key] = param.value;
        }

        uri_builder.Query = query.ToString();
        return uri_builder.Uri.ToString();
    }

    private static HttpMethod map_http_method(http_method method)
    {
        return method switch
        {
            http_method.get => HttpMethod.Get,
            http_method.post => HttpMethod.Post,
            http_method.put => HttpMethod.Put,
            http_method.patch => HttpMethod.Patch,
            http_method.delete => HttpMethod.Delete,
            http_method.head => HttpMethod.Head,
            http_method.options => HttpMethod.Options,
            http_method.trace => HttpMethod.Trace,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "Unsupported HTTP method")
        };
    }

    private static HttpContent build_content(request_body_model body)
    {
        return body.body_type switch
        {
            request_body_type.none => new StringContent(""),
            request_body_type.raw => new StringContent(body.raw_content ?? "", Encoding.UTF8, "text/plain"),
            request_body_type.json => new StringContent(body.raw_content ?? "", Encoding.UTF8, "application/json"),
            request_body_type.form_data => new FormUrlEncodedContent(body.form_data ?? new Dictionary<string, string>()),
            request_body_type.x_www_form_urlencoded => new FormUrlEncodedContent(body.form_urlencoded ?? new Dictionary<string, string>()),
            _ => throw new ArgumentOutOfRangeException(nameof(body.body_type), body.body_type, "Unsupported body type")
        };
    }

    private static async Task<http_response_model> build_response_async(HttpResponseMessage response, long elapsed_ms, CancellationToken cancellation_token)
    {
        var body_bytes = await response.Content.ReadAsByteArrayAsync(cancellation_token);
        var body_string = body_bytes.Length > 0 ? Encoding.UTF8.GetString(body_bytes) : null;

        var headers = response.Headers
            .Concat(response.Content.Headers)
            .Select(h => new key_value_pair_model
            {
                key = h.Key,
                value = string.Join(", ", h.Value),
                enabled = true
            })
            .ToList();

        var content_type = response.Content.Headers.ContentType?.MediaType;

        return new http_response_model
        {
            status_code = (int)response.StatusCode,
            status_description = response.ReasonPhrase ?? response.StatusCode.ToString(),
            headers = headers,
            body_bytes = body_bytes,
            body_string = body_string,
            elapsed_ms = elapsed_ms,
            size_bytes = body_bytes.Length,
            content_type = content_type
        };
    }
}
