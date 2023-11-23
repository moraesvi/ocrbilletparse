using OCRBilletParse.Common;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Queue.Helper;
using OCRBilletParse.Queue.Interface;
using System.Text;
using System.Text.Json;

namespace OCRBilletParse.Queue.Logic;
public class BillParseQueueLogic : IBillParseQueueLogic
{
    private IBillParseQueueInfraestructure BillParseQueueInfraestructure { get; }
    private INoSqlStorageService NoSqlStorageService { get; }
    public BillParseQueueLogic(IBillParseQueueInfraestructure billParseQueueInfraestructure, INoSqlStorageService noSqlStorageService)
    {
        BillParseQueueInfraestructure = billParseQueueInfraestructure;
        NoSqlStorageService = noSqlStorageService;
    }
    public async Task<ResultKeyValueItem<KeyValueItem>> CheckItemWasProcessed(string transactionId)
    {
        var itemProcessed = await NoSqlStorageService.Get(transactionId);
        if (itemProcessed != null  && !string.IsNullOrEmpty(itemProcessed.Value))
            return new ResultKeyValueItem<KeyValueItem>() { HasItem = true,  Value = itemProcessed };
        return new ResultKeyValueItem<KeyValueItem>() { HasItem = false };
    }
    public async Task SaveNoSqlDatabase(string key, string data)
    {
        await NoSqlStorageService.Save(key, data);
    }
    public string SendToQueue(ImageParam imageParam)
    {
        string transactionId = QueueHelper.GenerateId();

        QueueKeyValueItem<ImageParam> keyValueItem = new QueueKeyValueItem<ImageParam>();
        keyValueItem.Key = transactionId;
        keyValueItem.Value = imageParam;

        BillParseQueueInfraestructure.SendToQueue(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(keyValueItem, new JsonSerializerOptions()
        {
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        })));

        return transactionId;
    }
}
