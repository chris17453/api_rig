namespace Core.Models;

public record key_value_pair_model
{
    public required string key { get; init; }
    public required string value { get; init; }
    public bool enabled { get; init; } = true;
}
