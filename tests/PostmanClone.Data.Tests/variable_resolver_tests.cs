using FluentAssertions;
using PostmanClone.Core.Models;
using PostmanClone.Data.Services;
using Xunit;

namespace PostmanClone.Data.Tests;

public class variable_resolver_tests
{
    private readonly variable_resolver _resolver = new();

    [Fact]
    public void resolve_replaces_variable_when_found()
    {
        var variables = new Dictionary<string, string> { { "base_url", "https://api.example.com" } };

        var result = _resolver.resolve("{{base_url}}/users", variables);

        result.Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void resolve_replaces_multiple_variables_when_found()
    {
        var variables = new Dictionary<string, string>
        {
            { "base_url", "https://api.example.com" },
            { "version", "v2" }
        };

        var result = _resolver.resolve("{{base_url}}/{{version}}/users", variables);

        result.Should().Be("https://api.example.com/v2/users");
    }

    [Fact]
    public void resolve_leaves_variable_when_not_found_and_policy_is_leave_as_is()
    {
        var variables = new Dictionary<string, string>();

        var result = _resolver.resolve("{{missing}}/users", variables, variable_resolution_policy.leave_as_is);

        result.Should().Be("{{missing}}/users");
    }

    [Fact]
    public void resolve_replaces_with_empty_when_not_found_and_policy_is_replace_with_empty()
    {
        var variables = new Dictionary<string, string>();

        var result = _resolver.resolve("prefix_{{missing}}_suffix", variables, variable_resolution_policy.replace_with_empty);

        result.Should().Be("prefix__suffix");
    }

    [Fact]
    public void resolve_throws_when_not_found_and_policy_is_throw_error()
    {
        var variables = new Dictionary<string, string>();

        var action = () => _resolver.resolve("{{missing}}", variables, variable_resolution_policy.throw_error);

        action.Should().Throw<InvalidOperationException>().WithMessage("*missing*not found*");
    }

    [Fact]
    public void resolve_returns_input_when_no_variables_present()
    {
        var variables = new Dictionary<string, string> { { "unused", "value" } };

        var result = _resolver.resolve("no variables here", variables);

        result.Should().Be("no variables here");
    }

    [Fact]
    public void resolve_returns_empty_when_input_is_empty()
    {
        var variables = new Dictionary<string, string>();

        var result = _resolver.resolve("", variables);

        result.Should().BeEmpty();
    }

    [Fact]
    public void resolve_request_replaces_url_variables()
    {
        var variables = new Dictionary<string, string> { { "base_url", "https://api.example.com" } };
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "{{base_url}}/users"
        };

        var result = _resolver.resolve_request(request, variables);

        result.url.Should().Be("https://api.example.com/users");
    }

    [Fact]
    public void resolve_request_replaces_header_variables()
    {
        var variables = new Dictionary<string, string> { { "token", "abc123" } };
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.get,
            url = "https://api.example.com",
            headers = new List<key_value_pair_model>
            {
                new() { key = "Authorization", value = "Bearer {{token}}" }
            }
        };

        var result = _resolver.resolve_request(request, variables);

        result.headers[0].value.Should().Be("Bearer abc123");
    }

    [Fact]
    public void resolve_request_replaces_body_variables()
    {
        var variables = new Dictionary<string, string> { { "user_id", "12345" } };
        var request = new http_request_model
        {
            name = "Test",
            method = http_method.post,
            url = "https://api.example.com",
            body = new request_body_model
            {
                body_type = request_body_type.json,
                raw_content = "{\"id\": \"{{user_id}}\"}"
            }
        };

        var result = _resolver.resolve_request(request, variables);

        result.body!.raw_content.Should().Be("{\"id\": \"12345\"}");
    }

    [Fact]
    public void resolve_key_value_pairs_replaces_both_key_and_value()
    {
        var variables = new Dictionary<string, string>
        {
            { "header_name", "X-Custom" },
            { "header_value", "custom-value" }
        };
        var pairs = new List<key_value_pair_model>
        {
            new() { key = "{{header_name}}", value = "{{header_value}}" }
        };

        var result = _resolver.resolve_key_value_pairs(pairs, variables);

        result[0].key.Should().Be("X-Custom");
        result[0].value.Should().Be("custom-value");
    }
}
