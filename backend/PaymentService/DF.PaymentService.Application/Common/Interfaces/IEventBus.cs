namespace DF.PaymentService.Application.Common.Interfaces;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;

    Task PublishAsync(string eventName, string message, CancellationToken cancellationToken = default);

    // Нове: async варіант
    Task SubscribeAsync<TEvent>(Func<TEvent, Task> handler, string? queueName = null, CancellationToken cancellationToken = default)
        where TEvent : class;

    // Backward-compatible
    void Subscribe<TEvent>(Func<TEvent, Task> handler)
        where TEvent : class;
}