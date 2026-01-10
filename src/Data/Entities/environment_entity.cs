namespace Data.Entities;

public class environment_entity
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string name { get; set; }
    public bool is_active { get; set; }
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; }
    
    // Navigation property
    public List<environment_variable_entity> variables { get; set; } = new();
}
