namespace RestaurantFlow.Payments.Api;

public sealed class Payment
{
    private Payment() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public string? DeclineReason { get; private set; }
    public DateTimeOffset ProcessedAt { get; private set; }

    public static Payment Authorized(Guid orderId, decimal amount) => new()
    {
        Id = Guid.NewGuid(), OrderId = orderId, Amount = amount, Status = "authorized", ProcessedAt = DateTimeOffset.UtcNow
    };

    public static Payment Declined(Guid orderId, decimal amount, string reason) => new()
    {
        Id = Guid.NewGuid(), OrderId = orderId, Amount = amount, Status = "declined", DeclineReason = reason, ProcessedAt = DateTimeOffset.UtcNow
    };
}

