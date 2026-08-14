using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Menu.Api;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5432;Database=menu;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<MenuDbContext>(options => options.UseNpgsql(connectionString));

var app = builder.Build();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapGet("/api/menu/items", async (string? category, MenuDbContext dbContext, CancellationToken cancellationToken) =>
{
    var query = dbContext.MenuItems.AsNoTracking().Where(item => item.IsAvailable);
    if (!string.IsNullOrWhiteSpace(category)) query = query.Where(item => item.Category == category);
    return Results.Ok(await query.OrderBy(item => item.Category).ThenBy(item => item.Name).ToListAsync(cancellationToken));
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
public partial class Program;

