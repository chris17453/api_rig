namespace PostmanClone.Core.Models;

public record basic_auth_model
{
    public required string username { get; init; }
    public required string password { get; init; }
}
