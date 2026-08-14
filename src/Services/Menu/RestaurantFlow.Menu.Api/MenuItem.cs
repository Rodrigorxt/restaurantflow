namespace RestaurantFlow.Menu.Api;

public sealed class MenuItem
{
    private MenuItem() { }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public bool IsAvailable { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public static MenuItem Create(string name, string description, string category, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required.", nameof(name));
        if (string.IsNullOrWhiteSpace(category)) throw new ArgumentException("Category is required.", nameof(category));
        if (price <= 0) throw new ArgumentOutOfRangeException(nameof(price), "Price must be positive.");
        return new MenuItem
        {
            Id = Guid.NewGuid(), Name = name.Trim(), Description = description.Trim(), Category = category.Trim(),
            Price = price, IsAvailable = true, UpdatedAt = DateTimeOffset.UtcNow
        };
    }

    public void SetAvailability(bool isAvailable)
    {
        IsAvailable = isAvailable;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

