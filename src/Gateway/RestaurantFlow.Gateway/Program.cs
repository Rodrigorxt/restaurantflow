using RestaurantFlow.Observability;
using RestaurantFlow.Security;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRestaurantFlowObservability("restaurantflow-gateway");
builder.Services.AddHealthChecks();
builder.Services.AddRestaurantFlowSecurity(builder.Configuration);
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();
app.MapHealthChecks("/health");
app.UseRestaurantFlowSecurity(builder.Configuration);
app.MapReverseProxy();
app.Run();

public partial class Program;
