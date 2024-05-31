using System.Text;
using RabbitMQ.Client;

var factory = new ConnectionFactory
{
    HostName = "127.0.0.1",
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();

channel.ConfirmSelect();

var properties = channel.CreateBasicProperties();
const string exchangeName = "ex.hash";

//Publish 100k messages
for (int i = 0; i < 100000; i++)
{
    var body = Encoding.UTF8.GetBytes(i.ToString());
    channel.BasicPublish(exchange: exchangeName,
        routingKey: i.ToString(),   //different routing key for each message
        basicProperties: properties,
        body: body);
}

channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(600));
Console.WriteLine("Done publishing 100k messages");
