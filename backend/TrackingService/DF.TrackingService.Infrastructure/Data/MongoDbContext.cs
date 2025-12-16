using DF.TrackingService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.TrackingService.Infrastructure.Data;

public class MongoDbContext(DbContextOptions<MongoDbContext> options) : DbContext(options)
{
    public DbSet<CourierLocation> TrackingEvents { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseMongoDB("mongodb://localhost:27017", "TrackingDb");
    }
}