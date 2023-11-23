using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace OCRBilletParse.Queue.Factory
{
    public class RabbitMqConnectionFactory
    {
        private IConfiguration Config { get; }
        private readonly string _queueName;
        private IModel _channel;
        public RabbitMqConnectionFactory(IConfiguration config, string queueName)
        {
            Config = config;
            _queueName = queueName;
            //InstanceMq();
        }
        public string QueueName => _queueName;
        public IModel GetConnection() => _channel;
        public IModel CreateQueue() 
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                Uri = new Uri(Config["Services:rabbitmq:ConnectionString"])
            };

            IConnection conn = factory.CreateConnection();
            _channel = conn.CreateModel();
            _channel.QueueDeclare(queue: _queueName,
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);
            return _channel;
        }
    }
}
