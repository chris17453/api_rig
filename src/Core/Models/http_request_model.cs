namespace Core.Models;

public record http_request_model
{
    public string id { get; init; } = Guid.NewGuid().ToString();
    public required string name { get; init; }
    public required http_method method { get; init; }
    public required string url { get; init; }
    public IReadOnlyList<key_value_pair_model> headers { get; init; } = [];
    public IReadOnlyList<key_value_pair_model> query_params { get; init; } = [];
    public request_body_model? body { get; init; }
    public auth_config_model? auth { get; init; }
    public string? pre_request_script { get; init; }
    public string? post_response_script { get; init; }
    public int timeout_ms { get; init; } = 30000;
}
