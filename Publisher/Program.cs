using System.Text;
using RabbitMQ.Client;


const string exchange = "ex.messages";
const string queue = "q.messages";

var factory = new ConnectionFactory
{
    HostName = "localhost"
};
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();     


var message = GetMessage(args);
var body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: exchange,
    routingKey: queue,
    basicProperties: null,
    body: body);

Console.WriteLine($"[x] Sent {message} to queue");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();


static string GetMessage(string[] args)
{
    return args.Length > 0 ? string.Join(" ", args) : "Reject this message";
}