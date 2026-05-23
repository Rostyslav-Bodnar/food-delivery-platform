using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.OrderService.Application.Messaging.Consumers;

public class ConsumerHostedService(
    IEnumerable<IConsumer> consumers,
    ILogger<ConsumerHostedService> logger) : IHostedService
{
    private readonly IReadOnlyList<IConsumer> _consumers = consumers.ToList();

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            await consumer.StartAsync(cancellationToken);
        }
        logger.LogInformation("Started {Count} RabbitMQ consumer(s)", _consumers.Count);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in _consumers)
        {
            try
            {
                await consumer.StopAsync(cancellationToken);
                await consumer.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error stopping consumer {Type}", consumer.GetType().Name);
            }
        }
        logger.LogInformation("Stopped {Count} RabbitMQ consumer(s)", _consumers.Count);
    }
}
