using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Http.Handlers;
using Xunit;

namespace PostmanClone.Http.Tests.Handlers;

public class bearer_auth_handler_tests
{
    [Fact]
    public async Task apply_auth_async_adds_bearer_token_when_bearer_auth_provided()
    {
        var handler = new bearer_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.bearer,
            bearer = new bearer_auth_model
            {
                token = "my-secret-token-12345"
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be("my-secret-token-12345");
    }

    [Fact]
    public async Task apply_auth_async_does_nothing_when_bearer_auth_is_null()
    {
        var handler = new bearer_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var auth = new auth_config_model
        {
            type = auth_type.bearer,
            bearer = null
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().BeNull();
    }

    [Fact]
    public async Task apply_auth_async_handles_jwt_tokens()
    {
        var handler = new bearer_auth_handler();
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com");
        var jwt_token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIn0.dozjgNryP4J3jVmNHl0w5N_XgL0n3I9PlFUP0THsR8U";
        var auth = new auth_config_model
        {
            type = auth_type.bearer,
            bearer = new bearer_auth_model
            {
                token = jwt_token
            }
        };

        await handler.apply_auth_async(request, auth, CancellationToken.None);

        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Parameter.Should().Be(jwt_token);
    }
}
