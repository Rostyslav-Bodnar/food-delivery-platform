using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using RabbitMQ.Client;
using System.Text;

namespace DF.UserService.Infrastructure.Messaging
{
    public class RabbitMqMessageBroker : IMessageBroker
    {
        private readonly IConnection connection;
        private readonly IModel channel;

        public RabbitMqMessageBroker(IConfiguration config)
        {
            var factory = new ConnectionFactory()
            {
                HostName = config["RabbitMQ:Host"],
                UserName = config["RabbitMQ:User"],
                Password = config["RabbitMQ:Pass"]
            };
            connection = factory.CreateConnection();
            channel = connection.CreateModel();
            channel.ExchangeDeclare("user-exchange", ExchangeType.Topic, durable: true);
        }

        public void Publish(string routingKey, object message)
        {
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            channel.BasicPublish("user-exchange", routingKey, null, body);
        }
    }
}
