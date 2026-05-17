using DF.OrderService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.OrderService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderedDish> OrderedDishes { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }
    
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

            entity.Property(o => o.DeliveryFee)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(o => o.CourierFee)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(o => o.OrderStatus)
                .HasConversion<int>()
                .IsRequired();

            entity.Property(o => o.CourierPaid)
                .IsRequired();

            entity.Property(o => o.OrderNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(o => o.DeliverToId);

            entity.Property(o => o.DeliverFromId);

            entity.Property(o => o.DeliveredById);

            // Optimistic concurrency via Postgres' system xmin column.
            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .IsRowVersion();
        });

        // IDEMPOTENCY KEYS
        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("IdempotencyKeys");
            entity.HasKey(k => k.Key);
            entity.Property(k => k.Key).HasMaxLength(100);
            entity.Property(k => k.Method).HasMaxLength(10).IsRequired();
            entity.Property(k => k.Path).HasMaxLength(200).IsRequired();
            entity.HasIndex(k => k.ExpiresAt);
        });

        // OUTBOX
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(o => o.Id);
            entity.Property(o => o.EventType).IsRequired().HasMaxLength(100);
            entity.Property(o => o.Payload).IsRequired();
            entity.Property(o => o.OccurredAtUtc).IsRequired();
            entity.HasIndex(o => new { o.DispatchedAtUtc, o.OccurredAtUtc });
        });

        // ORDERED DISH
        modelBuilder.Entity<OrderedDish>(entity =>
        {
            entity.ToTable("OrderedDishes");

            entity.HasKey(od => od.Id);

            entity.Property(od => od.OrderId).IsRequired();
            entity.Property(od => od.DishId).IsRequired();

            entity.Property(od => od.Quantity).IsRequired();
            entity.Property(od => od.UnitPrice)
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            entity.Property(od => od.DishName)
                .HasMaxLength(200)
                .IsRequired();

            entity.HasOne(od => od.Order)
                .WithMany(o => o.OrderedDishes)
                .HasForeignKey(od => od.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
