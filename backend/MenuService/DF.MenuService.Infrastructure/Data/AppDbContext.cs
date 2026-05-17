using DF.MenuService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dish> Dishes { get; set; }

    public DbSet<Ingredient> Ingredients { get; set; }

    public DbSet<IdempotencyKey> IdempotencyKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // DISH
        modelBuilder.Entity<Dish>(entity =>
        {
            entity.ToTable("Dishes");
            entity.HasKey(d => d.Id);

            entity.Property(d => d.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(d => d.Description)
                .HasMaxLength(500);

            entity.Property(d => d.Image)
                .HasMaxLength(300);

            entity.Property(d => d.ImagePublicId)
                .HasMaxLength(200);

            entity.Property(d => d.Price)
                .HasColumnType("decimal(10,2)");

            // Optimistic concurrency via Postgres' system xmin column.
            entity.Property<uint>("xmin")
                .HasColumnName("xmin")
                .IsRowVersion();
        });

        modelBuilder.Entity<IdempotencyKey>(entity =>
        {
            entity.ToTable("IdempotencyKeys");
            entity.HasKey(k => k.Key);
            entity.Property(k => k.Key).HasMaxLength(100);
            entity.Property(k => k.Method).HasMaxLength(10).IsRequired();
            entity.Property(k => k.Path).HasMaxLength(200).IsRequired();
            entity.HasIndex(k => k.ExpiresAt);
        });

        modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.ToTable("Ingredients");
                entity.HasKey(i => i.Id);

                entity.Property(i => i.Name)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(i => i.Dish)
                    .WithMany(d => d.Ingredients)
                    .HasForeignKey(d => d.DishId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        );
    }
}