using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.UserService.Application.Messaging;

public class ConsumerHostedService(
    IEnumerable<IConsumer> consumers,
    ILogger<ConsumerHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
        {
            try
            {
                await consumer.StartAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex,
                    "Failed to start consumer {Consumer}",
                    consumer.GetType().Name);
                throw;
            }
        }

        logger.LogInformation("All consumers started");
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        foreach (var consumer in consumers)
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
        logger.LogInformation("All consumers stopped");
    }
}
