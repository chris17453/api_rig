namespace PostmanClone.Core.Models;

public record oauth2_client_credentials_model
{
    public required string token_url { get; init; }
    public required string client_id { get; init; }
    public required string client_secret { get; init; }
    public string? scope { get; init; }
}
