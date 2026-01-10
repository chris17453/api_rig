using System.Text;
using Core.Models;

namespace Http.Handlers;

public class basic_auth_handler : i_auth_handler
{
    public Task apply_auth_async(HttpRequestMessage request, auth_config_model auth, CancellationToken cancellation_token)
    {
        if (auth.basic == null)
        {
            return Task.CompletedTask;
        }

        var credentials = $"{auth.basic.username}:{auth.basic.password}";
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);

        return Task.CompletedTask;
    }
}
