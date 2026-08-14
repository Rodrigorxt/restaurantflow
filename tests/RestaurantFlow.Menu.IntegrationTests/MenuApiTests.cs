using System.Net;
using System.Net.Http.Json;

namespace RestaurantFlow.Menu.IntegrationTests;

public sealed class MenuApiTests(MenuApiFactory factory) : IClassFixture<MenuApiFactory>
{
    private readonly HttpClient client = factory.CreateClient();

    [Fact]
    public async Task Available_item_can_be_created_listed_and_resolved_for_order_pricing()
    {
        var createResponse = await client.PostAsJsonAsync("/api/menu/items", new
        {
            name = "Truffle Burger",
            description = "Beef, truffle sauce and aged cheese",
            category = "Main",
            price = 29.90m
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MenuItemResponse>();
        Assert.NotNull(created);

        var menu = await client.GetFromJsonAsync<MenuItemResponse[]>("/api/menu/items");
        Assert.Contains(menu!, item => item.Id == created.Id);

        var resolveResponse = await client.PostAsJsonAsync("/internal/menu/items/resolve", new
        {
            itemIds = new[] { created.Id }
        });
        var snapshots = await resolveResponse.Content.ReadFromJsonAsync<MenuItemSnapshotResponse[]>();

        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        var snapshot = Assert.Single(snapshots!);
        Assert.Equal(created.Id, snapshot.Id);
        Assert.Equal("Truffle Burger", snapshot.Name);
        Assert.Equal(29.90m, snapshot.Price);
    }

    [Fact]
    public async Task Unavailable_item_is_excluded_from_order_price_resolution()
    {
        var createResponse = await client.PostAsJsonAsync("/api/menu/items", new
        {
            name = "Seasonal Dessert",
            description = "Limited availability",
            category = "Dessert",
            price = 18.50m
        });
        var created = await createResponse.Content.ReadFromJsonAsync<MenuItemResponse>();

        var availabilityResponse = await client.PatchAsJsonAsync(
            $"/api/menu/items/{created!.Id}/availability",
            new { isAvailable = false });
        Assert.Equal(HttpStatusCode.OK, availabilityResponse.StatusCode);

        var resolveResponse = await client.PostAsJsonAsync("/internal/menu/items/resolve", new
        {
            itemIds = new[] { created.Id }
        });
        var snapshots = await resolveResponse.Content.ReadFromJsonAsync<MenuItemSnapshotResponse[]>();

        Assert.Equal(HttpStatusCode.OK, resolveResponse.StatusCode);
        Assert.Empty(snapshots!);
    }

    private sealed record MenuItemResponse(Guid Id, string Name, decimal Price, bool IsAvailable);
    private sealed record MenuItemSnapshotResponse(Guid Id, string Name, decimal Price);
}
