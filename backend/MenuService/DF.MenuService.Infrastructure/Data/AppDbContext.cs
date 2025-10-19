using DF.MenuService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DF.MenuService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Menu> Menus { get; set; }
    public DbSet<Dish> Dishes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // MENU
        modelBuilder.Entity<Menu>(entity =>
        {
            entity.ToTable("Menus");
            entity.HasKey(m => m.Id);

            entity.Property(m => m.Name)
                .HasMaxLength(100);

            entity.Property(m => m.Image)
                .HasMaxLength(300);
        });

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

            // One Menu → Many Dishes
            entity.HasOne<Menu>()
                .WithMany()
                .HasForeignKey(d => d.MenuId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}