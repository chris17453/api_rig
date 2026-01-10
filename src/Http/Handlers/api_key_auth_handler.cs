using Core.Models;

namespace Http.Handlers;

public class api_key_auth_handler : i_auth_handler
{
    public Task apply_auth_async(HttpRequestMessage request, auth_config_model auth, CancellationToken cancellation_token)
    {
        if (auth.api_key == null)
        {
            return Task.CompletedTask;
        }

        if (auth.api_key.location == api_key_location.header)
        {
            request.Headers.TryAddWithoutValidation(auth.api_key.key, auth.api_key.value);
        }
        else if (auth.api_key.location == api_key_location.query)
        {
            var uri_builder = new UriBuilder(request.RequestUri!);
            var query = System.Web.HttpUtility.ParseQueryString(uri_builder.Query);
            query[auth.api_key.key] = auth.api_key.value;
            uri_builder.Query = query.ToString();
            request.RequestUri = uri_builder.Uri;
        }

        return Task.CompletedTask;
    }
}
