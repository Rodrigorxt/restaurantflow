using RestaurantFlow.Contracts;

namespace RestaurantFlow.ArchitectureTests;

public sealed class IntegrationContractTests
{
    [Fact]
    public void Integration_events_include_identity_correlation_and_timestamp()
    {
        var eventTypes = typeof(OrderSubmitted).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == typeof(OrderSubmitted).Namespace)
            .Where(type => type.Name != nameof(OrderItemSnapshot));

        foreach (var eventType in eventTypes)
        {
            Assert.NotNull(eventType.GetProperty("EventId"));
            Assert.NotNull(eventType.GetProperty("OrderId"));
            Assert.NotNull(eventType.GetProperty("OccurredAt"));
        }
    }

    [Fact]
    public void Contracts_package_does_not_reference_service_implementations()
    {
        var references = typeof(OrderSubmitted).Assembly.GetReferencedAssemblies();
        Assert.DoesNotContain(references, reference => reference.Name?.StartsWith("RestaurantFlow.") == true);
    }
}

