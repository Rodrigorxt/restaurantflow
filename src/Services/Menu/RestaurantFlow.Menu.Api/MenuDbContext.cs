using Microsoft.EntityFrameworkCore;

namespace RestaurantFlow.Menu.Api;

public sealed class MenuDbContext(DbContextOptions<MenuDbContext> options) : DbContext(options)
{
    public DbSet<MenuItem> MenuItems => Set<MenuItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("menu_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(200);
            entity.Property(item => item.Description).HasMaxLength(1000);
            entity.Property(item => item.Category).HasMaxLength(100);
            entity.Property(item => item.Price).HasPrecision(12, 2);
            entity.HasIndex(item => new { item.Category, item.IsAvailable });
        });
    }
}

