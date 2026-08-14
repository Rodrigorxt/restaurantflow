using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Payments.Api;
using RestaurantFlow.Observability;
using RestaurantFlow.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRestaurantFlowObservability("restaurantflow-payments");
var connectionString = builder.Configuration.GetConnectionString("Database")
    ?? "Host=localhost;Port=5435;Database=payments;Username=postgres;Password=postgres";

builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddRestaurantFlowSecurity(builder.Configuration);
builder.Services.AddDbContext<PaymentsDbContext>(options => options.UseNpgsql(connectionString));
builder.Services.AddMassTransit(configurator =>
{
    configurator.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter("payments", false));
    configurator.AddConsumer<OrderSubmittedConsumer>();
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
if (builder.Configuration.GetValue<bool>("Database:Migrate"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<PaymentsDbContext>().Database.MigrateAsync();
    if (builder.Configuration.GetValue<bool>("Database:MigrationsOnly")) return;
}
if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseRestaurantFlowSecurity(builder.Configuration);
app.MapHealthChecks("/health");
app.MapGet("/api/payments/{orderId:guid}", async (Guid orderId, PaymentsDbContext dbContext, CancellationToken cancellationToken) =>
{
    var payment = await dbContext.Payments.AsNoTracking().SingleOrDefaultAsync(x => x.OrderId == orderId, cancellationToken);
    return payment is null ? Results.NotFound() : Results.Ok(payment);
}).RequireAuthorization(Policies.Admin);
app.Run();

public partial class Program;
