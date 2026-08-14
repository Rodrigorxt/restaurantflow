using RestaurantFlow.Observability;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRestaurantFlowObservability("restaurantflow-gateway");
builder.Services.AddHealthChecks();
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.MapHealthChecks("/health");
app.MapReverseProxy();
app.Run();

public partial class Program;
