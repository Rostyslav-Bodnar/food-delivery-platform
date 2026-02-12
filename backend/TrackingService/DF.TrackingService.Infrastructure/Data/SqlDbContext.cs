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
        });
    }
}