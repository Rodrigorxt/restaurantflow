namespace RestaurantFlow.Contracts;

public sealed record OrderItemSnapshot(Guid MenuItemId, string Name, int Quantity, decimal UnitPrice);

public sealed record AuthorizePayment(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    string PaymentReference,
    decimal Total,
    DateTimeOffset OccurredAt);

public sealed record CreateKitchenTicket(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    DateTimeOffset OccurredAt);

public sealed record CancelOrder(
    Guid EventId,
    Guid CorrelationId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAt);

public sealed record OrderSubmitted(
    Guid EventId,
    Guid OrderId,
    Guid CustomerId,
    string CustomerEmail,
    string PaymentReference,
    decimal Total,
    IReadOnlyCollection<OrderItemSnapshot> Items,
    DateTimeOffset OccurredAt);

public sealed record PaymentAuthorized(
    Guid EventId,
    Guid OrderId,
    Guid PaymentId,
    decimal Amount,
    DateTimeOffset OccurredAt);

public sealed record PaymentDeclined(
    Guid EventId,
    Guid OrderId,
    string Reason,
    DateTimeOffset OccurredAt);

public sealed record KitchenTicketCreated(
    Guid EventId,
    Guid OrderId,
    Guid TicketId,
    DateTimeOffset OccurredAt);

public sealed record KitchenPreparationStarted(
    Guid EventId,
    Guid OrderId,
    Guid TicketId,
    DateTimeOffset OccurredAt);

public sealed record OrderReady(
    Guid EventId,
    Guid OrderId,
    Guid TicketId,
    DateTimeOffset OccurredAt);
