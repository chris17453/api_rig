namespace PostmanClone.Core.Models;

public record app_registration_model
{
    public string id { get; init; } = string.Empty;
    public string user_email { get; init; } = string.Empty;
    public string user_name { get; init; } = string.Empty;
    public string organization { get; init; } = string.Empty;
    public bool opted_in { get; init; }
    public DateTime registered_at { get; init; }
    public DateTime? last_updated_at { get; init; }
}
