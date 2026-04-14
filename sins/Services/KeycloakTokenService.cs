using System.Text.Json;

namespace sins.Services;

public class KeycloakTokenService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public KeycloakTokenService(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }

    public bool IsEnabled()
    {
        return string.Equals(_configuration["Auth:Provider"], "Keycloak", StringComparison.OrdinalIgnoreCase);
    }

    public async Task<(bool Ok, int StatusCode, string Body, string? Error)> PasswordGrantAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = ResolveTokenEndpoint();
        if (string.IsNullOrWhiteSpace(tokenEndpoint))
        {
            return (false, 500, string.Empty, "Keycloak token endpoint is not configured.");
        }

        var form = BuildBaseForm();
        form.Add(new("grant_type", "password"));
        form.Add(new("username", username));
        form.Add(new("password", password));

        return await PostFormAsync(tokenEndpoint, form, cancellationToken);
    }

    public async Task<(bool Ok, int StatusCode, string Body, string? Error)> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = ResolveTokenEndpoint();
        if (string.IsNullOrWhiteSpace(tokenEndpoint))
        {
            return (false, 500, string.Empty, "Keycloak token endpoint is not configured.");
        }

        var form = BuildBaseForm();
        form.Add(new("grant_type", "refresh_token"));
        form.Add(new("refresh_token", refreshToken));

        return await PostFormAsync(tokenEndpoint, form, cancellationToken);
    }

    public async Task<(bool Ok, int StatusCode, string Body, string? Error)> RevokeAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        var revocationEndpoint = ResolveRevocationEndpoint();
        if (string.IsNullOrWhiteSpace(revocationEndpoint))
        {
            return (false, 500, string.Empty, "Keycloak revocation endpoint is not configured.");
        }

        var form = BuildBaseForm();
        form.Add(new("token", token));

        return await PostFormAsync(revocationEndpoint, form, cancellationToken);
    }

    private List<KeyValuePair<string, string>> BuildBaseForm()
    {
        var clientId = _configuration["Keycloak:ClientId"] ?? string.Empty;
        var clientSecret = _configuration["Keycloak:ClientSecret"];
        var scope = _configuration["Keycloak:Scope"];

        var form = new List<KeyValuePair<string, string>>
        {
            new("client_id", clientId)
        };
        if (!string.IsNullOrWhiteSpace(clientSecret))
        {
            form.Add(new("client_secret", clientSecret));
        }
        if (!string.IsNullOrWhiteSpace(scope))
        {
            form.Add(new("scope", scope));
        }

        return form;
    }

    private async Task<(bool Ok, int StatusCode, string Body, string? Error)> PostFormAsync(
        string endpoint,
        List<KeyValuePair<string, string>> form,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(KeycloakTokenService));
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new FormUrlEncodedContent(form)
        };
        using var response = await client.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return (response.IsSuccessStatusCode, (int)response.StatusCode, body, null);
    }

    private string? ResolveTokenEndpoint()
    {
        var explicitEndpoint = _configuration["Keycloak:TokenEndpoint"];
        if (!string.IsNullOrWhiteSpace(explicitEndpoint))
        {
            return explicitEndpoint;
        }

        var authority = _configuration["Keycloak:Authority"]?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(authority) ? null : $"{authority}/protocol/openid-connect/token";
    }

    private string? ResolveRevocationEndpoint()
    {
        var explicitEndpoint = _configuration["Keycloak:RevocationEndpoint"];
        if (!string.IsNullOrWhiteSpace(explicitEndpoint))
        {
            return explicitEndpoint;
        }

        var authority = _configuration["Keycloak:Authority"]?.TrimEnd('/');
        return string.IsNullOrWhiteSpace(authority) ? null : $"{authority}/protocol/openid-connect/revoke";
    }
}
