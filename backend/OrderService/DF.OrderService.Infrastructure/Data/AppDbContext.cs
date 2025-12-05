using DF.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.OrderService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderedDish> OrderedDishes { get; set; }
    public DbSet<Location> Locations { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // LOCATION
        modelBuilder.Entity<Location>(entity =>
        {
            entity.ToTable("Locations");

            entity.HasKey(l => l.Id);

            entity.Property(l => l.FullAddress)
                .HasMaxLength(300)
                .IsRequired();

            entity.Property(l => l.City)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(l => l.Street)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(l => l.House)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(l => l.GeoPoint)
                .HasColumnType("geography (point)")
                .IsRequired(false);

        });

        // ORDER
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(o => o.Id);

            entity.Property(o => o.OrderDate)
                .IsRequired();

            entity.Property(o => o.TotalPrice)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(o => o.OrderStatus)
                .HasConversion<int>()
                .IsRequired();

            entity.HasOne(o => o.DeliverTo)
                .WithMany()
                .HasForeignKey(o => o.DeliverToId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(o => o.DeliverFrom)
                .WithMany()
                .HasForeignKey(o => o.DeliverFromId)
                .OnDelete(DeleteBehavior.Restrict);
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