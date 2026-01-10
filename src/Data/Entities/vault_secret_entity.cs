using Core.Models;

namespace Data.Entities;

public class vault_secret_entity
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string name { get; set; }
    public string description { get; set; } = string.Empty;
    public vault_secret_type secret_type { get; set; } = vault_secret_type.api_key;
    public string encrypted_value { get; set; } = string.Empty;
    public string metadata_json { get; set; } = string.Empty;
    public string tags_json { get; set; } = string.Empty;
    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; }
    public DateTime? last_used_at { get; set; }
    public DateTime? expires_at { get; set; }
}
