using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

// declare the queue from which we're going to consume
channel.QueueDeclare(
    queue: "queue.Task.Publisher1",
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);


Console.WriteLine(" [*] Waiting for messages.");

//Declare a consumer
var consumer = new EventingBasicConsumer(channel);
consumer.Received += (_, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] Received {message}");
    
    //fake a second of work for every dot in the message body.
    int dots = message.Split('.').Length - 1;
    Thread.Sleep(dots * 1000);
    Console.WriteLine(" [x] Done");
};

//Consume the message
channel.BasicConsume(queue: "queue.Task.Publisher1",
    autoAck: false, 
    consumer: consumer);
                     
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();


