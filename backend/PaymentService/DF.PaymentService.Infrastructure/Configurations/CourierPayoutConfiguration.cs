using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class CourierPayoutConfiguration : IEntityTypeConfiguration<CourierPayout>
{
    public void Configure(EntityTypeBuilder<CourierPayout> builder)
    {
        builder.ToTable("courier_payouts");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => new { x.CourierId, x.Status });

        builder.Property(x => x.CourierId)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ProcessedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.StripeTransferId)
            .HasMaxLength(128);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(512);
    }
}
