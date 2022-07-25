using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TaskApi.Models;
using TaskApi.Controllers;

public class TaskProcessorService : BackgroundService
{
    private readonly ILogger _logger;
    private IConnection _connection;
    private IModel _channel;
    private readonly TaskContext _context;
    private TaskController taskController;

    public TaskProcessorService(IServiceProvider services, ILoggerFactory loggerFactory)
    {
        Services = services;
        this._logger = loggerFactory.CreateLogger<TaskProcessorService>();
        InitRabbitMQ();
    }

    public IServiceProvider Services { get; }

    private void InitRabbitMQ()
    {
        var factory = new ConnectionFactory
        {

            // HostName = "localhost" , 
            // Port = 30724
            HostName = Environment.GetEnvironmentVariable("RABBITMQ_HOST"),
            Port = Convert.ToInt32(Environment.GetEnvironmentVariable("RABBITMQ_PORT"))

        };

        // create connection  
        _connection = factory.CreateConnection();

        // create channel  
        _channel = _connection.CreateModel();

        //_channel.ExchangeDeclare("demo.exchange", ExchangeType.Topic);
        _channel.QueueDeclare("tasks", false, false, false, null);
        _channel.QueueDeclare("task-processed", false, false, false, null);
        // _channel.QueueBind("demo.queue.log", "demo.exchange", "demo.queue.*", null);
        // _channel.BasicQos(0, 1, false);

        _connection.ConnectionShutdown += RabbitMQ_ConnectionShutdown;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.ThrowIfCancellationRequested();

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += (ch, ea) =>
        {
            // received message  
            var content = System.Text.Encoding.UTF8.GetString(ea.Body.ToArray());

            // handle the received message  
            HandleMessage(content);
            _channel.BasicAck(ea.DeliveryTag, false);
        };

        consumer.Shutdown += OnConsumerShutdown;
        consumer.Registered += OnConsumerRegistered;
        consumer.Unregistered += OnConsumerUnregistered;
        consumer.ConsumerCancelled += OnConsumerConsumerCancelled;

        _channel.BasicConsume("task-processed", false, consumer);

        return Task.CompletedTask;
    }

    private async Task HandleMessage(string content)
    {
        // we just print this message   
        //_logger.LogInformation($"consumer received {content}");
        //extract task...
        Console.WriteLine($"consumer received {content}");
        var taskUpdate = JsonSerializer.Deserialize<TaskUpdate>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (taskUpdate != null)
        {
            var mynum = taskUpdate.Id;
            if (mynum != null)
            {
                var t = await taskController.GetTaskItem(taskUpdate.Id);
                if (t != null)
                {
                    TaskItem currTask = t.Value;
                    var newTask = new TaskItem
                    {
                        Id = currTask.Id,
                        Description = currTask.Description,
                        Priority = currTask.Priority,
                        Status = taskUpdate.Status,
                        CustomerId = currTask.CustomerId
                    };
                    taskController.PutTaskItem(taskUpdate.Id, newTask);
                    currTask.Status = taskUpdate.Status;
                }
            }
        }
    }

    private void OnConsumerConsumerCancelled(object sender, ConsumerEventArgs e) { }
    private void OnConsumerUnregistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerRegistered(object sender, ConsumerEventArgs e) { }
    private void OnConsumerShutdown(object sender, ShutdownEventArgs e) { }
    private void RabbitMQ_ConnectionShutdown(object sender, ShutdownEventArgs e) { }

    public override void Dispose()
    {
        _channel.Close();
        _connection.Close();
        base.Dispose();
    }
}
