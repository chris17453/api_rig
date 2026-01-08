using PostmanClone.Core.Models;

namespace PostmanClone.Http.Handlers;

public class bearer_auth_handler : i_auth_handler
{
    public Task apply_auth_async(HttpRequestMessage request, auth_config_model auth, CancellationToken cancellation_token)
    {
        if (auth.bearer == null)
        {
            return Task.CompletedTask;
        }

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", auth.bearer.token);

        return Task.CompletedTask;
    }
}
