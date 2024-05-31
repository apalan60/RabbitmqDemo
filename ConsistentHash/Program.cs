using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory
{
    HostName = "127.0.0.1",
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

const string exchange = "ex.Hash";
const string exchangeType = "x-consistent-hash";
channel.ExchangeDeclare(exchange, exchangeType, durable: true, autoDelete: false);

for (var i = 1; i < 4; i++)
{
    var queue = channel.QueueDeclare($"q.consistent.Hash.{i}", durable: true, exclusive: false, autoDelete: false).QueueName;
    channel.QueuePurge(queue);

    var routingKey = i.ToString();  //Binding Weight  => 1 :  2 :  3  
    channel.QueueBind(queue, exchange, routingKey);
    Console.WriteLine($"queue:{queue},  binding weight:{routingKey}");
}



channel.ConfirmSelect();

var properties = channel.CreateBasicProperties();

//Publish 100k messages
for (int i = 0; i < 100000; i++)
{
    var body = Encoding.UTF8.GetBytes(i.ToString());
    channel.BasicPublish(exchange: exchange,
        routingKey: i.ToString(),   //different routing key for each message
        basicProperties: properties,
        body: body);
}

channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(600));
Console.WriteLine("Done publishing 100k messages");
