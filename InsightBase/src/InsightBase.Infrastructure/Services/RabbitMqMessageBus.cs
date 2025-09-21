using System.Data;
using System.Text;
using InsightBase.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace InsightBase.Infrastructure.Services
{
    public class RabbitMqMessageBus : IMessageBus, IDisposable
    {
        private readonly IConnection _connection;
        private readonly ILogger<RabbitMqMessageBus> _logger;
        public RabbitMqMessageBus(IConfiguration configuration, ILogger<RabbitMqMessageBus> logger)
        {
            _logger = logger;
            var factory = new ConnectionFactory
            {
                HostName = configuration["RABBITMQ_HOST"],
                Port = int.Parse(configuration["RABBITMQ_PORT"]),
                UserName = configuration["RABBITMQ_USER"],
                Password = configuration["RABBITMQ_PASSWORD"],

                DispatchConsumersAsync = true,

                AutomaticRecoveryEnabled = true,   // connection düştüğünde otomatik reconnect
                TopologyRecoveryEnabled = true,    // exchange/queue yeniden declare et
                RequestedHeartbeat = TimeSpan.FromSeconds(30) // broker ölü connection tespit etmeden canlı tutar
            };

            _connection = factory.CreateConnection();
            //_channel = _connection.CreateModel();
            _logger.LogInformation($"RabbitMQ connected to {factory.HostName}:{factory.Port}");
        }

        public Task PublishAsync<T>(string queueName, T message)
        {
            var channel = _connection.CreateModel(); //publish ve subscribe için ayrı connection açılıyor biri kapandığında diğeri de kapanmaması için
            channel.QueueDeclare(queue: queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);

            var json = JsonConvert.SerializeObject(message);
            var body = Encoding.UTF8.GetBytes(json);
            
            channel.BasicPublish(exchange: "",
                                routingKey: queueName,
                                basicProperties: null,
                                body: body);
            return Task.CompletedTask;
        }

        public void Subscribe<T>(string queueName, Func<T, Task> handler)
        {
            var channel = _connection.CreateModel();
            channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);

            var consumer = new AsyncEventingBasicConsumer(channel);
            consumer.Received += async (modei, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var json = Encoding.UTF8.GetString(body);
                    var message = JsonConvert.DeserializeObject<T>(json);
                    if (message == null)
                    {
                        _logger.LogWarning("Received null message on {QueueName}", queueName);
                        throw new Exception("Message deserialization returned null on RabbitMqMessageBus Subscribe.");
                    }
                    _logger.LogInformation($"Message received on {queueName}: {json}");

                    await handler(message);
                    channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error RabbitMqMessageBus Subscribe processing message: {ex}");
                    channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                }
            };
            channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
            _logger.LogInformation($"Subscribed to RabbitMQ queue: {queueName}");
        }

        public void Dispose() => _connection?.Dispose();
    }
}