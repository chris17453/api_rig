using System.Text;
using System.Text.Json;
using PostmanClone.Core.Models;

namespace PostmanClone.Http.Handlers;

public class oauth2_client_credentials_handler : i_auth_handler
{
    private readonly HttpClient _http_client;

    public oauth2_client_credentials_handler(HttpClient http_client)
    {
        _http_client = http_client;
    }

    public async Task apply_auth_async(HttpRequestMessage request, auth_config_model auth, CancellationToken cancellation_token)
    {
        if (auth.oauth2_client_credentials == null)
        {
            return;
        }

        var token = await get_access_token_async(auth.oauth2_client_credentials, cancellation_token);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<string> get_access_token_async(oauth2_client_credentials_model oauth2, CancellationToken cancellation_token)
    {
        var form_data = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = oauth2.client_id,
            ["client_secret"] = oauth2.client_secret
        };

        if (!string.IsNullOrWhiteSpace(oauth2.scope))
        {
            form_data["scope"] = oauth2.scope;
        }

        var token_request = new HttpRequestMessage(HttpMethod.Post, oauth2.token_url)
        {
            Content = new FormUrlEncodedContent(form_data)
        };

        var response = await _http_client.SendAsync(token_request, cancellation_token);
        response.EnsureSuccessStatusCode();

        var response_content = await response.Content.ReadAsStringAsync(cancellation_token);
        var token_response = JsonSerializer.Deserialize<JsonElement>(response_content);

        return token_response.GetProperty("access_token").GetString() 
            ?? throw new InvalidOperationException("No access token received from OAuth2 endpoint");
    }
}
