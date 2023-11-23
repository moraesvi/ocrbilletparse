using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Queue.Interface;
using System.Net.Http.Json;
using System.Text.Json;

namespace OCRBilletParse.Queue.Services;
public class ReceiptParseService : IReceiptParseService
{
    private ILogger<ReceiptParseService> Logger { get; }
    private IConfiguration Config { get; }
    private HttpClient ServiceHttpClient { get; }
    public ReceiptParseService(ILogger<ReceiptParseService> logger, IConfiguration config, HttpClient serviceHttpClient)
    {
        Logger = logger;
        Config = config;
        ServiceHttpClient = serviceHttpClient;
    }
    public async Task<BillTotalParseModel> Parse(ImageParam imageParam)
    {
        try
        {
            Logger.LogInformation("Requested a new item to service[queue]");
            var response = await ServiceHttpClient.PostAsJsonAsync($"{ServiceHttpClient.BaseAddress}/parse", imageParam, new JsonSerializerOptions()
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });

            if (response.IsSuccessStatusCode)
            {
                var billTotal = await JsonSerializer.DeserializeAsync<BillTotalParseModel>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions()
                {
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                return billTotal;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred sending item to service[queue]");
                Logger.LogError(error);

                return null;
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
}
