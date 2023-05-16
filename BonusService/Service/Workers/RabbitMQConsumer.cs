using BonusService.Service.BLL;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace BonusService.Service.Workers
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IMessageHandler _messageHandler;
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private readonly List<string> _queueNames = new(){ "VerificationReward" };
        public RabbitMQConsumer(IConnectionFactory connectionFactory, IMessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
            _connection = connectionFactory.CreateConnection();
            _channel = _connection.CreateModel();
        }

        
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var queueName in _queueNames)
            {
                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += async (model, ea) =>
                {
                    var key = ea.RoutingKey;
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    await _messageHandler.HandleMessageAsync(key, message);
                };

                _channel.BasicConsume(queue: queueName,
                         autoAck: true,
                         consumer: consumer);
            }

            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel.Close();
            _connection.Close();
            base.Dispose();
        }
    }
}
