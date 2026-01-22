using Microsoft.Extensions.Hosting;

namespace DF.OrderService.Application.Messaging.Consumers;

public class ConsumerHostedService(IEnumerable<IConsumer> consumers) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
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