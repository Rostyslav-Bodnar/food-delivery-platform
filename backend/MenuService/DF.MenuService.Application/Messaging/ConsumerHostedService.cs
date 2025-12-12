using Microsoft.Extensions.Hosting;

namespace DF.MenuService.Application.Messaging;

public class ConsumerHostedService : IHostedService
{
    private readonly IEnumerable<IConsumer> _consumers;

    public ConsumerHostedService(IEnumerable<IConsumer> consumers)
    {
        _consumers = consumers;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            consumer.Start();
        }
        Console.WriteLine("All consumers started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("All consumers stopped");
        return Task.CompletedTask;
    }
}