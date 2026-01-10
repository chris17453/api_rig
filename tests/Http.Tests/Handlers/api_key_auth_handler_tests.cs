using FluentAssertions;
using Core.Models;
using Http.Handlers;
using Xunit;

namespace Http.Tests.Handlers;

public class api_key_auth_handler_tests
{
    [Fact]
    public async Task apply_auth_async_adds_api_key_to_header_when_location_is_header()
    {
        var handler = new api_key_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.api_key,
            api_key = new api_key_auth_model
            {
                key = "X-API-Key",
                value = "my-api-key-value",
                location = api_key_location.header
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Contains("X-API-Key").Should().BeTrue();
        request.Headers.GetValues("X-API-Key").First().Should().Be("my-api-key-value");
    }

    [Fact]
    public async Task apply_auth_async_adds_api_key_to_query_when_location_is_query()
    {
        var handler = new api_key_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/endpoint");
        var auth = new auth_config_model
        {
            type = auth_type.api_key,
            api_key = new api_key_auth_model
            {
                key = "apiKey",
                value = "my-api-key-value",
                location = api_key_location.query
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.RequestUri.Should().NotBeNull();
        request.RequestUri!.Query.Should().Contain("apiKey=my-api-key-value");
    }

    [Fact]
    public async Task apply_auth_async_preserves_existing_query_params_when_adding_api_key()
    {
        var handler = new api_key_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/endpoint?param1=value1&param2=value2");
        var auth = new auth_config_model
        {
            type = auth_type.api_key,
            api_key = new api_key_auth_model
            {
                key = "apiKey",
                value = "my-key",
                location = api_key_location.query
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.RequestUri.Should().NotBeNull();
        request.RequestUri!.Query.Should().Contain("param1=value1");
        request.RequestUri.Query.Should().Contain("param2=value2");
        request.RequestUri.Query.Should().Contain("apiKey=my-key");
    }

    [Fact]
    public async Task apply_auth_async_does_nothing_when_api_key_is_null()
    {
        var handler = new api_key_auth_handler();
        var original_uri = "https://api.example.com/endpoint";
        var request = new HttpRequestMessage(HttpMethod.Get, original_uri);
        var auth = new auth_config_model
        {
            type = auth_type.api_key,
            api_key = null
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.RequestUri.Should().NotBeNull();
        request.RequestUri!.ToString().Should().Be(original_uri);
        request.Headers.Should().BeEmpty();
    }
}
