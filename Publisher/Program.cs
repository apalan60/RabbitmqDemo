using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory
{
    HostName = "localhost"
};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();     

const string exchange = "ex.messages";
channel.ExchangeDeclare(exchange, ExchangeType.Direct);

channel.QueueDeclare(
    queue: "q.messages",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: exchange,
    routingKey: string.Empty,
    basicProperties: null,
    body: body);

Console.WriteLine($"[x] Sent {message} to queue");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();


static string GetMessage(string[] args)
{
    return args.Length > 0 ? string.Join(" ", args) : "reject this message";
}