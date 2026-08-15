using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Consumers;
using RestaurantFlow.Orders.Api.Domain;
using RestaurantFlow.Orders.Api.Infrastructure;
using RestaurantFlow.Orders.Api.Integrations;
using RestaurantFlow.Observability;
using RestaurantFlow.Security;
using RestaurantFlow.Orders.Api.Workflow;
using Quartz;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRestaurantFlowObservability("restaurantflow-orders");
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5433;Database=orders;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddRestaurantFlowSecurity(builder.Configuration);
builder.Services.AddDbContext<OrdersDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.Configure<OrderWorkflowOptions>(
    builder.Configuration.GetSection(OrderWorkflowOptions.SectionName));
builder.Services.AddQuartz(quartz =>
{
    quartz.SchedulerName = "RestaurantFlow-Order-Workflow-Scheduler";
    quartz.SchedulerId = "AUTO";
    quartz.UseDefaultThreadPool(threadPool => threadPool.MaxConcurrency = 10);
    quartz.UsePersistentStore(store =>
    {
        store.UseProperties = true;
        store.PerformSchemaValidation = true;
        store.RetryInterval = TimeSpan.FromSeconds(15);
        store.UsePostgres(postgres =>
        {
            postgres.ConnectionString = connectionString;
            postgres.TablePrefix = "quartz.qrtz_";
        });
        store.UseSystemTextJsonSerializer();
        store.UseClustering(cluster =>
        {
            cluster.CheckinInterval = TimeSpan.FromSeconds(10);
            cluster.CheckinMisfireThreshold = TimeSpan.FromSeconds(20);
        });
    });
});
builder.Services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);
builder.Services.AddHttpClient("identity");
builder.Services.AddTransient<ClientCredentialsTokenHandler>();
builder.Services
    .AddHttpClient<MenuCatalogClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:Menu"] ?? "http://localhost:5232");
        client.Timeout = TimeSpan.FromSeconds(10);
    })
    .AddHttpMessageHandler<ClientCredentialsTokenHandler>()
    .AddStandardResilienceHandler();
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddPublishMessageScheduler();
    configurator.AddQuartzConsumers();
    configurator.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("orders", false));
    configurator.AddConsumers(typeof(Program).Assembly);
    configurator.AddSagaStateMachine<OrderWorkflowStateMachine, OrderWorkflowState>()
        .EntityFrameworkRepository(repository =>
        {
            repository.ExistingDbContext<OrdersDbContext>();
            repository.UsePostgres();
            repository.ConcurrencyMode = ConcurrencyMode.Pessimistic;
        });
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
        rabbit.UsePublishMessageScheduler();
        rabbit.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromMilliseconds(200), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(500)));
        rabbit.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:Migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<OrdersDbContext>().Database.MigrateAsync();
    if (builder.Configuration.GetValue<bool>("Database:MigrationsOnly")) return;
}
app.UseExceptionHandler();
app.UseRestaurantFlowSecurity(builder.Configuration);
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");

app.MapPost("/api/orders", async (PlaceOrderRequest request, ClaimsPrincipal user, IConfiguration configuration, MenuCatalogClient menuCatalog, OrdersDbContext dbContext, IPublishEndpoint publisher, CancellationToken cancellationToken) =>
{
    try
    {
        var customerId = request.CustomerId;
        var customerEmail = request.CustomerEmail;
        if (configuration.GetValue("Authentication:Enabled", false))
        {
            if (!Guid.TryParse(user.FindFirstValue("sub"), out customerId))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["identity"] = ["The authenticated subject must be a GUID."] });
            customerEmail = user.FindFirstValue("email")
                ?? throw new ArgumentException("The authenticated identity must contain an email claim.");
        }

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
        var order = Order.Place(customerId, customerEmail, items);
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
}).RequireAuthorization(Policies.Customer);

app.MapGet("/api/orders/{id:guid}", async (Guid id, ClaimsPrincipal user, IConfiguration configuration, OrdersDbContext dbContext, CancellationToken cancellationToken) =>
{
    var order = await dbContext.Orders.AsNoTracking().Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (order is null) return Results.NotFound();
    if (configuration.GetValue("Authentication:Enabled", false)
        && !user.IsInRole("admin")
        && (!Guid.TryParse(user.FindFirstValue("sub"), out var customerId) || order.CustomerId != customerId))
        return Results.Forbid();
    return Results.Ok(order);
}).RequireAuthorization(Policies.Customer);

app.MapGet("/api/orders/{id:guid}/workflow", async (Guid id, ClaimsPrincipal user, IConfiguration configuration, OrdersDbContext dbContext, CancellationToken cancellationToken) =>
{
    var workflow = await dbContext.Set<OrderWorkflowState>().AsNoTracking().SingleOrDefaultAsync(
        state => state.CorrelationId == id,
        cancellationToken);
    if (workflow is null) return Results.NotFound();
    if (configuration.GetValue("Authentication:Enabled", false)
        && !user.IsInRole("admin")
        && (!Guid.TryParse(user.FindFirstValue("sub"), out var customerId) || workflow.CustomerId != customerId))
        return Results.Forbid();
    return Results.Ok(workflow);
}).RequireAuthorization(Policies.Customer);

app.Run();

public sealed record PlaceOrderRequest(Guid CustomerId, string CustomerEmail, string PaymentReference, IReadOnlyCollection<PlaceOrderItemRequest> Items);
public sealed record PlaceOrderItemRequest(Guid MenuItemId, int Quantity);

public partial class Program;
