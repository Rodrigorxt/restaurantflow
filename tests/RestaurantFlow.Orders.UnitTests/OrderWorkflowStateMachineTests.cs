using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RestaurantFlow.Contracts;
using RestaurantFlow.Orders.Api.Workflow;

namespace RestaurantFlow.Orders.UnitTests;

public sealed class OrderWorkflowStateMachineTests
{
    [Fact]
    public async Task Submitted_order_requests_payment_and_authorized_payment_requests_kitchen_ticket()
    {
        await using var provider = CreateProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(CreateOrderSubmitted(orderId));
        Assert.True(await harness.Published.Any<AuthorizePayment>());

        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(), orderId, Guid.NewGuid(), 42m, DateTimeOffset.UtcNow));
        Assert.True(await harness.Published.Any<CreateKitchenTicket>());
    }

    [Fact]
    public async Task Declined_payment_requests_order_compensation()
    {
        await using var provider = CreateProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(CreateOrderSubmitted(orderId));
        Assert.True(await harness.Published.Any<AuthorizePayment>());

        await harness.Bus.Publish(new PaymentDeclined(
            Guid.NewGuid(), orderId, "Card declined", DateTimeOffset.UtcNow));

        Assert.True(await harness.Published.Any<CancelOrder>(message =>
            message.Context.Message.OrderId == orderId
            && message.Context.Message.Reason == "Card declined"));
    }

    [Fact]
    public async Task Payment_timeout_requests_order_compensation()
    {
        await using var provider = CreateProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(CreateOrderSubmitted(orderId));

        Assert.True(await harness.Published.Any<CancelOrder>(message =>
            message.Context.Message.OrderId == orderId
            && message.Context.Message.Reason == "Payment authorization timed out."));
    }

    [Fact]
    public async Task Late_timeout_after_kitchen_acceptance_is_ignored()
    {
        await using var provider = CreateProvider();
        var harness = provider.GetRequiredService<ITestHarness>();
        await harness.Start();
        var orderId = Guid.NewGuid();

        await harness.Bus.Publish(CreateOrderSubmitted(orderId));
        await harness.Bus.Publish(new PaymentAuthorized(
            Guid.NewGuid(), orderId, Guid.NewGuid(), 42m, DateTimeOffset.UtcNow));
        await harness.Bus.Publish(new KitchenTicketCreated(
            Guid.NewGuid(), orderId, Guid.NewGuid(), DateTimeOffset.UtcNow));
        await harness.Bus.Publish(new OrderWorkflowTimedOut(
            Guid.NewGuid(), orderId, "kitchen-acceptance", DateTimeOffset.UtcNow));

        Assert.False(await harness.Published.Any<CancelOrder>(message =>
            message.Context.Message.OrderId == orderId));
    }

    private static ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Options.Create(new OrderWorkflowOptions
        {
            PaymentTimeout = TimeSpan.FromMilliseconds(250),
            KitchenAcceptanceTimeout = TimeSpan.FromMilliseconds(250)
        }));
        services.AddMassTransitTestHarness(configurator =>
        {
            configurator.AddDelayedMessageScheduler();
            configurator.AddSagaStateMachine<OrderWorkflowStateMachine, OrderWorkflowState>();
            configurator.UsingInMemory((context, bus) =>
            {
                bus.UseDelayedMessageScheduler();
                bus.ConfigureEndpoints(context);
            });
        });
        return services.BuildServiceProvider(true);
    }

    private static OrderSubmitted CreateOrderSubmitted(Guid orderId) => new(
        Guid.NewGuid(),
        orderId,
        Guid.NewGuid(),
        "customer@example.com",
        "approved",
        42m,
        [new OrderItemSnapshot(Guid.NewGuid(), "Burger", 1, 42m)],
        DateTimeOffset.UtcNow);
}
