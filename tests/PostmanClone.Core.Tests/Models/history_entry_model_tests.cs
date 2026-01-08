using FluentAssertions;
using PostmanClone.Core.Models;

namespace PostmanClone.Core.Tests.Models;

public class history_entry_model_tests
{
    [Fact]
    public void history_entry_contains_required_fields_when_created()
    {
        var entry = new history_entry_model
        {
            request_name = "Get Users",
            method = http_method.get,
            url = "https://api.example.com/users",
            status_code = 200,
            status_description = "OK",
            elapsed_ms = 150,
            response_size_bytes = 1024,
            environment_id = "env-1",
            environment_name = "Production"
        };

        entry.id.Should().NotBeNullOrEmpty();
        entry.request_name.Should().Be("Get Users");
        entry.method.Should().Be(http_method.get);
        entry.url.Should().Be("https://api.example.com/users");
        entry.status_code.Should().Be(200);
        entry.elapsed_ms.Should().Be(150);
        entry.executed_at.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void history_entry_stores_error_message_when_request_fails()
    {
        var entry = new history_entry_model
        {
            request_name = "Failed Request",
            method = http_method.post,
            url = "https://api.example.com/error",
            error_message = "Connection timeout"
        };

        entry.status_code.Should().BeNull();
        entry.error_message.Should().Be("Connection timeout");
    }

    [Fact]
    public void history_entry_stores_request_snapshot_when_provided()
    {
        var request = new http_request_model
        {
            name = "Test Request",
            method = http_method.get,
            url = "https://api.example.com/test"
        };

        var entry = new history_entry_model
        {
            request_name = request.name,
            method = request.method,
            url = request.url,
            request_snapshot = request
        };

        entry.request_snapshot.Should().NotBeNull();
        entry.request_snapshot!.id.Should().Be(request.id);
    }
}
