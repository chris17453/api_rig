namespace Core.Models;

public record history_entry_model
{
    public string id { get; init; } = Guid.NewGuid().ToString();
    public required string request_name { get; init; }
    public required http_method method { get; init; }
    public required string url { get; init; }
    public int? status_code { get; init; }
    public string? status_description { get; init; }
    public long? elapsed_ms { get; init; }
    public long? response_size_bytes { get; init; }
    public string? environment_id { get; init; }
    public string? environment_name { get; init; }
    public string? collection_id { get; init; }
    public string? collection_item_id { get; init; }
    public string? collection_name { get; init; }
    public string? source_tab_id { get; init; }
    public DateTime executed_at { get; init; } = DateTime.UtcNow;
    public string? error_message { get; init; }
    public http_request_model? request_snapshot { get; init; }
    public http_response_model? response_snapshot { get; init; }
}
