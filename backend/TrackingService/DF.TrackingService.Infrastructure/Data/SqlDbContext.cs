using DF.TrackingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.TrackingService.Infrastructure.Data;

public class SqlDbContext(DbContextOptions<SqlDbContext> options) : DbContext(options)
{
    public DbSet<Location> Locations { get; set; }
    public DbSet<BusinessLocation> BusinessLocations { get; set; }
    
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

            // Spatial GIST index on GeoPoint — required for any PostGIS
            // proximity query to use the index instead of a full scan.
            entity.HasIndex(l => l.GeoPoint)
                .HasMethod("gist");

            // Unique partial index on OrderId — guards OrderCreatedConsumer
            // idempotency against at-least-once redelivery.
            entity.HasIndex(l => l.OrderId)
                .IsUnique()
                .HasFilter("\"OrderId\" IS NOT NULL");
        });

        modelBuilder.Entity<BusinessLocation>(entity =>
        {
            entity.ToTable("BusinessLocations");

            entity.HasKey(bl => bl.Id);

            entity.Property(bl => bl.BusinessId)
                .IsRequired();

            entity.Property(bl => bl.LocationId)
                .IsRequired();

            entity.HasOne(bl => bl.Location)
                .WithMany()
                .HasForeignKey(bl => bl.LocationId)
                .OnDelete(DeleteBehavior.Cascade);

            // GetByBusinessIdAsync is the hot read; index covers it.
            entity.HasIndex(bl => bl.BusinessId);
        });
    }
}