using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

const int messageCount = 50000;

static void PublishMessagesIndividually()
{
    using var connection = CreateConnection();
    using var channel = connection.CreateModel();
    
    var queueName = channel.QueueDeclare().QueueName;
    channel.ConfirmSelect(); //publisher acknowledgements

    var startTime = Stopwatch.GetTimestamp();
    
    for (int i = 0; i < messageCount; i++)
    {
        var body = Encoding.UTF8.GetBytes(i.ToString());
        channel.BasicPublish("", queueName, null, body);
        channel.WaitForConfirmsOrDie();
    }
    
    var endTime = Stopwatch.GetTimestamp();
    Console.WriteLine($"Published {messageCount:N0} messages individually in {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0} ms");
}



static IConnection CreateConnection()
{
    var factory = new ConnectionFactory
    {
        HostName = "localhost",
    };

    return factory.CreateConnection();
}