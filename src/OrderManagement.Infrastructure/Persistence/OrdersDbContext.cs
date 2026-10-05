using Microsoft.EntityFrameworkCore;
using OrderManagement.Domain.Orders;

namespace OrderManagement.Infrastructure.Persistence;

public sealed class OrdersDbContext(DbContextOptions<OrdersDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

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
    }
}
