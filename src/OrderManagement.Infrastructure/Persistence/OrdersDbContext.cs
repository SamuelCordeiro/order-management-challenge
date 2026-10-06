using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var order = modelBuilder.Entity<Order>();
        order.ToTable("orders");
        order.HasKey(x => x.Id);
        order.Property(x => x.Id).HasColumnName("id");
        order.Property(x => x.Customer).HasColumnName("customer").HasMaxLength(200).IsRequired();
        order.Property(x => x.Product).HasColumnName("product").HasMaxLength(200).IsRequired();
        order.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2).IsRequired();
        order.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        order.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        order.HasIndex(x => x.CreatedAt);

        var history = modelBuilder.Entity<OrderStatusHistory>();
        history.ToTable("order_status_history");
        history.HasKey(x => x.Id);
        history.Property(x => x.Id).HasColumnName("id");
        history.Property(x => x.OrderId).HasColumnName("order_id").IsRequired();
        history.Property(x => x.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        history.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
        history.Property(x => x.Source).HasColumnName("source").HasMaxLength(50).IsRequired();
        history.Property(x => x.MessageId).HasColumnName("message_id");
        history.HasIndex(x => new { x.OrderId, x.OccurredAt });
        history.HasOne<Order>().WithMany(x => x.StatusHistory).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}
