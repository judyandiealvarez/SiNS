using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using sins.Services;

namespace sins.Tests;

[TestClass]
public sealed class KeycloakTokenServiceTests
{
    [TestMethod]
    public async Task PasswordGrantAsync_UsesAuthorityDerivedTokenEndpoint()
    {
        var capturedRequestUri = string.Empty;
        var capturedBody = string.Empty;
        var service = BuildService(
            new Dictionary<string, string?>
            {
                ["Auth:Provider"] = "Keycloak",
                ["Keycloak:Authority"] = "http://keycloak:8080/realms/sins",
                ["Keycloak:ClientId"] = "sins-spa",
                ["Keycloak:Scope"] = "openid profile email"
            },
            request =>
            {
                capturedRequestUri = request.RequestUri?.ToString() ?? string.Empty;
                capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"abc\"}", Encoding.UTF8, "application/json")
                };
            });

        var result = await service.PasswordGrantAsync("admin", "admin123");

        Assert.IsTrue(result.Ok);
        Assert.AreEqual(200, result.StatusCode);
        Assert.AreEqual(
            "http://keycloak:8080/realms/sins/protocol/openid-connect/token",
            capturedRequestUri);

        StringAssert.Contains(capturedBody, "grant_type=password");
        StringAssert.Contains(capturedBody, "client_id=sins-spa");
        StringAssert.Contains(capturedBody, "username=admin");
        StringAssert.Contains(capturedBody, "password=admin123");
    }

    [TestMethod]
    public async Task RefreshTokenAsync_UsesExplicitTokenEndpointAndClientSecret()
    {
        var capturedRequestUri = string.Empty;
        var capturedBody = string.Empty;
        var service = BuildService(
            new Dictionary<string, string?>
            {
                ["Auth:Provider"] = "Keycloak",
                ["Keycloak:TokenEndpoint"] = "http://keycloak:8080/custom/token",
                ["Keycloak:ClientId"] = "sins-spa",
                ["Keycloak:ClientSecret"] = "secret-value"
            },
            request =>
            {
                capturedRequestUri = request.RequestUri?.ToString() ?? string.Empty;
                capturedBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"access_token\":\"abc\"}", Encoding.UTF8, "application/json")
                };
            });

        await service.RefreshTokenAsync("refresh-123");

        Assert.AreEqual("http://keycloak:8080/custom/token", capturedRequestUri);

        StringAssert.Contains(capturedBody, "grant_type=refresh_token");
        StringAssert.Contains(capturedBody, "refresh_token=refresh-123");
        StringAssert.Contains(capturedBody, "client_secret=secret-value");
    }

    [TestMethod]
    public async Task RevokeAsync_UsesAuthorityDerivedRevocationEndpoint()
    {
        var capturedRequest = default(HttpRequestMessage);
        var service = BuildService(
            new Dictionary<string, string?>
            {
                ["Auth:Provider"] = "Keycloak",
                ["Keycloak:Authority"] = "http://keycloak:8080/realms/sins",
                ["Keycloak:ClientId"] = "sins-spa"
            },
            request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(string.Empty, Encoding.UTF8, "application/json")
                };
            });

        await service.RevokeAsync("refresh-123");

        Assert.IsNotNull(capturedRequest);
        Assert.AreEqual(
            "http://keycloak:8080/realms/sins/protocol/openid-connect/revoke",
            capturedRequest.RequestUri?.ToString());
    }

    [TestMethod]
    public void IsEnabled_ReturnsTrueOnlyForKeycloakProvider()
    {
        var keycloakService = BuildService(
            new Dictionary<string, string?> { ["Auth:Provider"] = "Keycloak" },
            _ => new HttpResponseMessage(HttpStatusCode.OK));
        var embeddedService = BuildService(
            new Dictionary<string, string?> { ["Auth:Provider"] = "Embedded" },
            _ => new HttpResponseMessage(HttpStatusCode.OK));

        Assert.IsTrue(keycloakService.IsEnabled());
        Assert.IsFalse(embeddedService.IsEnabled());
    }

    private static KeycloakTokenService BuildService(
        Dictionary<string, string?> values,
        Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var client = new HttpClient(new DelegateHandler(responder), disposeHandler: true);
        var factory = new SingleClientFactory(client);
        return new KeycloakTokenService(configuration, factory);
    }

    private sealed class DelegateHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

        public DelegateHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        {
            _responder = responder;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responder(request));
        }
    }

    private sealed class SingleClientFactory : IHttpClientFactory
    {
        private readonly HttpClient _client;

        public SingleClientFactory(HttpClient client)
        {
            _client = client;
        }

        public HttpClient CreateClient(string name)
        {
            return _client;
        }
    }
}
