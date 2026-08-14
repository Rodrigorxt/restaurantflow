using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Infrastructure;

namespace RestaurantFlow.Orders.Api.Consumers;

public sealed class KitchenTicketCreatedConsumer(OrdersDbContext dbContext) : IConsumer<KitchenTicketCreated>
{
    public async Task Consume(ConsumeContext<KitchenTicketCreated> context)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);
        if (order is null || order.Status != Domain.OrderStatus.Paid) return;
        order.AcceptByKitchen();
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}

public sealed class KitchenPreparationStartedConsumer(OrdersDbContext dbContext) : IConsumer<KitchenPreparationStarted>
{
    public async Task Consume(ConsumeContext<KitchenPreparationStarted> context)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);
        if (order is null || order.Status == Domain.OrderStatus.Preparing) return;
        order.MarkAsPreparing();
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}

public sealed class OrderReadyConsumer(OrdersDbContext dbContext) : IConsumer<OrderReady>
{
    public async Task Consume(ConsumeContext<OrderReady> context)
    {
        var order = await dbContext.Orders.SingleOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);
        if (order is null || order.Status == Domain.OrderStatus.Ready) return;
        order.MarkAsReady();
        await dbContext.SaveChangesAsync(context.CancellationToken);
    }
}

