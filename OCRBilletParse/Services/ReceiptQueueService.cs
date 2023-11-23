using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Common;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace OCRBilletParse.Services;
public class ReceiptQueueService : IReceiptTransformLogic
{
    private ILogger<ReceiptQueueService> Logger { get; }
    private IConfiguration Config { get; }
    private HttpClient ServiceHttpClient { get; }
    public ReceiptQueueService(ILogger<ReceiptQueueService> logger, IConfiguration config, HttpClient serviceHttpClient)
    {
        Logger = logger;
        Config = config;
        ServiceHttpClient = serviceHttpClient;
    }
    public async Task<string> Send(ImageParam imageParam)
    {
        try
        {
            Logger.LogInformation("Requested a new item to service[queue]");
            var response = await ServiceHttpClient.PostAsJsonAsync($"{Config["Services:QueueApi:Uri"]}/send", imageParam, new JsonSerializerOptions()
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (response.IsSuccessStatusCode)
            {
                string transactionId = await response.Content.ReadAsStringAsync();
                return transactionId;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred sending item to service[queue]");
                Logger.LogError(error);

                return string.Empty;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred sending item to service[queue]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
    public async Task<GenericKeyValueItem<BillTotalParseModel>> Check(string transactionId)
    {
        try
        {
            Logger.LogInformation("Requested the item from service[queue]");
            var response = await ServiceHttpClient.GetAsync($"{Config["Services:QueueApi:Uri"]}/{transactionId}/check");

            if (response.IsSuccessStatusCode)
            {
                ResultKeyValueItem<KeyValueItem> item = await JsonSerializer.DeserializeAsync<ResultKeyValueItem<KeyValueItem>>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions()
                {
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (item.HasItem)
                {
                    var billTotal = JsonSerializer.Deserialize<BillTotalParseModel>(item.Value.Value, new JsonSerializerOptions()
                    {
                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });
                    return new GenericKeyValueItem<BillTotalParseModel>() 
                    {
                        Key = item.Value.Key,
                        Value = billTotal
                    };
                }

                return new GenericKeyValueItem<BillTotalParseModel>();
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred getting item from service[queue]");
                Logger.LogError(error);

                return new GenericKeyValueItem<BillTotalParseModel>();
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred getting item from service[queue]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
}
