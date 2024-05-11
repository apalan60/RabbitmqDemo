using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory{HostName = "localhost"};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

const string exchangeName = "topic_logs";
channel.ExchangeDeclare(exchange: exchangeName, type: ExchangeType.Topic);

//Create binding keys
var queueName = channel.QueueDeclare().QueueName;

if (ArgsIsEmpty(args)) return;
foreach (var bindingKey in args)
{
    channel.QueueBind(queue: queueName, exchange: exchangeName, routingKey: bindingKey);
}

Console.WriteLine(" [*] Waiting for logs.");


var consumer = new EventingBasicConsumer(channel);
consumer.Received += (model, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var routingKey = ea.RoutingKey;
    Console.WriteLine(" [x] Received '{0}':'{1}'", routingKey, message);
};
channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();






bool ArgsIsEmpty(IReadOnlyCollection<string> strings)
{
    if (strings.Count >= 1) return false;
    Console.Error.WriteLine("Usage: {0} [binding_key...]",
        Environment.GetCommandLineArgs()[0]);
    Console.WriteLine(" Press [enter] to exit.");
    Console.ReadLine();
    Environment.ExitCode = 1;
    return true;
}