using Core.Models;

namespace Http.Handlers;

public interface i_auth_handler
{
    Task apply_auth_async(HttpRequestMessage request, auth_config_model auth, CancellationToken cancellation_token);
}
