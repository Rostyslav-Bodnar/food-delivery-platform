using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class CourierEarningConfiguration : IEntityTypeConfiguration<CourierEarning>
{
    public void Configure(EntityTypeBuilder<CourierEarning> builder)
    {
        builder.ToTable("courier_earnings");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.OrderId).IsUnique();
        builder.HasIndex(x => new { x.CourierId, x.Status, x.AvailableAtUtc });

        builder.Property(x => x.CourierId)
            .IsRequired();

        builder.Property(x => x.OrderId)
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

        builder.Property(x => x.EarnedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.AvailableAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.PaidAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.StripeTransferId)
            .HasMaxLength(128);

        builder.Ignore(x => x.DomainEvents);
    }
}
