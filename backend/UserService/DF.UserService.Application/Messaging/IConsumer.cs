namespace DF.UserService.Application.Messaging;

public interface IConsumer : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
