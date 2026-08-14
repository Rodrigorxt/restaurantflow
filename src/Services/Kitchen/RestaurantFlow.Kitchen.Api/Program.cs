using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;
using RestaurantFlow.Kitchen.Api;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5434;Database=kitchen;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddDbContext<KitchenDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddMassTransit(configurator =>
{
    configurator.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("kitchen", false));
    configurator.AddConsumer<PaymentAuthorizedConsumer>();
    configurator.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        rabbit.UseMessageRetry(retry => retry.Intervals(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15)));
        rabbit.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<KitchenDbContext>().Database.MigrateAsync();
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.MapHealthChecks("/health");
app.MapGet("/api/kitchen/tickets", async (KitchenDbContext dbContext, CancellationToken cancellationToken) =>
    Results.Ok(await dbContext.Tickets.AsNoTracking().OrderBy(ticket => ticket.CreatedAt).ToListAsync(cancellationToken)));
app.MapPost("/api/kitchen/tickets/{id:guid}/start", async (Guid id, KitchenDbContext dbContext, IPublishEndpoint publisher, CancellationToken cancellationToken) =>
{
    var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (ticket is null) return Results.NotFound();
    ticket.Start();
    await dbContext.SaveChangesAsync(cancellationToken);
    await publisher.Publish(new KitchenPreparationStarted(Guid.NewGuid(), ticket.OrderId, ticket.Id, DateTimeOffset.UtcNow), cancellationToken);
    return Results.Ok(ticket);
});
app.MapPost("/api/kitchen/tickets/{id:guid}/complete", async (Guid id, KitchenDbContext dbContext, IPublishEndpoint publisher, CancellationToken cancellationToken) =>
{
    var ticket = await dbContext.Tickets.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
    if (ticket is null) return Results.NotFound();
    ticket.Complete();
    await dbContext.SaveChangesAsync(cancellationToken);
    await publisher.Publish(new OrderReady(Guid.NewGuid(), ticket.OrderId, ticket.Id, DateTimeOffset.UtcNow), cancellationToken);
    return Results.Ok(ticket);
});
app.Run();

public partial class Program;
