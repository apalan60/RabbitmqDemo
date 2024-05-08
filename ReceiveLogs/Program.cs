using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ExchangeDeclare(exchange: "logs", type: ExchangeType.Fanout);

//Binding the exchange to the queue
var queueName = channel.QueueDeclare().QueueName;   //a random queue name. For example it may look like amq.gen-JzTY20BRgKO-HjmUJj0wLg.
channel.QueueBind(queue: queueName, exchange: "logs", routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for logs.");

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (_, ea) =>
{
    var body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] {message}");
};
channel.BasicConsume(queue: queueName, autoAck: true, consumer: consumer);  


Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
