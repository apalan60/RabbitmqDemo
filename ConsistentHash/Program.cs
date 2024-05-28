using RabbitMQ.Client;

var factory = new ConnectionFactory
{
    HostName = "127.0.0.1"
};

using var connection = factory.CreateConnection();
using var channel = connection.CreateModel();
