using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Consumers;
using RestaurantFlow.Orders.Api.Domain;
using RestaurantFlow.Orders.Api.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5433;Database=orders;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
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

app.MapPost("/api/orders", async (PlaceOrderRequest request, OrdersDbContext dbContext, IPublishEndpoint publisher, CancellationToken cancellationToken) =>
{
    try
    {
        var items = request.Items.Select(item => OrderItem.Create(item.MenuItemId, item.Name, item.Quantity, item.UnitPrice));
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
public sealed record PlaceOrderItemRequest(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);

public partial class Program;
