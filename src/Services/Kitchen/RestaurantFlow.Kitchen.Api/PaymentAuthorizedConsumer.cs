using MassTransit;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Contracts;

namespace RestaurantFlow.Kitchen.Api;

public sealed class PaymentAuthorizedConsumer(KitchenDbContext dbContext) : IConsumer<CreateKitchenTicket>
{
    public async Task Consume(ConsumeContext<CreateKitchenTicket> context)
    {
        if (await dbContext.Tickets.AnyAsync(ticket => ticket.OrderId == context.Message.OrderId, context.CancellationToken)) return;

        var ticket = KitchenTicket.Create(context.Message.OrderId);
        dbContext.Tickets.Add(ticket);
        await dbContext.SaveChangesAsync(context.CancellationToken);
        await context.Publish(new KitchenTicketCreated(Guid.NewGuid(), ticket.OrderId, ticket.Id, DateTimeOffset.UtcNow));
    }
}
