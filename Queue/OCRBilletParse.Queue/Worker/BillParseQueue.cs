using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Queue.Factory;
using OCRBilletParse.Queue.Interface;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace OCRBilletParse.Queue.Worker;
public class BillParseQueue : BackgroundService
{
    private const int MAX_CONCURRENT_THREADS = 10;
    private ILogger<BillParseQueue> Logger { get; }
    private IConfiguration Config { get; }
    private IBillParseQueueLogic BillParseQueueLogic { get; }
    private IReceiptParseService ReceiptParseService { get; }
    private RabbitMqConnectionFactory ConnectionFactory { get; }
    private IConnection Conn { get; }
    private IModel Channel { get; }
    private List<Task> _lstTaskQueue;
    public BillParseQueue(ILogger<BillParseQueue> logger, IConfiguration config, IBillParseQueueLogic billParseQueueLogic, IReceiptParseService receiptParseService, RabbitMqConnectionFactory connectionFactory)
    {
        Logger = logger;
        Config = config;
        BillParseQueueLogic = billParseQueueLogic;
        ReceiptParseService = receiptParseService;
        ConnectionFactory = connectionFactory;
        _lstTaskQueue = new List<Task>();
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            Logger.LogInformation("[waiting for messages...]");
            while (!stoppingToken.IsCancellationRequested)
            {
                if (ConnectionFactory.GetConnection()?.IsOpen ?? false)
                {
                    var consumer = new EventingBasicConsumer(Channel);
                    consumer.Received += Consumer_Received;

                    ConnectionFactory.GetConnection()
                                      .BasicConsume(queue: ConnectionFactory.QueueName,
                                                    autoAck: false,
                                                    consumer: consumer);
                }

                if (_lstTaskQueue.Count > 0)
                {
                    Task.WhenAll(_lstTaskQueue).GetAwaiter().GetResult();
                    _lstTaskQueue.Clear();
                }

                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
        catch (OperationCanceledException ex)
        {
            Logger.LogInformation("!!!OperationCancelled!!!  Start clean up from here");
            Logger.LogError(ex.StackTrace);
        }
        catch (Exception ex)
        {
            Logger.LogInformation($"Exception Caught: {ex.GetType().FullName}");
            Logger.LogError(ex.StackTrace);
        }
    }
    private void Consumer_Received(object sender, BasicDeliverEventArgs e)
    {
        Logger.LogInformation($"[new message receveid | {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] " + Encoding.UTF8.GetString(e.Body.ToArray()));

        try
        {
            var keyValueItem = JsonSerializer.Deserialize<QueueKeyValueItem<ImageParam>>(Encoding.UTF8.GetString(e.Body.ToArray()), new JsonSerializerOptions()
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            Task tskQueue = Task.Run(async () =>
            {
                try
                {
                    ConnectionFactory.GetConnection().BasicAck(e.DeliveryTag, multiple: false);

                    var receiptData = await ReceiptParseService.Parse(keyValueItem.Value);
                    await BillParseQueueLogic.SaveNoSqlDatabase(keyValueItem.Key, JsonSerializer.Serialize(receiptData, new JsonSerializerOptions()
                    {
                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    }));
                }
                catch (Exception)
                {
                    ConnectionFactory.GetConnection().BasicNack(e.DeliveryTag, multiple: false, requeue: false);
                }
            });

            _lstTaskQueue.Add(tskQueue);

            if (_lstTaskQueue.Count % MAX_CONCURRENT_THREADS == 0)
            {
                Task.WhenAll(_lstTaskQueue).GetAwaiter().GetResult();
                Thread.Sleep(TimeSpan.FromSeconds(1));
                _lstTaskQueue.Clear();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError(ex.Message);
        }
    }
}
