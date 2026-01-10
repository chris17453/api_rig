namespace Data.Entities;

public class environment_variable_entity
{
    public int id { get; set; } // Auto-increment primary key
    public required string key { get; set; }
    public string value { get; set; } = string.Empty;
    
    // Foreign key
    public string environment_id { get; set; } = string.Empty;
    
    // Navigation property
    public environment_entity? environment { get; set; }
}
