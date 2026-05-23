using DF.UserService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DF.UserService.Infrastructure.Data
{
    public class AppDbContext(DbContextOptions<AppDbContext> options)
        : IdentityDbContext<User, IdentityRole<Guid>, Guid>(options)
    {
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<PayoutRecord> PayoutRecords { get; set; }
        public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();
        
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
                // SetNull (not Restrict): when the referenced Account is deleted
                // (including via cascade from User → Accounts), null out the
                // User.AccountId pointer. Restrict here used to block User
                // deletion entirely because Account → User is Cascade.
                entity.HasOne(u => u.CurrentAccount)
                    .WithMany()
                    .HasForeignKey(u => u.AccountId)
                    .OnDelete(DeleteBehavior.SetNull);

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

                entity.Property(b => b.StripeProvisioningLastError)
                    .HasMaxLength(500);

                // Worker query indexes onto these — accelerates the "pending"
                // and "ready-for-retry" filters in GetPendingStripeAccountsAsync.
                entity.HasIndex(b => b.StripeAccountId);
                entity.HasIndex(b => b.StripeProvisioningLastAttemptUtc);

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

                entity.Property(rt => rt.TokenHash)
                    .HasMaxLength(64)
                    .IsRequired();

                entity.Property(rt => rt.ReplacedByHash)
                    .HasMaxLength(64);

                entity.Property(rt => rt.CreatedAtUtc)
                    .IsRequired();

                entity.Property(rt => rt.Expires)
                    .IsRequired();

                entity.HasIndex(rt => rt.TokenHash).IsUnique();
                entity.HasIndex(rt => rt.UserId);

                entity.Ignore(rt => rt.IsExpired);
                entity.Ignore(rt => rt.IsActive);

                // Postgres-native concurrency token (system xmin column).
                // Prevents two simultaneous Refresh calls from rotating the
                // same row twice and forking the chain.
                entity.Property<uint>("xmin")
                    .HasColumnType("xid")
                    .ValueGeneratedOnAddOrUpdate()
                    .IsConcurrencyToken();

                entity.HasOne(rt => rt.User)
                    .WithMany()
                    .HasForeignKey(rt => rt.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.ToTable("RefreshTokens");
            });
            
            // === PROCESSED WEBHOOK ===
            builder.Entity<ProcessedWebhook>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.WebhookId)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(p => p.ProcessedAtUtc)
                    .IsRequired();

                entity.HasIndex(p => p.WebhookId).IsUnique();

                entity.ToTable("ProcessedWebhooks");
            });

            // === PAYOUT RECORD ===
            builder.Entity<PayoutRecord>(entity =>
            {
                entity.HasKey(p => p.Id);

                entity.Property(p => p.StripeAccountId)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(p => p.StripePayoutId)
                    .HasMaxLength(255)
                    .IsRequired();

                entity.Property(p => p.Currency)
                    .HasMaxLength(10)
                    .IsRequired();

                entity.Property(p => p.Status)
                    .HasMaxLength(50)
                    .IsRequired();

                entity.Property(p => p.FailureCode)
                    .HasMaxLength(100);

                entity.Property(p => p.FailureMessage)
                    .HasMaxLength(1000);

                entity.Property(p => p.BalanceTransactionId)
                    .HasMaxLength(255);

                entity.Property(p => p.AmountMinor)
                    .IsRequired();

                entity.Property(p => p.CreatedAtUtc)
                    .IsRequired();

                // Business relationship (важливо)
                entity.HasOne<BusinessAccount>()
                    .WithMany()
                    .HasForeignKey(p => p.BusinessId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Indexes (дуже важливо для Stripe sync)
                entity.HasIndex(p => p.StripePayoutId).IsUnique();
                entity.HasIndex(p => p.StripeAccountId);
                entity.HasIndex(p => p.BusinessId);
                entity.HasIndex(p => p.Status);
                entity.HasIndex(p => p.CreatedAtUtc);

                entity.ToTable("PayoutRecords");
            });
        }

    }
}
