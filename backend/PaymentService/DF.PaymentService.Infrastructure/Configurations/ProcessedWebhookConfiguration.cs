using DF.PaymentService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DF.PaymentService.Infrastructure.Configurations;

public class ProcessedWebhookConfiguration : IEntityTypeConfiguration<ProcessedWebhook>
{
    public void Configure(EntityTypeBuilder<ProcessedWebhook> builder)
    {
        builder.ToTable("processed_webhooks");

        builder.HasKey(x => x.EventId);

        builder.Property(x => x.EventId)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.ReceivedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(x => x.ReceivedAtUtc);
    }
}