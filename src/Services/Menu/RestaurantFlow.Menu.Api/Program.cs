using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Menu.Api;
using RestaurantFlow.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRestaurantFlowObservability("restaurantflow-menu");
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5432;Database=menu;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<MenuDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:Migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<MenuDbContext>().Database.MigrateAsync();
    if (builder.Configuration.GetValue<bool>("Database:MigrationsOnly")) return;
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapGet("/api/menu/items", async (string? category, MenuDbContext dbContext, CancellationToken cancellationToken) =>
{
    var query = dbContext.MenuItems.AsNoTracking().Where(item => item.IsAvailable);
    if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.Category == category);
    return Results.Ok(await query.OrderBy(item => item.Category).ThenBy(item => item.Name).ToListAsync(cancellationToken));
});
app.MapPost("/internal/menu/items/resolve", async (ResolveMenuItemsRequest request, MenuDbContext dbContext, CancellationToken cancellationToken) =>
{
    var itemIds = request.ItemIds.Distinct().ToArray();
    if (itemIds.Length == 0)
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["itemIds"] = ["At least one menu item is required."] });

    var items = await dbContext.MenuItems
        .AsNoTracking()
        .Where(item => itemIds.Contains(item.Id) && item.IsAvailable)
        .Select(item => new MenuItemSnapshot(item.Id, item.Name, item.Price))
        .ToListAsync(cancellationToken);

    return Results.Ok(items);
});
app.MapPost("/api/menu/items", async (CreateMenuItemRequest request, MenuDbContext dbContext, CancellationToken cancellationToken) =>
{
    try
    {
        var item = MenuItem.Create(request.Name, request.Description, request.Category, request.Price);
        dbContext.MenuItems.Add(item);
        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Created($"/api/menu/items/{item.Id}", item);
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["menuItem"] = [exception.Message] });
    }
});
app.MapPatch("/api/menu/items/{id:guid}/availability", async (Guid id, SetAvailabilityRequest request, MenuDbContext dbContext, CancellationToken cancellationToken) =>
{
    var item = await dbContext.MenuItems.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    if (item is null) return Results.NotFound();
    item.SetAvailability(request.IsAvailable);
    await dbContext.SaveChangesAsync(cancellationToken);
    return Results.Ok(item);
});
app.Run();

public sealed record CreateMenuItemRequest(string Name, string Description, string Category, decimal Price);
public sealed record SetAvailabilityRequest(bool IsAvailable);
public sealed record ResolveMenuItemsRequest(IReadOnlyCollection<Guid> ItemIds);
public sealed record MenuItemSnapshot(Guid Id, string Name, decimal Price);
public partial class Program;
