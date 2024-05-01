using System.Text;
using RabbitMQ.Client;

//Create connection to rabbitmq server
//If we wanted to connect to a node on a different machine we'd simply specify its hostname or IP address here.
var factory = new ConnectionFactory
{
    HostName = "localhost"
    // Uri = new Uri("amqp://guset:guest@localhost:5672"),
    // ClientProvidedName = "Rabbitmq Demo app server"  
};
using var connection = factory.CreateConnection();

//create a channel, which is where most of the API for getting things done resides.
using var channel = connection.CreateModel();     

//Publish message to queue
channel.QueueDeclare(
    queue: "queue.consoleApp.Client1", 
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

const string message = """      
                       {
                           "First message": "Hello Rabbitmq",
                       }
                       """;
var body = Encoding.UTF8.GetBytes(message);

channel.BasicPublish(
    exchange: string.Empty, //default exchange
    routingKey: "queue.consoleApp.Client1",
    basicProperties: null,
    body: body);

Console.WriteLine($"[x] Sent {message} to queue");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();

//working on login failed