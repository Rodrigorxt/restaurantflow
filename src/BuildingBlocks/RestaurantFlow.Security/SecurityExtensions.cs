using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace RestaurantFlow.Security;

public static class SecurityExtensions
{
    public static IServiceCollection AddRestaurantFlowSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("Authentication:Enabled", false);

        if (enabled)
        {
            var authority = configuration["Authentication:Authority"]
                ?? throw new InvalidOperationException("Authentication:Authority is required when authentication is enabled.");
            var audience = configuration["Authentication:Audience"] ?? "restaurantflow-api";

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.MapInboundClaims = false;
                    options.Authority = authority;
                    options.Audience = audience;
                    options.RequireHttpsMetadata = configuration.GetValue("Authentication:RequireHttpsMetadata", true);
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = true,
                        ValidAudience = audience,
                        NameClaimType = "preferred_username",
                        RoleClaimType = "roles"
                    };
                });
        }

        services.AddAuthorization(options =>
        {
            AddPolicy(options, Policies.Customer, enabled, "customer", "admin");
            AddPolicy(options, Policies.Kitchen, enabled, "kitchen", "admin");
            AddPolicy(options, Policies.Admin, enabled, "admin");
            AddPolicy(options, Policies.Internal, enabled, "internal", "admin");
        });

        return services;
    }

    public static WebApplication UseRestaurantFlowSecurity(this WebApplication app, IConfiguration configuration)
    {
        if (configuration.GetValue("Authentication:Enabled", false)) app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    private static void AddPolicy(
        AuthorizationOptions options,
        string name,
        bool authenticationEnabled,
        params string[] roles)
    {
        options.AddPolicy(name, policy =>
        {
            if (authenticationEnabled)
            {
                policy.RequireAuthenticatedUser();
                policy.RequireRole(roles);
            }
            else
            {
                policy.RequireAssertion(_ => true);
            }
        });
    }
}

public static class Policies
{
    public const string Customer = "customer";
    public const string Kitchen = "kitchen";
    public const string Admin = "admin";
    public const string Internal = "internal";
}
