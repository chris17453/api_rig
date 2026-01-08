namespace PostmanClone.Data.Entities;

public class collection_entity
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string name { get; set; }
    public string? description { get; set; }
    public string? version { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; }
    
    // Serialized JSON for auth config and collection variables
    public string? auth_json { get; set; }
    public string? variables_json { get; set; }
    
    // Navigation property
    public List<collection_item_entity> items { get; set; } = new();
}
