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
        private readonly IModel _channel;
        private readonly ILogger<RabbitMqMessageBus> _logger;
        public RabbitMqMessageBus(IConfiguration configuration, ILogger<RabbitMqMessageBus> logger)
        {
            _logger = logger;
            var host = configuration["RABBITMQ_HOST"];
            var port = int.Parse(configuration["RABBITMQ_PORT"]);
            var user = configuration["RABBITMQ_USER"];
            var password = configuration["RABBITMQ_PASSWORD"];
            var factory = new ConnectionFactory
            {
                HostName = host,
                Port = port,
                UserName = user,
                Password = password,
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _logger.LogInformation("RabbitMQ connected to {Host}:{Port}", host, port);

        }

        public Task PublishAsync<T>(string queueName, T message)
        {
            _channel.QueueDeclare(queue: queueName,
                                durable: true,
                                exclusive: false,
                                autoDelete: false,
                                arguments: null);
            var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message));
            _channel.BasicPublish(exchange: "",
                                routingKey: queueName,
                                basicProperties: null,
                                body: body);
            return Task.CompletedTask;
        }

        public void Subscribe<T>(string queueName, Func<T, Task> handler)
        {
            _channel.QueueDeclare(queue: queueName,
                                 durable: true,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (modei, ea) =>
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                _logger.LogInformation("Message received on {Queue}: {Json}", queueName, json);
                try
                {
                    var message = JsonConvert.DeserializeObject<T>(json);
                    await handler(message);
                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    _channel.BasicNack(ea.DeliveryTag, false, requeue: true);
                }
            };
            _channel.BasicConsume(queue: queueName,
                                 autoAck: false,
                                 consumer: consumer);
        }
        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }


    }
}