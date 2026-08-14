using System.Net.Http.Json;

namespace RestaurantFlow.Orders.Api.Integrations;

public sealed class MenuCatalogClient(HttpClient httpClient)
{
    public async Task<IReadOnlyDictionary<Guid, MenuItemSnapshot>> ResolveAsync(
        IEnumerable<Guid> itemIds,
        CancellationToken cancellationToken)
    {
        var request = new ResolveMenuItemsRequest(itemIds.Distinct().ToArray());
        using var response = await httpClient.PostAsJsonAsync("/internal/menu/items/resolve", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<MenuItemSnapshot[]>(cancellationToken)
            ?? throw new InvalidOperationException("Menu service returned an empty response.");

        return items.ToDictionary(item => item.Id);
    }
}

public sealed record ResolveMenuItemsRequest(IReadOnlyCollection<Guid> ItemIds);
public sealed record MenuItemSnapshot(Guid Id, string Name, decimal Price);
