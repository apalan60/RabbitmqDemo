using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Consumer;
public class Consumer : IDisposable
{
    private readonly IConnection _connection;
    private const string Queue = "q.messages";
    private const string Exchange = "ex.messages";
    private const string DeadLetterExchange = "dead_letter_exchange";

    private Consumer()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        _connection = factory.CreateConnection();
        var channel = _connection.CreateModel();

        // Configuring a Dead Letter Exchange using Optional Queue Arguments
        var queueArgs = new ConcurrentDictionary<string, object>();
        queueArgs.TryAdd("x-dead-letter-exchange", DeadLetterExchange);
        queueArgs.TryAdd("x-message-ttl", 10000);
        var queue = channel.QueueDeclare(queue: Queue, durable: false, exclusive: false, autoDelete: true, arguments: queueArgs);
        
        channel.ExchangeDeclare(Exchange, ExchangeType.Direct);
        channel.QueueBind(Queue, Exchange,"123");
        
        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += OnMessageReceived;

        channel.BasicConsume(queue: queue.QueueName, autoAck: true, consumer: consumer);
    }

    public void Dispose() => _connection.Close();

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

        // if (message.StartsWith('R'))
        // {
        //     _channel.BasicReject(ea.DeliveryTag, false);
        // }
        // else
        // {
        //     Thread.Sleep(TimeSpan.FromSeconds(11));
        // }
    }
}