namespace PostmanClone.Core.Models;

public record http_response_model
{
    public required int status_code { get; init; }
    public required string status_description { get; init; }
    public IReadOnlyList<key_value_pair_model> headers { get; init; } = [];
    public byte[]? body_bytes { get; init; }
    public string? body_string { get; init; }
    public long elapsed_ms { get; init; }
    public long size_bytes { get; init; }
    public string? content_type { get; init; }
    public string? error_message { get; init; }
    public bool is_success => status_code >= 200 && status_code < 300;
}
