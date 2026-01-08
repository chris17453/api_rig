namespace PostmanClone.Core.Models;

public record postman_collection_model
{
    public string id { get; init; } = Guid.NewGuid().ToString();
    public required string name { get; init; }
    public string? description { get; init; }
    public string? version { get; init; }
    public IReadOnlyList<collection_item_model> items { get; init; } = [];
    public IReadOnlyList<key_value_pair_model> variables { get; init; } = [];
    public auth_config_model? auth { get; init; }
    public DateTime created_at { get; init; } = DateTime.UtcNow;
    public DateTime? updated_at { get; init; }
}
