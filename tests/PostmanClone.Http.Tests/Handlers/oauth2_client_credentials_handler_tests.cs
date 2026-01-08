using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Http.Handlers;
using System.Net;
using System.Text.Json;
using Xunit;

namespace PostmanClone.Http.Tests.Handlers;

public class oauth2_client_credentials_handler_tests
{
    [Fact]
    public async Task apply_auth_async_requests_token_and_adds_bearer_header()
    {
        var token_response = new { access_token = "test-access-token-12345", token_type = "Bearer", expires_in = 3600 };
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            if (request.RequestUri?.ToString() == "https://oauth.example.com/token")
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(token_response))
                };
                return Task.FromResult(response);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var http_client = new HttpClient(mock_handler);
        var handler = new oauth2_client_credentials_handler(http_client);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.oauth2_client_credentials,
            oauth2_client_credentials = new oauth2_client_credentials_model
            {
                token_url = "https://oauth.example.com/token",
                client_id = "my-client-id",
                client_secret = "my-client-secret"
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("test-access-token-12345");
    }

    [Fact]
    public async Task apply_auth_async_includes_scope_when_provided()
    {
        HttpRequestMessage? captured_request = null;
        var token_response = new { access_token = "token-with-scope", token_type = "Bearer" };
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            if (request.RequestUri?.ToString() == "https://oauth.example.com/token")
            {
                captured_request = request;
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonSerializer.Serialize(token_response))
                };
                return Task.FromResult(response);
            }
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        });

        var http_client = new HttpClient(mock_handler);
        var handler = new oauth2_client_credentials_handler(http_client);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.oauth2_client_credentials,
            oauth2_client_credentials = new oauth2_client_credentials_model
            {
                token_url = "https://oauth.example.com/token",
                client_id = "my-client-id",
                client_secret = "my-client-secret",
                scope = "read write"
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        captured_request.Should().NotBeNull();
        var content = await captured_request!.Content!.ReadAsStringAsync();
        content.Should().Contain("scope=read");
    }

    [Fact]
    public async Task apply_auth_async_does_nothing_when_oauth2_config_is_null()
    {
        var http_client = new HttpClient();
        var handler = new oauth2_client_credentials_handler(http_client);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.oauth2_client_credentials,
            oauth2_client_credentials = null
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task apply_auth_async_throws_when_token_endpoint_returns_error()
    {
        var mock_handler = new mock_http_message_handler((request, ct) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized)
            {
                Content = new StringContent("{\"error\": \"invalid_client\"}")
            };
            return Task.FromResult(response);
        });

        var http_client = new HttpClient(mock_handler);
        var handler = new oauth2_client_credentials_handler(http_client);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.oauth2_client_credentials,
            oauth2_client_credentials = new oauth2_client_credentials_model
            {
                token_url = "https://oauth.example.com/token",
                client_id = "bad-client-id",
                client_secret = "bad-secret"
            }
        };

        var act = async () => await handler.apply_auth_async(request, auth, CancellationToken.None);

        await act.Should().ThrowAsync<HttpRequestException>();
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
