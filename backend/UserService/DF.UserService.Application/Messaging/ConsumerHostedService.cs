using Microsoft.Extensions.Hosting;

namespace DF.UserService.Application.Messaging;

public class ConsumerHostedService(GetAccountConsumer consumer) : IHostedService
{

    public Task StartAsync(CancellationToken cancellationToken)
    {
        consumer.Start();
        Console.WriteLine("UserService RabbitMQ consumer started");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Console.WriteLine("UserService RabbitMQ consumer stopped");
        return Task.CompletedTask;
    }
}