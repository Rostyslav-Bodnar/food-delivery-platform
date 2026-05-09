using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class PaymentTaskConfiguration : IEntityTypeConfiguration<PaymentTask>
{
    public void Configure(EntityTypeBuilder<PaymentTask> builder)
    {
        builder.ToTable("payment_tasks");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.PaymentId)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);

        builder.Property(x => x.CreatedOnUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.NextAttemptUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.ProcessedOnUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Error);

        builder.HasIndex(x => new { x.ProcessedOnUtc, x.NextAttemptUtc, x.Type });
        builder.HasIndex(x => x.PaymentId);
    }
}