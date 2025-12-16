using DF.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.OrderService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderedDish> OrderedDishes { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ORDER
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.BusinessId)
                .IsRequired();

            entity.Property(o => o.OrderedBy)
                .IsRequired();

            entity.Property(o => o.OrderDate)
                .IsRequired();

            entity.Property(o => o.TotalPrice)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(o => o.OrderStatus)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(o => o.OrderNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(o => o.DeliverToId);

            entity.Property(o => o.DeliverFromId);

            entity.Property(o => o.DeliveredById);
        });

        // ORDERED DISH
        modelBuilder.Entity<OrderedDish>(entity =>
        {
            entity.ToTable("OrderedDishes");

            entity.HasKey(od => od.Id);

            entity.Property(od => od.OrderId)
                .IsRequired();

            entity.Property(od => od.DishId)
                .IsRequired();

            entity.HasOne(od => od.Order)
                .WithMany()
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}