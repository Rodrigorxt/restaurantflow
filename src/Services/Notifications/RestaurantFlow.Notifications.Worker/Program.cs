using MassTransit;
using RestaurantFlow.Notifications.Worker;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddMassTransit(configurator =>
{
    configurator.AddConsumers(typeof(Program).Assembly);
    configurator.UsingRabbitMq((context, rabbit) =>
    {
        rabbit.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", host =>
        {
            host.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            host.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });
        rabbit.UseMessageRetry(retry => retry.Exponential(5, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2)));
        rabbit.ConfigureEndpoints(context);
    });
});

await builder.Build().RunAsync();

public partial class Program;

