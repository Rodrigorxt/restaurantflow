using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Infrastructure;

namespace RestaurantFlow.Orders.Api.Consumers;

public sealed class PaymentAuthorizedConsumer(OrdersDbContext dbContext) : IConsumer<PaymentAuthorized>
{
    public async Task Consume(ConsumeContext<PaymentAuthorized> context)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(
            order => order.Id == context.Message.OrderId,
            context.CancellationToken);

        if (order is null || order.Status != Domain.OrderStatus.PendingPayment) return;

        order.MarkAsPaid();
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}

