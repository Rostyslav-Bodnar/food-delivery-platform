using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class OutboxDeadConfiguration : IEntityTypeConfiguration<OutboxDeadMessage>
{
    public void Configure(EntityTypeBuilder<OutboxDeadMessage> builder)
    {
        builder.ToTable("outbox_dead_messages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Content)
            .IsRequired();

        builder.Property(x => x.OccurredOn)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.FailedOn)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.RetryCount)
            .IsRequired();

        builder.Property(x => x.Error);

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(64);

        builder.HasIndex(x => x.FailedOn);
    }
}