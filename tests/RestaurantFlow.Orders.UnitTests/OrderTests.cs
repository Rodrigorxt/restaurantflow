using RestaurantFlow.Orders.Api.Domain;

namespace RestaurantFlow.Orders.UnitTests;

public sealed class OrderTests
{
    [Fact]
    public void Place_calculates_total_and_starts_pending_payment()
    {
        var order = Order.Place(Guid.NewGuid(), "Customer@Example.com", [
            OrderItem.Create(Guid.NewGuid(), "Burger", 2, 25.50m),
            OrderItem.Create(Guid.NewGuid(), "Soda", 1, 8m)
        ]);

        Assert.Equal(59m, order.Total);
        Assert.Equal(OrderStatus.PendingPayment, order.Status);
        Assert.Equal("customer@example.com", order.CustomerEmail);
    }

    [Fact]
    public void Place_rejects_an_empty_order()
    {
        var action = () => Order.Place(Guid.NewGuid(), "customer@example.com", []);
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Paid_order_can_progress_to_ready()
    {
        var order = CreateOrder();
        order.MarkAsPaid();
        order.AcceptByKitchen();
        order.MarkAsPreparing();
        order.MarkAsReady();
        Assert.Equal(OrderStatus.Ready, order.Status);
    }

    [Fact]
    public void Pending_order_cannot_start_kitchen_preparation()
    {
        var order = CreateOrder();
        Assert.Throws<InvalidOperationException>(order.MarkAsPreparing);
    }

    [Fact]
    public void Declined_payment_cancels_order_with_reason()
    {
        var order = CreateOrder();
        order.CancelPayment("Card declined");
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal("Card declined", order.CancellationReason);
    }

    private static Order CreateOrder() => Order.Place(
        Guid.NewGuid(),
        "customer@example.com",
        [OrderItem.Create(Guid.NewGuid(), "Burger", 1, 25m)]);
}

