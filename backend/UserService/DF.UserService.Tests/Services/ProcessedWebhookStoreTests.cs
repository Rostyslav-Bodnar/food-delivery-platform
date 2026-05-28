using DF.UserService.Application.Services;
using DF.UserService.Domain.Entities;
using DF.UserService.Tests.Helpers;

namespace DF.UserService.Tests.Services;

public class ProcessedWebhookStoreTests
{
    [Fact]
    public async Task ExistsAsync_ReturnsFalse_WhenWebhookNotSeen()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new ProcessedWebhookStore(db);

        var exists = await sut.ExistsAsync("evt_unknown", CancellationToken.None);

        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsAsync_ReturnsTrue_AfterMarkProcessed()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new ProcessedWebhookStore(db);

        await sut.MarkProcessedAsync("evt_123", CancellationToken.None);
        var exists = await sut.ExistsAsync("evt_123", CancellationToken.None);

        exists.Should().BeTrue();
    }

    [Fact]
    public async Task MarkProcessedAsync_PersistsRow()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new ProcessedWebhookStore(db);

        await sut.MarkProcessedAsync("evt_persist", CancellationToken.None);

        db.ProcessedWebhooks.Should().ContainSingle()
            .Which.WebhookId.Should().Be("evt_persist");
    }

    [Fact]
    public async Task ExistsAsync_OnlyMatchesExactWebhookId()
    {
        await using var db = DbContextFactory.CreateInMemory();
        var sut = new ProcessedWebhookStore(db);

        await sut.MarkProcessedAsync("evt_abc", CancellationToken.None);

        (await sut.ExistsAsync("evt_abc", CancellationToken.None)).Should().BeTrue();
        (await sut.ExistsAsync("evt_xyz", CancellationToken.None)).Should().BeFalse();
    }
}
