using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class CourierBalanceConfiguration : IEntityTypeConfiguration<CourierBalance>
{
    public void Configure(EntityTypeBuilder<CourierBalance> builder)
    {
        builder.ToTable("courier_balances");

        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.CourierId).IsUnique();

        builder.Property(x => x.CourierId)
            .IsRequired();

        builder.Property(x => x.PendingAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.AvailableAmount)
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.StripeAccountId)
            .HasMaxLength(128);

        builder.Property(x => x.PayoutsEnabled)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
