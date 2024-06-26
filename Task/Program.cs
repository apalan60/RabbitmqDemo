﻿using System.Text;
using RabbitMQ.Client;

//Create connection to rabbitmq server
//If we wanted to connect to a node on a different machine we'd simply specify its hostname or IP address here.
var factory = new ConnectionFactory
{
    HostName = "localhost"
    // Uri = new Uri("amqp://guest:guest@localhost:5672"),
    // ClientProvidedName = "Rabbitmq Demo app server"  
};
using var connection = factory.CreateConnection();

//create a channel, which is where most of the API for getting things done resides.
using var channel = connection.CreateModel();     

//Publish message to queue
channel.QueueDeclare(
    queue: "queue.Task.Publisher1", 
    durable: true,
    exclusive: false,
    autoDelete: false,
    arguments: null);

{
    channel.BasicQos(0, 1, false);
}

//Get message from command line arguments
var message = GetMessage(args);

var body = Encoding.UTF8.GetBytes(message);

var properties = channel.CreateBasicProperties();
properties.Persistent = false;

channel.BasicPublish(
    exchange: string.Empty, //default exchange
    routingKey: "queue.Task.Publisher1",
    basicProperties: properties,
    body: body);

Console.WriteLine($"[x] Sent {message} to queue");
Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();





static string GetMessage(string[] args)
{
    return args.Length > 0 ? string.Join(" ", args) : "Hello World!";
}