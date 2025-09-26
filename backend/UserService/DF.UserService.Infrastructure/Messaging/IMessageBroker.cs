namespace DF.UserService.Infrastructure.Messaging
{
    public interface IMessageBroker
    {
        void Publish(string routingKey, object message);
    }
}
