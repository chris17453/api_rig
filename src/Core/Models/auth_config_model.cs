namespace Core.Models;

public record auth_config_model
{
    public auth_type type { get; init; } = auth_type.none;
    public basic_auth_model? basic { get; init; }
    public bearer_auth_model? bearer { get; init; }
    public api_key_auth_model? api_key { get; init; }
    public oauth2_client_credentials_model? oauth2_client_credentials { get; init; }
}
