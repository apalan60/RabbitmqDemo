using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

const int messageCount = 50000;

PublishMessagesIndividually();
PublishMessagesInBatch();

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

static void PublishMessagesInBatch()
{
    using var connection = CreateConnection();
    using var channel = connection.CreateModel();
    
    var queueName = channel.QueueDeclare().QueueName;
    channel.ConfirmSelect();
    
    var batchSize = 1000;
    var outstandingMessageCount = 0;
    
    var startTime = Stopwatch.GetTimestamp();
    for (int i = 0; i < messageCount; i++)
    {
        var body = Encoding.UTF8.GetBytes(i.ToString());
        channel.BasicPublish("", queueName, null, body);
        outstandingMessageCount++;
        
        if (outstandingMessageCount == batchSize)
        {
            channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5)); //Waits until all messages published on this channel since the last call have been ack by the broker. If a nack is received or the timeout
            outstandingMessageCount = 0;
        }
    }

    if (outstandingMessageCount > 0)
    {
        channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
    }
    
    var endTime = Stopwatch.GetTimestamp();
    
    Console.WriteLine($"Published {messageCount:N0} messages in batch of {batchSize} in {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0} ms");
}


static IConnection CreateConnection()
{
    var factory = new ConnectionFactory
    {
        HostName = "localhost",
    };

    return factory.CreateConnection();
}