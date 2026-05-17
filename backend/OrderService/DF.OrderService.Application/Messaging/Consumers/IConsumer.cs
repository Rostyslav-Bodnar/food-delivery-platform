namespace DF.OrderService.Application.Messaging.Consumers;

public interface IConsumer : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
