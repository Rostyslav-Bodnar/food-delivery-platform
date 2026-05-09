using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Domain.Entities.Common;
using Microsoft.EntityFrameworkCore;

namespace DF.PaymentService.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<CourierBalance> CourierBalances => Set<CourierBalance>();
    public DbSet<CourierEarning> CourierEarnings => Set<CourierEarning>();
    public DbSet<CourierPayout> CourierPayouts => Set<CourierPayout>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<OutboxDeadMessage>  OutboxDeadMessages => Set<OutboxDeadMessage>();
    public DbSet<PaymentTask> PaymentTasks => Set<PaymentTask>();
    public DbSet<ProcessedMessage> ProcessedMessages => Set<ProcessedMessage>();
    public DbSet<ProcessedWebhook> ProcessedWebhooks => Set<ProcessedWebhook>();

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var domainEvents = ChangeTracker
            .Entries<AggregateRoot>()
            .SelectMany(e => e.Entity.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            OutboxMessages.Add(OutboxMessage.Create(domainEvent));
        }

        foreach (var entry in ChangeTracker.Entries<AggregateRoot>())
        {
            entry.Entity.ClearDomainEvents();
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}
