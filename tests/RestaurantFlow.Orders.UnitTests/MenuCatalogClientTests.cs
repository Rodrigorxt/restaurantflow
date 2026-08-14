using System.Net;
using System.Text;
using RestaurantFlow.Orders.Api.Integrations;

namespace RestaurantFlow.Orders.UnitTests;

public sealed class MenuCatalogClientTests
{
    [Fact]
    public async Task Resolve_returns_server_owned_item_data()
    {
        var itemId = Guid.NewGuid();
        using var httpClient = CreateClient(HttpStatusCode.OK, $$"""
            [{"id":"{{itemId}}","name":"Truffle Burger","price":29.90}]
            """);
        var client = new MenuCatalogClient(httpClient);

        var result = await client.ResolveAsync([itemId], CancellationToken.None);

        Assert.Equal("Truffle Burger", result[itemId].Name);
        Assert.Equal(29.90m, result[itemId].Price);
    }

    [Fact]
    public async Task Resolve_propagates_an_unsuccessful_menu_response()
    {
        using var httpClient = CreateClient(HttpStatusCode.ServiceUnavailable, "{}");
        var client = new MenuCatalogClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.ResolveAsync([Guid.NewGuid()], CancellationToken.None));
    }

    private static HttpClient CreateClient(HttpStatusCode statusCode, string content) =>
        new(new StubHandler(new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(content, Encoding.UTF8, "application/json")
        }))
        {
            BaseAddress = new Uri("http://menu.test")
        };

    private sealed class StubHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(response);
    }
}
