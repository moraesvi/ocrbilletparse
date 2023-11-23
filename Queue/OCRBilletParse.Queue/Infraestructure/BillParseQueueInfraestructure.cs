using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Queue.Factory;
using OCRBilletParse.Queue.Interface;
using RabbitMQ.Client;

namespace OCRBilletParse.Queue.Infraestructure;
public class BillParseQueueInfraestructure : IBillParseQueueInfraestructure
{
    private ILogger<BillParseQueueInfraestructure> Logger { get; }
    private IConfiguration Config { get; }
    private IConnection Conn { get; }
    private IModel Channel { get; set; }
    private RabbitMqConnectionFactory ConnectionFactory { get; }
    public BillParseQueueInfraestructure(ILogger<BillParseQueueInfraestructure> logger, IConfiguration config, RabbitMqConnectionFactory connectionFactory)
    {
        Logger = logger;
        Config = config;
        ConnectionFactory = connectionFactory;
        //Channel = ConnectionFactory.CreateQueue();
    }
    public void SendToQueue(byte[] message)
    {
        try
        {
            if (Channel == null || Channel.IsClosed)
                Channel = ConnectionFactory.CreateQueue();

            Channel.BasicPublish(exchange: "",
                                  routingKey: ConnectionFactory.QueueName,
                                  basicProperties: null,
                                  body: message);

            Logger.LogInformation("[msg sent to queue[BillParse]");
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred sending a message to queue[BillParse]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
}
