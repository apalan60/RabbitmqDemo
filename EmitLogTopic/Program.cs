using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

const string exchangeName = "topic_logs";
var routingKey = args.Length > 0 ? args[0] : "anonymous.info";
var message = args.Length > 1 ? string.Join(" ", args.Skip(1).ToArray()) : "Hello World!";

channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic);

channel.BasicPublish(exchange: exchangeName,
    routingKey: routingKey,
    basicProperties: null,
    body:Encoding.UTF8.GetBytes(message));

Console.WriteLine($" [x] Sent '{routingKey}':'{message}'");
