using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Infrastructure;

namespace RestaurantFlow.Orders.Api.Consumers;

public sealed class PaymentDeclinedConsumer(OrdersDbContext dbContext) : IConsumer<CancelOrder>
{
    public async Task Consume(ConsumeContext<CancelOrder> context)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(
            order => order.Id == context.Message.OrderId,
            context.CancellationToken);

        if (order is null || order.Status != Domain.OrderStatus.PendingPayment) return;

        order.CancelPayment(context.Message.Reason);
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}
