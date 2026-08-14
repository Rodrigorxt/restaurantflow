using MassTransit;
using RestaurantFlow.Contracts;

namespace RestaurantFlow.Orders.Api.Workflow;

public sealed class OrderWorkflowStateMachine : MassTransitStateMachine<OrderWorkflowState>
{
    public State AwaitingPayment { get; private set; } = null!;
    public State AwaitingKitchen { get; private set; } = null!;
    public State InPreparation { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<PaymentAuthorized> PaymentAuthorized { get; private set; } = null!;
    public Event<PaymentDeclined> PaymentDeclined { get; private set; } = null!;
    public Event<KitchenTicketCreated> KitchenTicketCreated { get; private set; } = null!;
    public Event<KitchenPreparationStarted> KitchenPreparationStarted { get; private set; } = null!;
    public Event<OrderReady> OrderReady { get; private set; } = null!;

    public OrderWorkflowStateMachine()
    {
        InstanceState(state => state.CurrentState);

        Event(() => OrderSubmitted, config => config.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentAuthorized, config => config.CorrelateById(context => context.Message.OrderId));
        Event(() => PaymentDeclined, config => config.CorrelateById(context => context.Message.OrderId));
        Event(() => KitchenTicketCreated, config => config.CorrelateById(context => context.Message.OrderId));
        Event(() => KitchenPreparationStarted, config => config.CorrelateById(context => context.Message.OrderId));
        Event(() => OrderReady, config => config.CorrelateById(context => context.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .Then(context =>
                {
                    context.Saga.CustomerId = context.Message.CustomerId;
                    context.Saga.Total = context.Message.Total;
                    context.Saga.CreatedAt = context.Message.OccurredAt;
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .Publish(context => new AuthorizePayment(
                    Guid.NewGuid(),
                    context.Saga.CorrelationId,
                    context.Message.OrderId,
                    context.Message.PaymentReference,
                    context.Message.Total,
                    DateTimeOffset.UtcNow))
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(PaymentAuthorized)
                .Then(context =>
                {
                    context.Saga.PaymentId = context.Message.PaymentId;
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .Publish(context => new CreateKitchenTicket(
                    Guid.NewGuid(),
                    context.Saga.CorrelationId,
                    context.Message.OrderId,
                    DateTimeOffset.UtcNow))
                .TransitionTo(AwaitingKitchen),
            When(PaymentDeclined)
                .Then(context =>
                {
                    context.Saga.FailureReason = context.Message.Reason;
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .Publish(context => new CancelOrder(
                    Guid.NewGuid(),
                    context.Saga.CorrelationId,
                    context.Message.OrderId,
                    context.Message.Reason,
                    DateTimeOffset.UtcNow))
                .Finalize());

        During(AwaitingKitchen,
            When(KitchenTicketCreated)
                .Then(context =>
                {
                    context.Saga.KitchenTicketId = context.Message.TicketId;
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .TransitionTo(InPreparation));

        During(InPreparation,
            When(KitchenPreparationStarted)
                .Then(context => context.Saga.UpdatedAt = context.Message.OccurredAt),
            When(OrderReady)
                .Then(context => context.Saga.UpdatedAt = context.Message.OccurredAt)
                .Finalize());
    }
}
