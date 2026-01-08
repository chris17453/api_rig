namespace PostmanClone.Core.Models;

public record api_key_auth_model
{
    public required string key { get; init; }
    public required string value { get; init; }
    public api_key_location location { get; init; } = api_key_location.header;
}
