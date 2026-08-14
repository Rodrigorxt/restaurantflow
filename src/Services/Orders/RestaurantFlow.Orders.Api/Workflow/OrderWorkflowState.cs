using MassTransit;

namespace RestaurantFlow.Orders.Api.Workflow;

public sealed class OrderWorkflowState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? KitchenTicketId { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
