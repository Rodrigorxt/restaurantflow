using MassTransit;
using RestaurantFlow.Contracts;
using Microsoft.Extensions.Options;

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
    public Schedule<OrderWorkflowState, OrderWorkflowTimedOut> WorkflowTimeout { get; private set; } = null!;

    public OrderWorkflowStateMachine(IOptions<OrderWorkflowOptions> options)
    {
        var workflowOptions = options.Value;
        InstanceState(state => state.CurrentState);

        Schedule(() => WorkflowTimeout, state => state.WorkflowTimeoutTokenId, schedule =>
        {
            schedule.Received = received => received.CorrelateById(context => context.Message.OrderId);
        });

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
                .Schedule(WorkflowTimeout,
                    context => new OrderWorkflowTimedOut(
                        Guid.NewGuid(),
                        context.Saga.CorrelationId,
                        "payment",
                        DateTimeOffset.UtcNow),
                    _ => workflowOptions.PaymentTimeout)
                .TransitionTo(AwaitingPayment));

        During(AwaitingPayment,
            When(PaymentAuthorized)
                .Unschedule(WorkflowTimeout)
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
                .Schedule(WorkflowTimeout,
                    context => new OrderWorkflowTimedOut(
                        Guid.NewGuid(),
                        context.Saga.CorrelationId,
                        "kitchen-acceptance",
                        DateTimeOffset.UtcNow),
                    _ => workflowOptions.KitchenAcceptanceTimeout)
                .TransitionTo(AwaitingKitchen),
            When(PaymentDeclined)
                .Unschedule(WorkflowTimeout)
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
                .Finalize(),
            When(WorkflowTimeout.Received)
                .Then(context =>
                {
                    context.Saga.FailureReason = "Payment authorization timed out.";
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .Publish(context => new CancelOrder(
                    Guid.NewGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CorrelationId,
                    context.Saga.FailureReason!,
                    DateTimeOffset.UtcNow))
                .Finalize());

        During(AwaitingKitchen,
            Ignore(PaymentAuthorized),
            Ignore(PaymentDeclined),
            When(KitchenTicketCreated)
                .Unschedule(WorkflowTimeout)
                .Then(context =>
                {
                    context.Saga.KitchenTicketId = context.Message.TicketId;
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .TransitionTo(InPreparation),
            When(WorkflowTimeout.Received)
                .Then(context =>
                {
                    context.Saga.FailureReason = "Kitchen acceptance timed out.";
                    context.Saga.UpdatedAt = context.Message.OccurredAt;
                })
                .Publish(context => new CancelOrder(
                    Guid.NewGuid(),
                    context.Saga.CorrelationId,
                    context.Saga.CorrelationId,
                    context.Saga.FailureReason!,
                    DateTimeOffset.UtcNow))
                .Finalize());

        During(InPreparation,
            Ignore(PaymentAuthorized),
            Ignore(PaymentDeclined),
            Ignore(KitchenTicketCreated),
            Ignore(WorkflowTimeout.Received),
            When(KitchenPreparationStarted)
                .Then(context => context.Saga.UpdatedAt = context.Message.OccurredAt),
            When(OrderReady)
                .Then(context => context.Saga.UpdatedAt = context.Message.OccurredAt)
                .Finalize());

        During(Final,
            Ignore(PaymentAuthorized),
            Ignore(PaymentDeclined),
            Ignore(KitchenTicketCreated),
            Ignore(KitchenPreparationStarted),
            Ignore(OrderReady),
            Ignore(WorkflowTimeout.Received));
    }
}
