using FluentAssertions;
using Core.Models;
using Http.Services;
using System.Net;
using System.Text;
using Xunit;

namespace Http.Tests.Services;

public class http_request_executor_tests
{
    [Fact]
    public async Task execute_async_returns_successful_response_for_valid_request()
    {
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"success\": true}", Encoding.UTF8, "application/json")
            };
            return Task.FromResult(response);
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test"
        };

        var response = await executor.execute_async(request, CancellationToken.None);

        response.status_code.Should().Be(200);
        response.is_success.Should().BeTrue();
        response.body_string.Should().Be("{\"success\": true}");
        response.content_type.Should().Be("application/json");
        response.elapsed_ms.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task execute_async_includes_headers_in_request()
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            headers =
            [
                new key_value_pair_model { key = "X-Custom-Header", value = "custom-value", enabled = true },
                new key_value_pair_model { key = "X-Disabled-Header", value = "disabled-value", enabled = false }
            ]
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.Headers.Contains("X-Custom-Header").Should().BeTrue();
        captured_request.Headers.Contains("X-Disabled-Header").Should().BeFalse();
    }

    [Fact]
    public async Task execute_async_includes_query_params_in_url()
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            query_params =
            [
                new key_value_pair_model { key = "param1", value = "value1", enabled = true },
                new key_value_pair_model { key = "param2", value = "value2", enabled = true },
                new key_value_pair_model { key = "disabled", value = "value3", enabled = false }
            ]
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.RequestUri.Should().NotBeNull();
        captured_request.RequestUri!.Query.Should().Contain("param1=value1");
        captured_request.RequestUri.Query.Should().Contain("param2=value2");
        captured_request.RequestUri.Query.Should().NotContain("disabled");
    }

    [Fact]
    public async Task execute_async_sends_json_body_when_body_type_is_json()
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var json_content = "{\"name\": \"test\", \"value\": 123}";
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.post,
            url = "https://api.example.com/test",
            body = new request_body_model
            {
                body_type = request_body_type.json,
                raw_content = json_content
            }
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.Content.Should().NotBeNull();
        var content = await captured_request.Content!.ReadAsStringAsync();
        content.Should().Be(json_content);
        captured_request.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task execute_async_sends_form_urlencoded_body_when_body_type_is_x_www_form_urlencoded()
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.post,
            url = "https://api.example.com/test",
            body = new request_body_model
            {
                body_type = request_body_type.x_www_form_urlencoded,
                form_urlencoded = new Dictionary<string, string>
                {
                    ["field1"] = "value1",
                    ["field2"] = "value2"
                }
            }
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.Content.Should().NotBeNull();
        var content = await captured_request.Content!.ReadAsStringAsync();
        content.Should().Contain("field1=value1");
        content.Should().Contain("field2=value2");
    }

    [Theory]
    [InlineData(http_method.get, "GET")]
    [InlineData(http_method.post, "POST")]
    [InlineData(http_method.put, "PUT")]
    [InlineData(http_method.patch, "PATCH")]
    [InlineData(http_method.delete, "DELETE")]
    [InlineData(http_method.head, "HEAD")]
    [InlineData(http_method.options, "OPTIONS")]
    public async Task execute_async_uses_correct_http_method(http_method method, string expected_method)
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = method,
            url = "https://api.example.com/test"
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.Method.Method.Should().Be(expected_method);
    }

    [Fact]
    public async Task execute_async_applies_basic_auth_when_configured()
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            auth = new auth_config_model
            {
                type = auth_type.basic,
                basic = new basic_auth_model
                {
                    username = "user",
                    password = "pass"
                }
            }
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.Headers.Authorization.Should().NotBeNull();
        captured_request.Headers.Authorization!.Scheme.Should().Be("Basic");
    }

    [Fact]
    public async Task execute_async_applies_bearer_auth_when_configured()
    {
        HttpRequestMessage? captured_request = null;
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            captured_request = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            auth = new auth_config_model
            {
                type = auth_type.bearer,
                bearer = new bearer_auth_model
                {
                    token = "test-token"
                }
            }
        };

        await executor.execute_async(request, CancellationToken.None);

        captured_request.Should().NotBeNull();
        captured_request!.Headers.Authorization.Should().NotBeNull();
        captured_request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        captured_request.Headers.Authorization.Parameter.Should().Be("test-token");
    }

    [Fact]
    public async Task execute_async_returns_error_response_when_timeout_occurs()
    {
        var mock_handler = new mock_http_message_handler(async (request, ct) =>
        {
            await Task.Delay(5000, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            timeout_ms = 100
        };

        var response = await executor.execute_async(request, CancellationToken.None);

        response.status_code.Should().Be(0);
        response.status_description.Should().Be("Timeout");
        response.error_message.Should().Contain("timed out");
        response.is_success.Should().BeFalse();
    }

    [Fact]
    public async Task execute_async_respects_cancellation_token()
    {
        var mock_handler = new mock_http_message_handler(async (request, ct) =>
        {
            await Task.Delay(5000, ct);
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test",
            timeout_ms = 30000
        };

        var cts = new CancellationTokenSource();
        cts.CancelAfter(100);

        var response = await executor.execute_async(request, cts.Token);

        response.status_code.Should().Be(0);
        response.error_message.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task execute_async_returns_error_response_when_request_fails()
    {
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            throw new HttpRequestException("Network error");
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test"
        };

        var response = await executor.execute_async(request, CancellationToken.None);

        response.status_code.Should().Be(0);
        response.status_description.Should().Be("Error");
        response.error_message.Should().Contain("Network error");
        response.is_success.Should().BeFalse();
    }

    [Fact]
    public async Task execute_async_captures_response_headers()
    {
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("test")
            };
            response.Headers.Add("X-Response-Header", "header-value");
            response.Content.Headers.Add("X-Content-Header", "content-value");
            return Task.FromResult(response);
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test"
        };

        var response = await executor.execute_async(request, CancellationToken.None);

        response.headers.Should().Contain(h => h.key == "X-Response-Header");
        response.headers.Should().Contain(h => h.key == "X-Content-Header");
    }

    [Fact]
    public async Task execute_async_calculates_response_size()
    {
        var response_content = "This is a test response with some content";
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response_content)
            };
            return Task.FromResult(response);
        });

        var http_client = new HttpClient(mock_handler);
        var executor = new http_request_executor(http_client);
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test"
        };

        var response = await executor.execute_async(request, CancellationToken.None);

        response.size_bytes.Should().Be(Encoding.UTF8.GetByteCount(response_content));
        response.body_string.Should().Be(response_content);
    }

    private class mock_http_message_handler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public mock_http_message_handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return _handler(request, cancellationToken);
        }
    }
}
