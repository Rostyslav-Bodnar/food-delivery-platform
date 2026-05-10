using DF.MenuService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dish> Dishes { get; set; }
    
    public DbSet<Ingredient> Ingredients { get; set; }

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

            entity.Property(d => d.Price)
                .HasColumnType("decimal(10,2)");
        });

        modelBuilder.Entity<Ingredient>(entity =>
            {
                entity.ToTable("Ingredients");
                entity.HasKey(i => i.Id);
                
                entity.Property(i => i.Name)
                    .IsRequired()
                    .HasMaxLength(100);
                
                entity.HasOne(i => i.Dish)
                    .WithMany()
                    .HasForeignKey(d => d.DishId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        );
    }
}