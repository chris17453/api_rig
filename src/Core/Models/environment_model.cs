namespace Core.Models;

public record environment_model
{
    public string id { get; init; } = Guid.NewGuid().ToString();
    public required string name { get; init; }
    public IReadOnlyDictionary<string, string> variables { get; init; } = new Dictionary<string, string>();
    public DateTime created_at { get; init; } = DateTime.UtcNow;
    public DateTime? updated_at { get; init; }
}
