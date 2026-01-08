namespace PostmanClone.Core.Models;

public record bearer_auth_model
{
    public required string token { get; init; }
}
