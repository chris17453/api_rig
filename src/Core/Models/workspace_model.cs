namespace Core.Models;

/// <summary>
/// Represents a workspace that groups collections, environments, and history.
/// </summary>
public class workspace_model
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string name { get; set; }
    public string description { get; set; } = string.Empty;
    public string icon { get; set; } = string.Empty;
    public string color { get; set; } = "#3B82F6"; // Default blue
    public bool is_active { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; }
}
