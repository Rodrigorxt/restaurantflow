namespace RestaurantFlow.Orders.Api.Domain;

public sealed class Order
{
    private Order() { }

    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public string CustomerEmail { get; private set; } = string.Empty;
    public string Status { get; private set; } = OrderStatus.PendingPayment;
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public List<OrderItem> Items { get; private set; } = [];

    public static Order Place(Guid customerId, string customerEmail, IEnumerable<OrderItem> items)
    {
        var itemList = items.ToList();
        if (customerId == Guid.Empty) throw new ArgumentException("Customer is required.", nameof(customerId));
        if (string.IsNullOrWhiteSpace(customerEmail)) throw new ArgumentException("Customer email is required.", nameof(customerEmail));
        if (itemList.Count == 0) throw new ArgumentException("An order must contain at least one item.", nameof(items));

        var now = DateTimeOffset.UtcNow;
        return new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            CustomerEmail = customerEmail.Trim().ToLowerInvariant(),
            Items = itemList,
            Total = itemList.Sum(item => item.UnitPrice * item.Quantity),
            CreatedAt = now,
            UpdatedAt = now
        };
    }

    public void MarkAsPaid()
    {
        EnsureStatus(OrderStatus.PendingPayment);
        Status = OrderStatus.Paid;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void CancelPayment(string reason)
    {
        EnsureStatus(OrderStatus.PendingPayment);
        Status = OrderStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
        CancellationReason = reason;
    }

    public string? CancellationReason { get; private set; }

    public void MarkAsPreparing()
    {
        if (Status is not (OrderStatus.Paid or OrderStatus.AcceptedByKitchen))
            throw new InvalidOperationException($"Order {Id} cannot start preparation from status {Status}.");
        Status = OrderStatus.Preparing;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void AcceptByKitchen()
    {
        EnsureStatus(OrderStatus.Paid);
        Status = OrderStatus.AcceptedByKitchen;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void MarkAsReady()
    {
        EnsureStatus(OrderStatus.Preparing);
        Status = OrderStatus.Ready;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureStatus(string expected)
    {
        if (Status != expected)
            throw new InvalidOperationException($"Order {Id} expected status {expected}, but was {Status}.");
    }
}

public sealed class OrderItem
{
    private OrderItem() { }

    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid MenuItemId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    public static OrderItem Create(Guid menuItemId, string name, int quantity, decimal unitPrice)
    {
        if (menuItemId == Guid.Empty) throw new ArgumentException("Menu item is required.", nameof(menuItemId));
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Item name is required.", nameof(name));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");
        if (unitPrice <= 0) throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price must be positive.");

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            MenuItemId = menuItemId,
            Name = name.Trim(),
            Quantity = quantity,
            UnitPrice = unitPrice
        };
    }
}

public static class OrderStatus
{
    public const string PendingPayment = "pending-payment";
    public const string Paid = "paid";
    public const string AcceptedByKitchen = "accepted-by-kitchen";
    public const string Preparing = "preparing";
    public const string Ready = "ready";
    public const string Cancelled = "cancelled";
}

