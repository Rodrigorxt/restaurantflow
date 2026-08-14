using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Consumers;
using RestaurantFlow.Orders.Api.Domain;
using RestaurantFlow.Orders.Api.Infrastructure;
using RestaurantFlow.Orders.Api.Integrations;
using RestaurantFlow.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRestaurantFlowObservability("restaurantflow-orders");
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5433;Database=orders;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
builder.Services
    .AddHttpClient<MenuCatalogClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:Menu"] ?? "http://localhost:5001");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddStandardResilienceHandler();
builder.Services.AddMassTransit(configurator =>
{
    configurator.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("orders", false));
    configurator.AddConsumers(typeof(Program).Assembly);
    configurator.AddEntityFrameworkOutbox<OrdersDbContext>(outbox =>
    {
        outbox.UsePostgres();
        outbox.UseBusOutbox();
    });
    configurator.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        rabbit.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(500)));
        rabbit.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
}
app.UseExceptionHandler();
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapPost("/api/orders", async (PlaceOrderRequest request, MenuCatalogClient menuCatalog, OrdersDbContext dbContext, IPublishEndpoint publisher, CancellationToken cancellationToken) =>
{
    try
    {
        var requestedItems = request.Items.ToArray();
        if (requestedItems.Length == 0)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["At least one item is required."] });
        if (requestedItems.Any(item => item.Quantity <= 0))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Every item quantity must be positive."] });
        if (requestedItems.Select(item => item.MenuItemId).Distinct().Count() != requestedItems.Length)
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["items"] = ["Duplicate menu items are not allowed."] });

        var menuItems = await menuCatalog.ResolveAsync(requestedItems.Select(item => item.MenuItemId), cancellationToken);
        var unavailableItemIds = requestedItems.Select(item => item.MenuItemId).Except(menuItems.Keys).ToArray();
        if (unavailableItemIds.Length > 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = [$"Menu items are missing or unavailable: {string.Join(", ", unavailableItemIds)}."]
            });

        var items = requestedItems.Select(item =>
        {
            var menuItem = menuItems[item.MenuItemId];
            return OrderItem.Create(menuItem.Id, menuItem.Name, item.Quantity, menuItem.Price);
        });
        var order = Order.Place(request.CustomerId, request.CustomerEmail, items);
        dbContext.Orders.Add(order);

        await publisher.Publish(new OrderSubmitted(
            Guid.NewGuid(),
            order.Id,
            order.CustomerId,
            order.CustomerEmail,
            request.PaymentReference,
            order.Total,
            order.Items.Select(item => new OrderItemSnapshot(item.MenuItemId, item.Name, item.Quantity, item.UnitPrice)).ToArray(),
            DateTimeOffset.UtcNow), cancellationToken);

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Accepted($"/api/orders/{order.Id}", new { order.Id, order.Status, order.Total });
    }
    catch (ArgumentException exception)
    {
        return Results.ValidationProblem(new Dictionary<string, string[]> { ["order"] = [exception.Message] });
    }
});

app.MapGet("/api/orders/{id:guid}", async (Guid id, OrdersDbContext dbContext, CancellationToken cancellationToken) =>
{
    var order = await dbContext.Orders.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    return order is null ? Results.NotFound() : Results.Ok(order);
});

app.Run();

public sealed record PlaceOrderRequest(Guid CustomerId, string CustomerEmail, string PaymentReference, IReadOnlyCollection<PlaceOrderItemRequest> Items);
public sealed record PlaceOrderItemRequest(Guid MenuItemId, int Quantity);

public partial class Program;
