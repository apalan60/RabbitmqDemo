using System.Collections.Concurrent;
using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RPCClient;

public class RpcClient : IDisposable
{
    private readonly IModel _channel;
    
    //TaskCompletionSource is a helper class that makes it easy to create a task that can be completed from the outside.
    private readonly ConcurrentDictionary<string, TaskCompletionSource<string>> _callbackMapper = new();
    private readonly string _replyQueueName;

    public RpcClient()
    {
        var factory = new ConnectionFactory { HostName = "localhost" };
        using var connection = factory.CreateConnection();
        _channel = connection.CreateModel();
        _replyQueueName = _channel.QueueDeclare().QueueName;
        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (_, ea) =>
        {
            // if the correlation id is not found, it means the response is not expected, so we ignore it.
            if(!_callbackMapper.TryRemove(ea.BasicProperties.CorrelationId, out var tcs)) 
                return;
            
            var body = ea.Body.ToArray();
            var response = Encoding.UTF8.GetString(body);
            
            // After we get the response, we set the result on the TaskCompletionSource. This will unblock the thread that is waiting for the response.
            tcs.TrySetResult(response);
        };
        
        _channel.BasicConsume(
            queue: _replyQueueName,
            autoAck: true, 
            consumer: consumer);
        
    }

    public Task<string> CallAsync(string message, CancellationToken cancellationToken = default)
    {
        IBasicProperties properties = _channel.CreateBasicProperties();
        
        var correlationId = Guid.NewGuid().ToString();
        properties.CorrelationId = correlationId;
        properties.ReplyTo = _replyQueueName;
        
        var taskCompletionSource = new TaskCompletionSource<string>();
        _callbackMapper.TryAdd(correlationId, taskCompletionSource);
        
        var messageBytes = Encoding.UTF8.GetBytes(message);
        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: "rpc_queue",
            basicProperties: properties,
            body: messageBytes);
        
        //註冊一個delegate，當這個 CancellationToken 被取消時，這個委派會被呼叫
        cancellationToken.Register(() => _callbackMapper.TryRemove(correlationId, out _));
        return taskCompletionSource.Task;
    }
    
    public void Dispose()
    {
        throw new NotImplementedException();
    }
}

public static class Rpc
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("RPC Client");
        string n = args.Length > 0 ? args[0] : "30";
        await InvokeAsync(n);
        
        Console.WriteLine(" Press [enter] to exit.");
        Console.ReadLine();
    }

    private static async Task InvokeAsync(string n)
    {
        using var rpcClient = new RpcClient();
        Console.WriteLine($" [x] Requesting fib({n})");
        
        var response = await rpcClient.CallAsync(n);
        Console.WriteLine($" [.] Got '{response}'");
    }
}