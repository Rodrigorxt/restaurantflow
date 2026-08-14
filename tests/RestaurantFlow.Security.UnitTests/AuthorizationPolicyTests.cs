using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantFlow.Security;

namespace RestaurantFlow.Security.UnitTests;

public sealed class AuthorizationPolicyTests
{
    private readonly IAuthorizationService authorization = CreateAuthorizationService();

    [Fact]
    public async Task Administrator_can_manage_menu_items()
    {
        var result = await authorization.AuthorizeAsync(CreateIdentity("admin"), null, Policies.Admin);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Customer_cannot_access_kitchen_operations()
    {
        var result = await authorization.AuthorizeAsync(CreateIdentity("customer"), null, Policies.Kitchen);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task Anonymous_identity_cannot_call_internal_endpoints()
    {
        var result = await authorization.AuthorizeAsync(new ClaimsPrincipal(), null, Policies.Internal);
        Assert.False(result.Succeeded);
    }

    private static ClaimsPrincipal CreateIdentity(string role) => new(new ClaimsIdentity(
        [new Claim("sub", Guid.NewGuid().ToString()), new Claim("roles", role)],
        "test",
        "preferred_username",
        "roles"));

    private static IAuthorizationService CreateAuthorizationService()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:Enabled"] = "true",
            ["Authentication:Authority"] = "https://identity.test/realms/restaurantflow",
            ["Authentication:Audience"] = "restaurantflow-api"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddRestaurantFlowSecurity(configuration);
        return services.BuildServiceProvider().GetRequiredService<IAuthorizationService>();
    }
}
