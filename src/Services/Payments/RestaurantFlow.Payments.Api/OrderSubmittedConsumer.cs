using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;

namespace RestaurantFlow.Payments.Api;

public sealed class OrderSubmittedConsumer(PaymentsDbContext dbContext, ILogger<OrderSubmittedConsumer> logger) : IConsumer<AuthorizePayment>
{
    public async Task Consume(ConsumeContext<AuthorizePayment> context)
    {
        if (await dbContext.Payments.AnyAsync(payment => payment.OrderId == context.Message.OrderId, context.CancellationToken))
        {
            logger.LogInformation("Payment for order {OrderId} was already processed", context.Message.OrderId);
            return;
        }

        var shouldDecline = context.Message.PaymentReference.StartsWith("decline", StringComparison.OrdinalIgnoreCase);
        var payment = shouldDecline
            ? Payment.Declined(context.Message.OrderId, context.Message.Total, "Payment was declined by the simulated provider.")
            : Payment.Authorized(context.Message.OrderId, context.Message.Total);

        dbContext.Payments.Add(payment);
        await dbContext.SaveChangesAsync(context.CancellationToken);

        if (shouldDecline)
        {
            await context.Publish(new PaymentDeclined(Guid.NewGuid(), payment.OrderId, payment.DeclineReason!, DateTimeOffset.UtcNow));
            return;
        }

        await context.Publish(new PaymentAuthorized(Guid.NewGuid(), payment.OrderId, payment.Id, payment.Amount, DateTimeOffset.UtcNow));
    }
}
