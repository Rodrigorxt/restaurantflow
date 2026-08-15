namespace RestaurantFlow.Orders.Api.Workflow;

public sealed class OrderWorkflowOptions
{
    public const string SectionName = "OrderWorkflow";

    public TimeSpan PaymentTimeout { get; set; } = TimeSpan.FromMinutes(2);
    public TimeSpan KitchenAcceptanceTimeout { get; set; } = TimeSpan.FromMinutes(2);
}
