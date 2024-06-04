﻿using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer;
public class Consumer : IDisposable
{
    private readonly IModel _channel;
    private readonly IConnection _connection;
    
    private Consumer()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Configuring a Dead Letter Exchange using Optional Queue Arguments, not binding the queue to the exchange
        var queueArgs = new ConcurrentDictionary<string, object>();
        queueArgs.TryAdd("x-dead-letter-exchange", "dead_letter_exchange");
        queueArgs.TryAdd("x-message-ttl", 20000);
        var queue = _channel.QueueDeclare(durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);

        // Binding the queue to the exchange
        const string exchange = "ex.messages";
        _channel.ExchangeDeclare(exchange, ExchangeType.Direct);
        _channel.QueueBind(queue: queue.QueueName, exchange: exchange, routingKey: string.Empty);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += OnMessageReceived;

        // Negative acknowledgment
        _channel.BasicConsume(queue: queue.QueueName, autoAck: false, consumer: consumer);
    }
    
    public void Dispose()
    {
        _connection.Close();
    }

    public static void Main()
    {
        using var consumer = new Consumer();
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }
    
    private void OnMessageReceived(object? sender, BasicDeliverEventArgs ea)
    {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($" [x] {message}");

        if (message.StartsWith('R'))
        {
            _channel.BasicReject(ea.DeliveryTag, false);
        }
        else
        {
            Thread.Sleep(TimeSpan.FromSeconds(21));
            _channel.BasicAck(ea.DeliveryTag, false);
        }
    }
}