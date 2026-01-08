using PostmanClone.Core.Interfaces;
using PostmanClone.Core.Models;

namespace PostmanClone.App.Services;

public class mock_request_executor : i_request_executor
{
    public async Task<http_response_model> execute_async(http_request_model request, CancellationToken cancellation_token)
    {
        // Simulate network delay
        await Task.Delay(500, cancellation_token);

        var mock_body = $$"""
        {
            "message": "Mock response from PostmanClone",
            "request": {
                "method": "{{request.method}}",
                "url": "{{request.url}}"
            },
            "timestamp": "{{DateTime.UtcNow:O}}",
            "data": {
                "id": 1,
                "name": "Sample Item",
                "active": true
            }
        }
        """;

        return new http_response_model
        {
            status_code = 200,
            status_description = "OK",
            headers = new List<key_value_pair_model>
            {
                new() { key = "Content-Type", value = "application/json" },
                new() { key = "X-Mock-Response", value = "true" },
                new() { key = "Date", value = DateTime.UtcNow.ToString("R") }
            },
            body_string = mock_body,
            body_bytes = System.Text.Encoding.UTF8.GetBytes(mock_body),
            elapsed_ms = 487,
            size_bytes = mock_body.Length,
            content_type = "application/json"
        };
    }
}
