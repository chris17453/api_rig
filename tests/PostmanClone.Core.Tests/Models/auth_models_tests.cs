using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Core.Tests.Models;

public class auth_models_tests
{
    [Fact]
    public void auth_config_discriminates_correctly_when_type_is_basic()
    {
        var auth = new auth_config_model
        {
            type = auth_type.basic,
            basic = new basic_auth_model { username = "user", password = "pass" }
        };

        auth.type.Should().Be(auth_type.basic);
        auth.basic.Should().NotBeNull();
        auth.basic!.username.Should().Be("user");
        auth.basic.password.Should().Be("pass");
        auth.bearer.Should().BeNull();
        auth.api_key.Should().BeNull();
        auth.oauth2_client_credentials.Should().BeNull();
    }

    [Fact]
    public void auth_config_discriminates_correctly_when_type_is_bearer()
    {
        var auth = new auth_config_model
        {
            type = auth_type.bearer,
            bearer = new bearer_auth_model { token = "my-token" }
        };

        auth.type.Should().Be(auth_type.bearer);
        auth.bearer.Should().NotBeNull();
        auth.bearer!.token.Should().Be("my-token");
    }

    [Fact]
    public void auth_config_discriminates_correctly_when_type_is_api_key()
    {
        var auth = new auth_config_model
        {
            type = auth_type.api_key,
            api_key = new api_key_auth_model
            {
                key = "X-API-Key",
                value = "secret-key",
                location = api_key_location.header
            }
        };

        auth.type.Should().Be(auth_type.api_key);
        auth.api_key.Should().NotBeNull();
        auth.api_key!.key.Should().Be("X-API-Key");
        auth.api_key.location.Should().Be(api_key_location.header);
    }

    [Fact]
    public void auth_config_discriminates_correctly_when_type_is_oauth2()
    {
        var auth = new auth_config_model
        {
            type = auth_type.oauth2_client_credentials,
            oauth2_client_credentials = new oauth2_client_credentials_model
            {
                token_url = "https://auth.example.com/token",
                client_id = "client-id",
                client_secret = "client-secret",
                scope = "read write"
            }
        };

        auth.type.Should().Be(auth_type.oauth2_client_credentials);
        auth.oauth2_client_credentials.Should().NotBeNull();
        auth.oauth2_client_credentials!.token_url.Should().Be("https://auth.example.com/token");
        auth.oauth2_client_credentials.scope.Should().Be("read write");
    }

    [Fact]
    public void api_key_location_defaults_to_header_when_not_specified()
    {
        var api_key = new api_key_auth_model
        {
            key = "api-key",
            value = "secret"
        };

        api_key.location.Should().Be(api_key_location.header);
    }
}
