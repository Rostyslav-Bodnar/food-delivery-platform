using RabbitMQ.Client;

namespace DF.UserService.Application.Messaging;

public static class MessagingTopology
{
    public const string DeadLetterExchange = "user.dlx";
    public const string DeadLetterQueue = "user.dlq";

    public static IDictionary<string, object?> DeadLetterArgs(string routingKey) => new Dictionary<string, object?>
    {
        ["x-dead-letter-exchange"] = DeadLetterExchange,
        ["x-dead-letter-routing-key"] = routingKey
    };

    public static async Task EnsureDeadLetterAsync(IChannel channel, CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: DeadLetterExchange, type: ExchangeType.Direct,
            durable: true, autoDelete: false, cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: DeadLetterQueue, exchange: DeadLetterExchange, routingKey: "#",
            cancellationToken: cancellationToken);
    }
}
