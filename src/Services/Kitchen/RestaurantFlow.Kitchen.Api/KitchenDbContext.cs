using Microsoft.EntityFrameworkCore;

namespace RestaurantFlow.Kitchen.Api;

public sealed class KitchenDbContext(DbContextOptions<KitchenDbContext> options) : DbContext(options)
{
    public DbSet<KitchenTicket> Tickets => Set<KitchenTicket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KitchenTicket>(entity =>
        {
            entity.ToTable("kitchen_tickets");
            entity.HasKey(ticket => ticket.Id);
            entity.HasIndex(ticket => ticket.OrderId).IsUnique();
            entity.Property(ticket => ticket.Status).HasMaxLength(30);
        });
    }
}

