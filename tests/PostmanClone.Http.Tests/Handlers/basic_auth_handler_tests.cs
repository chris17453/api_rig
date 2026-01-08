using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Http.Handlers;
using Xunit;

namespace PostmanClone.Http.Tests.Handlers;

public class basic_auth_handler_tests
{
    [Fact]
    public async Task apply_auth_async_adds_authorization_header_when_basic_auth_provided()
    {
        var handler = new basic_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.basic,
            basic = new basic_auth_model
            {
                username = "testuser",
                password = "testpass"
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        request.Headers.Authorization.Parameter.Should().Be(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("testuser:testpass")));
    }

    [Fact]
    public async Task apply_auth_async_does_nothing_when_basic_auth_is_null()
    {
        var handler = new basic_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.basic,
            basic = null
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task apply_auth_async_encodes_special_characters_correctly()
    {
        var handler = new basic_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.basic,
            basic = new basic_auth_model
            {
                username = "user@domain.com",
                password = "p@ss:w0rd!"
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Basic");
        var decoded = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter!));
        decoded.Should().Be("user@domain.com:p@ss:w0rd!");
    }
}
