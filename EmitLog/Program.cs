using System.Text;
using RabbitMQ.Client;

//The messages will be lost if no queue is bound to the exchange yet, but that's okay for us; if no consumer is listening yet we can safely discard the message.
var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Fanout);

var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);


channel.BasicPublish(exchange: "logs", routingKey: string.Empty, basicProperties: null, body: body);




Console.WriteLine($" [x] Sent {message}");

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
static string GetMessage(string[] args)
{
    return args.Length > 0 ? string.Join(" ", args) : "info: Hello World!";
}