using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var factory = new ConnectionFactory { HostName = "localhost" };
using var connection = factory.CreateConnection();
var channel = connection.CreateModel();

channel.QueueDeclare(queue: "rpc_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);

//spread the load equally over multiple servers
channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

//此處為RPC訊息傳遞方向與Worker不同的地方，Worker Queue模式中，訊息是單向的，從生產者到消費者；RPC模式中，訊息是雙向的，客戶端發送訊息，請Server做回應。
//Use BasicConsume to access the queue. Then we register a delivery handler in which we do the work and send the response back.
const string queueName = "rpc_queue";

var consumer = new EventingBasicConsumer(channel);
consumer.Received += (_, ea) =>
{
    string response = string.Empty;
    
    try
    {
        var message = Encoding.UTF8.GetString(ea.Body.ToArray());
        int n = int.Parse(message);
        Console.WriteLine($" [.] Fib({message})");
        response = Fib(n).ToString();
    }
    catch (Exception e)
    {
        Console.WriteLine($" [.] {e.Message}");
        response = string.Empty;
    }
    finally
    {
        var responseBytes = Encoding.UTF8.GetBytes(response);
        var props = ea.BasicProperties;
        var replyProps = channel.CreateBasicProperties();
        replyProps.CorrelationId = props.CorrelationId;
            
        channel.BasicPublish(exchange: string.Empty, routingKey: props.ReplyTo, basicProperties: replyProps, body: responseBytes);
        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
    }
};
channel.BasicConsume(queue:queueName, autoAck: false, consumer: consumer);

Console.WriteLine(" [x] Awaiting RPC requests");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();



static int Fib(int n)
{
    if (n is 0 or 1)
    {
        return n;
    }

    return Fib(n - 1) + Fib(n - 2);
}
