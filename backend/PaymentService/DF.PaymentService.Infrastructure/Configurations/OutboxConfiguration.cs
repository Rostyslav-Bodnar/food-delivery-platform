using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class OutboxConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.OccurredOn)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ProcessedOn)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Error);

        builder.Property(x => x.RetryCount)
            .HasDefaultValue(0);

        builder.Property(x => x.NextAttempt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.Poisoned)
            .HasDefaultValue(false);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(64);

        // Індекси під публішер
        builder.HasIndex(x => new { x.ProcessedOn, x.Poisoned, x.NextAttempt });
        builder.HasIndex(x => x.OccurredOn);
    }
}