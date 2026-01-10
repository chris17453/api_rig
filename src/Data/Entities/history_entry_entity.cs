using Core.Models;

namespace Data.Entities;

public class history_entry_entity
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string request_name { get; set; }
    public required http_method method { get; set; }
    public required string url { get; set; }
    public int? status_code { get; set; }
    public string? status_description { get; set; }
    public long? elapsed_ms { get; set; }
    public long? response_size_bytes { get; set; }
    public string? environment_id { get; set; }
    public string? environment_name { get; set; }
    public string? collection_id { get; set; }
    public string? collection_item_id { get; set; }
    public string? collection_name { get; set; }
    public string? source_tab_id { get; set; }
    public DateTime executed_at { get; set; } = DateTime.UtcNow;
    public string? error_message { get; set; }
    
    // Serialized JSON for request/response snapshots
    public string? request_snapshot_json { get; set; }
    public string? response_snapshot_json { get; set; }
}
