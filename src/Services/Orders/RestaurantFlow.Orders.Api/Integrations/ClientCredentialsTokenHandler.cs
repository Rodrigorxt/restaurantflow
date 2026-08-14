using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RestaurantFlow.Orders.Api.Integrations;

public sealed class ClientCredentialsTokenHandler(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration) : DelegatingHandler
{
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private string? accessToken;
    private DateTimeOffset expiresAt;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (configuration.GetValue("Authentication:Enabled", false))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                await GetAccessTokenAsync(cancellationToken));
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (accessToken is not null && expiresAt > DateTimeOffset.UtcNow.AddSeconds(30)) return accessToken;

        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (accessToken is not null && expiresAt > DateTimeOffset.UtcNow.AddSeconds(30)) return accessToken;

            var tokenEndpoint = configuration["Authentication:TokenEndpoint"]
                ?? throw new InvalidOperationException("Authentication:TokenEndpoint is required.");
            var clientId = configuration["Authentication:ClientId"]
                ?? throw new InvalidOperationException("Authentication:ClientId is required.");
            var clientSecret = configuration["Authentication:ClientSecret"]
                ?? throw new InvalidOperationException("Authentication:ClientSecret is required.");

            using var tokenClient = httpClientFactory.CreateClient("identity");
            using var response = await tokenClient.PostAsync(tokenEndpoint, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            }), cancellationToken);
            response.EnsureSuccessStatusCode();

            var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken)
                ?? throw new InvalidOperationException("Identity provider returned an empty token response.");
            accessToken = token.AccessToken;
            expiresAt = DateTimeOffset.UtcNow.AddSeconds(token.ExpiresIn);
            return accessToken;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    private sealed record TokenResponse(
        [property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}
