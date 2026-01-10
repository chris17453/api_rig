using FluentAssertions;
using Core.Models;

namespace Core.Tests.Models;

public class http_request_model_tests
{
    [Fact]
    public void request_creates_with_default_values_when_minimal_properties_set()
    {
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/users"
        };

        request.id.Should().NotBeNullOrEmpty();
        request.headers.Should().BeEmpty();
        request.query_params.Should().BeEmpty();
        request.body.Should().BeNull();
        request.auth.Should().BeNull();
        request.timeout_ms.Should().Be(30000);
    }

    [Fact]
    public void request_preserves_all_properties_when_fully_configured()
    {
        var headers = new List<key_value_pair_model>
        {
            new() { key = "Content-Type", value = "application/json" }
        };

        var body = new request_body_model
        {
            body_type = request_body_type.json,
            raw_content = "{\"name\": \"test\"}"
        };

        var auth = new auth_config_model
        {
            type = auth_type.bearer,
            bearer = new bearer_auth_model { token = "test-token" }
        };

        var request = new http_request_model
        {
            name = "Full Request",
            method = http_method.post,
            url = "https://api.example.com/users",
            headers = headers,
            body = body,
            auth = auth,
            timeout_ms = 60000
        };

        request.name.Should().Be("Full Request");
        request.method.Should().Be(http_method.post);
        request.url.Should().Be("https://api.example.com/users");
        request.headers.Should().HaveCount(1);
        request.body!.body_type.Should().Be(request_body_type.json);
        request.auth!.type.Should().Be(auth_type.bearer);
        request.timeout_ms.Should().Be(60000);
    }
}
