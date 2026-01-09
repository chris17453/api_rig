namespace PostmanClone.Data.Entities;

public class app_registration_entity
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public string user_email { get; set; } = string.Empty;
    public string user_name { get; set; } = string.Empty;
    public string organization { get; set; } = string.Empty;
    public bool opted_in { get; set; }
    public DateTime registered_at { get; set; } = DateTime.UtcNow;
    public DateTime? last_updated_at { get; set; }
}
