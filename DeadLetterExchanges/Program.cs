using RabbitMQ.Client;

var factory = new ConnectionFactory{ HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(exchange: "dead_letter_exchange", type: ExchangeType.Direct);

var queue = channel.QueueDeclare(queue: "dead_letter_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

channel.QueueBind(queue: queue.QueueName, exchange: "dead_letter_exchange", routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for dead letters.");
