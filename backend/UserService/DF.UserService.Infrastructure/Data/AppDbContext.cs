using DF.UserService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Infrastructure.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Account> Accounts { get; set; }

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
        
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Account
            builder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasIndex(a => a.UserId)
                    .IsUnique();

                entity.Property(a => a.AccountType)
                    .HasConversion<int>() // enum -> int
                    .IsRequired();
                
                entity.Property(a => a.ImageUrl)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                entity.HasOne(a => a.User)
                    .WithOne()
                    .HasForeignKey<Account>(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("Accounts");
            });


            // User
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.FullName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.ToTable("Users");
            });

            // RefreshToken
            builder.Entity<RefreshToken>(entity =>
            {
                entity.HasKey(rt => rt.Id);

                entity.Property(rt => rt.Token)
                    .IsRequired();

                entity.Property(rt => rt.Expires)
                    .IsRequired();

                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("RefreshTokens");
            });
        }

    }
}
