namespace RestaurantFlow.Kitchen.Api;

public sealed class KitchenTicket
{
    private KitchenTicket() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public string Status { get; private set; } = "queued";
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static KitchenTicket Create(Guid orderId)
    {
        var now = DateTimeOffset.UtcNow;
        return new KitchenTicket { Id = Guid.NewGuid(), OrderId = orderId, CreatedAt = now, UpdatedAt = now };
    }

    public void Start()
    {
        if (Status != "queued") throw new InvalidOperationException($"Ticket {Id} is not queued.");
        Status = "preparing";
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Complete()
    {
        if (Status != "preparing") throw new InvalidOperationException($"Ticket {Id} is not being prepared.");
        Status = "ready";
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

