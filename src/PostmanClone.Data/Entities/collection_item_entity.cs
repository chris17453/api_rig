using PostmanClone.Core.Models;

namespace PostmanClone.Data.Entities;

public class collection_item_entity
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string name { get; set; }
    public string? description { get; set; }
    public bool is_folder { get; set; }
    public string? folder_path { get; set; }
    public int sort_order { get; set; }
    
    // Request properties (null for folders)
    public http_method? request_method { get; set; }
    public string? request_url { get; set; }
    public string? request_headers_json { get; set; }
    public string? request_query_params_json { get; set; }
    public string? request_body_json { get; set; }
    public string? request_auth_json { get; set; }
    public string? pre_request_script { get; set; }
    public string? post_response_script { get; set; }
    public int timeout_ms { get; set; } = 30000;
    
    // Foreign key
    public string collection_id { get; set; } = string.Empty;
    
    // Navigation property
    public collection_entity? collection { get; set; }
    
    // Parent folder reference (null for root items)
    public string? parent_item_id { get; set; }
    public collection_item_entity? parent_item { get; set; }
    public List<collection_item_entity> children { get; set; } = new();
}
