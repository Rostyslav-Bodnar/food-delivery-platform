using RabbitMQ.Client;

namespace DF.MenuService.Application.Messaging;

public static class MessagingTopology
{
    public const string DeadLetterExchange = "menu.dlx";
    public const string DeadLetterQueue = "menu.dlq";

    // Args to pass when declaring a primary queue so Nack(requeue:false) routes here.
    public static IDictionary<string, object?> DeadLetterArgs(string routingKey) => new Dictionary<string, object?>
    {
        ["x-dead-letter-exchange"] = DeadLetterExchange,
        ["x-dead-letter-routing-key"] = routingKey
    };

    public static async Task EnsureDeadLetterAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        // Catch-all: anything dead-lettered into the DLX lands in the same DLQ regardless of routing key.
        await channel.QueueBindAsync(
            queue: DeadLetterQueue,
            exchange: DeadLetterExchange,
            routingKey: "#",
            cancellationToken: cancellationToken);
    }
}
