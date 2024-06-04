using RabbitMQ.Client;

var factory = new ConnectionFactory{ HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// Dead Letter Exchange
const string dlxExchange = "dead_letter_exchange";
channel.ExchangeDeclare(exchange: dlxExchange, type: ExchangeType.Direct);

// Dead Letter Queue
var queue = channel.QueueDeclare(queue: "dead_letter_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
channel.QueueBind(queue: queue.QueueName, exchange: dlxExchange, routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for dead letters.");
