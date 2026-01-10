namespace Core.Models;

/// <summary>
/// Represents the type of secret stored in the vault.
/// </summary>
public enum vault_secret_type
{
    api_key,
    bearer_token,
    basic_auth,
    oauth2,
    certificate,
    custom
}

/// <summary>
/// Represents a secret stored in the vault.
/// </summary>
public class vault_secret_model
{
    public string id { get; set; } = Guid.NewGuid().ToString();
    public required string name { get; set; }
    public string description { get; set; } = string.Empty;
    public vault_secret_type secret_type { get; set; } = vault_secret_type.api_key;

    /// <summary>
    /// The encrypted value of the secret. This should be stored encrypted at rest.
    /// </summary>
    public string encrypted_value { get; set; } = string.Empty;

    /// <summary>
    /// Additional metadata stored as JSON (e.g., OAuth2 config, certificate details).
    /// </summary>
    public string metadata_json { get; set; } = string.Empty;

    public DateTime created_at { get; set; } = DateTime.UtcNow;
    public DateTime? updated_at { get; set; }
    public DateTime? last_used_at { get; set; }

    /// <summary>
    /// Optional expiration date for the secret.
    /// </summary>
    public DateTime? expires_at { get; set; }

    /// <summary>
    /// Tags for organizing secrets.
    /// </summary>
    public List<string> tags { get; set; } = new();
}
