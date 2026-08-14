using MassTransit;
using RestaurantFlow.Contracts;

namespace RestaurantFlow.Notifications.Worker;

public sealed class PaymentDeclinedNotificationConsumer(ILogger<PaymentDeclinedNotificationConsumer> logger) : IConsumer<PaymentDeclined>
{
    public Task Consume(ConsumeContext<PaymentDeclined> context)
    {
        logger.LogWarning("Notification queued: payment for order {OrderId} was declined. Reason: {Reason}", context.Message.OrderId, context.Message.Reason);
        return Task.CompletedTask;
    }
}

public sealed class KitchenPreparationNotificationConsumer(ILogger<KitchenPreparationNotificationConsumer> logger) : IConsumer<KitchenPreparationStarted>
{
    public Task Consume(ConsumeContext<KitchenPreparationStarted> context)
    {
        logger.LogInformation("Notification queued: order {OrderId} is being prepared", context.Message.OrderId);
        return Task.CompletedTask;
    }
}

public sealed class OrderReadyNotificationConsumer(ILogger<OrderReadyNotificationConsumer> logger) : IConsumer<OrderReady>
{
    public Task Consume(ConsumeContext<OrderReady> context)
    {
        logger.LogInformation("Notification queued: order {OrderId} is ready", context.Message.OrderId);
        return Task.CompletedTask;
    }
}

