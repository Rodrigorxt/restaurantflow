using MassTransit;
using AppAny.Quartz.EntityFrameworkCore.Migrations;
using AppAny.Quartz.EntityFrameworkCore.Migrations.PostgreSQL;
using Microsoft.EntityFrameworkCore;
using RestaurantFlow.Orders.Api.Domain;
using RestaurantFlow.Orders.Api.Workflow;

namespace RestaurantFlow.Orders.Api.Infrastructure;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.CustomerEmail).HasMaxLength(320);
            entity.Property(order => order.Status).HasMaxLength(40);
            entity.Property(order => order.Total).HasPrecision(12, 2);
            entity.Property(order => order.CancellationReason).HasMaxLength(500);
            entity.HasMany(order => order.Items).WithOne().HasForeignKey(item => item.OrderId);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Name).HasMaxLength(200);
            entity.Property(item => item.UnitPrice).HasPrecision(12, 2);
        });

        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
        modelBuilder.AddQuartz(quartz => quartz.UsePostgreSql());
        modelBuilder.Entity<OrderWorkflowState>(entity =>
        {
            entity.ToTable("order_workflow_states");
            entity.HasKey(state => state.CorrelationId);
            entity.Property(state => state.CurrentState).HasMaxLength(64);
            entity.Property(state => state.Total).HasPrecision(12, 2);
            entity.Property(state => state.FailureReason).HasMaxLength(500);
        });
    }
}
