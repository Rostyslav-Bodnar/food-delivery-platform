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

            // === USER ===
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(u => u.Surname)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(u => u.UserRole)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(u => u.CreatedAt)
                    .HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Поточний акаунт (nullable, один до одного)
                entity.HasOne(u => u.CurrentAccount)
                    .WithMany() // CurrentAccount не має колекції
                    .HasForeignKey(u => u.AccountId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable("Users");
            });

            // === BASE ACCOUNT (abstract) ===
            builder.Entity<Account>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.Property(a => a.AccountType)
                    .HasConversion<int>()
                    .IsRequired();

                entity.Property(a => a.ImageUrl)
                    .HasMaxLength(500)
                    .IsUnicode(false);

                // Один користувач може мати багато акаунтів
                entity.HasOne(a => a.User)
                    .WithMany(u => u.Accounts)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Унікальний індекс: один акаунт одного типу на користувача
                entity.HasIndex(a => new { a.UserId, a.AccountType }).IsUnique();

                entity.ToTable("Accounts");
            });

            // === COURIER ACCOUNT ===
            builder.Entity<CourierAccount>(entity =>
            {
                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.Surname)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.Address)
                    .HasMaxLength(300);
                
                entity.Property(c => c.Description)
                    .HasMaxLength(1000);
                
                entity.Property(a => a.PhoneNumber)
                    .HasMaxLength(20);

                entity.ToTable("CourierAccounts");
            });

            // === BUSINESS ACCOUNT ===
            builder.Entity<BusinessAccount>(entity =>
            {
                entity.Property(b => b.Name)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(b => b.Description)
                    .HasMaxLength(1000);

                entity.ToTable("BusinessAccounts");
            });

            // === CUSTOMER ACCOUNT ===
            builder.Entity<CustomerAccount>(entity =>
            {
                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.Surname)
                    .HasMaxLength(100)
                    .IsRequired();

                entity.Property(c => c.Address)
                    .HasMaxLength(300);
                
                entity.Property(a => a.PhoneNumber)
                    .HasMaxLength(20);

                entity.ToTable("CustomerAccounts");
            });

            // === REFRESH TOKEN ===
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
