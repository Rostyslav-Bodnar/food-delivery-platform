using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId)
               .IsRequired();

        // Унікальний індекс по OrderId для ідемпотентності
        builder.HasIndex(p => p.OrderId)
               .IsUnique();

        // Індекс по StripePaymentIntentId для швидкого пошуку у вебхуках
        builder.HasIndex(p => p.StripePaymentIntentId);

        // Enum -> string
        builder.Property(p => p.Method)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(p => p.Status)
               .HasConversion<string>()
               .IsRequired();

        builder.Property(p => p.StripePaymentIntentId)
               .HasMaxLength(128);

        builder.Property(p => p.StripeClientSecret)
               .HasMaxLength(256);

        builder.Property(p => p.CancelReason)
               .HasMaxLength(256);

        builder.Property(p => p.FailReason)
               .HasMaxLength(256);

        builder.Property(p => p.DisputeId)
               .HasMaxLength(128);

        builder.Property(p => p.DisputeStatus)
               .HasMaxLength(64);

        // Дати: використовуємо timestamptz (UTC)
        builder.Property(p => p.ExpiresAt)
               .HasColumnType("timestamp with time zone");

        builder.Property(p => p.EvidenceSubmittedAt)
               .HasColumnType("timestamp with time zone");

        // ----- Owned ValueObject: Amount (Money)
        builder.OwnsOne(p => p.Amount, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("amount_value")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("amount_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // ----- Owned ValueObject: TotalRefunded (Money)
        builder.OwnsOne(p => p.TotalRefunded, money =>
        {
            money.Property(m => m.Amount)
                 .HasColumnName("total_refunded_value")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

            money.Property(m => m.Currency)
                 .HasColumnName("total_refunded_currency")
                 .HasMaxLength(3)
                 .IsRequired();
        });

        // ----- Колекція Refunds як OwnsMany
        builder.OwnsMany(p => p.Refunds, rr =>
        {
            rr.ToTable("payment_refunds");
            rr.WithOwner().HasForeignKey("payment_id");

            rr.HasKey(x => x.Id);
            rr.Property(x => x.Id).ValueGeneratedNever();

            rr.Property<Guid>("payment_id");

            rr.Property(x => x.StripeRefundId)
              .HasMaxLength(128);

            rr.Property(x => x.OccurredOnUtc)
              .HasColumnType("timestamp with time zone")
              .IsRequired();

            rr.OwnsOne(x => x.Amount, m =>
            {
                m.Property(v => v.Amount)
                 .HasColumnName("amount_value")
                 .HasColumnType("numeric(18,2)")
                 .IsRequired();

                m.Property(v => v.Currency)
                 .HasColumnName("amount_currency")
                 .HasMaxLength(3)
                 .IsRequired();
            });

            rr.HasIndex("payment_id"); // частий фільтр
        });

        // Domain events не мапимо
        builder.Ignore(p => p.DomainEvents);
    }
}