using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using RabbitMQ.Client;

const int messageCount = 50000;

PublishMessagesIndividually();
PublishMessagesInBatch();
await HandlePublishConfirmsAsynchronously();

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

static async Task HandlePublishConfirmsAsynchronously()
{
    using var connection = CreateConnection();
    using var channel = connection.CreateModel();
    
    var queueName = channel.QueueDeclare().QueueName;
    channel.ConfirmSelect();
    
    var outstandingConfirms = new ConcurrentDictionary<ulong, string>();
    
    var startTime = Stopwatch.GetTimestamp();
    for (int i = 0; i < messageCount; i++)
    {
        var body = Encoding.UTF8.GetBytes(i.ToString());
        outstandingConfirms.TryAdd(channel.NextPublishSeqNo, i.ToString());
        channel.BasicPublish(string.Empty, queueName, null, body);
    }
    
    //2 callbacks: one for confirmed messages and one for nack-ed messages (messages that can be considered lost by the broker)
    channel.BasicAcks += (_, args) =>  CleanOutstandingConfirms(args.DeliveryTag, args.Multiple); 
    
    channel.BasicNacks += (_, args) =>
    {
        outstandingConfirms.TryGetValue(args.DeliveryTag, out var body);
        Console.WriteLine($"Message with body {body} has been nack-ed. Sequence number: {args.DeliveryTag}, multiple: {args.Multiple}");
        CleanOutstandingConfirms(args.DeliveryTag, args.Multiple);
    };
    
    if (!await WaitUntil(60, () => outstandingConfirms.IsEmpty))
        throw new Exception("All messages could not be confirmed in 60 seconds");
    
    var endTime = Stopwatch.GetTimestamp();
    Console.WriteLine($"Published {messageCount:N0} messages asynchronously in {Stopwatch.GetElapsedTime(startTime, endTime).TotalMilliseconds:N0} ms");
    
    
    //持續檢查 condition 是否為真，並在每次檢查之間等待 100 豪秒。如果在指定的時間內條件變為真，則方法返回 true；如果時間到期而條件仍未變為真，則返回 false。
    async Task<bool> WaitUntil(int seconds, Func<bool> condition)
    {
        int waited = 0;
        while(!condition() && waited < seconds * 1000)
        {
            await Task.Delay(TimeSpan.FromMilliseconds(100));
            waited += 100;
        }
        
        return condition();
    }
    
    void CleanOutstandingConfirms(ulong deliveryTag, bool multiple)
    {
        if (multiple)  //If false, only one message is confirmed/nack-ed, if true, all messages with a lower or equal sequence number are confirmed/nack-ed
        {
            var confirmedSeqNos = outstandingConfirms.Keys.Where(seqNo => seqNo <= deliveryTag);
            foreach (var seqNo in confirmedSeqNos)
            {
                outstandingConfirms.TryRemove(seqNo, out _);
            }
        }
        else
        {
            outstandingConfirms.TryRemove(deliveryTag, out _);
        }
    }
}



static IConnection CreateConnection()
{
    var factory = new ConnectionFactory
    {
        HostName = "localhost",
    };

    return factory.CreateConnection();
}

