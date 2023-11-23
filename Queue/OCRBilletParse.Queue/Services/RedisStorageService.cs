using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Common;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Queue.Interface;
using OCRBilletParse.Services;
using System.Net.Http.Json;
using System.Text.Json;

namespace OCRBilletParse.Queue.Services;
public class RedisStorageService : INoSqlStorageService
{
    private ILogger<ReceiptQueueService> Logger { get; }
    private IConfiguration Config { get; }
    private HttpClient ServiceHttpClient { get; }
    public RedisStorageService(ILogger<ReceiptQueueService> logger, IConfiguration config, HttpClient serviceHttpClient)
    {
        Logger = logger;
        Config = config;
        ServiceHttpClient = serviceHttpClient;
    }
    public async Task<KeyValueItem> Get(string key)
    {
        try
        {
            Logger.LogInformation("Requested a new item to service[storage]");
            var response = await ServiceHttpClient.GetAsync($"{ServiceHttpClient.BaseAddress}/{key}");
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var redisItemDb = await JsonSerializer.DeserializeAsync<KeyValueItem>(await response.Content.ReadAsStreamAsync(), new JsonSerializerOptions()
                {
                    DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });
                return redisItemDb;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred sending item to service[storage]");
                Logger.LogError(error);

                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred sending item to service[storage]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
    public async Task<bool> Check(string key)
    {
        try
        {
            Logger.LogInformation("Requested a new item to service[storage]");
            var response = await ServiceHttpClient.GetAsync($"{ServiceHttpClient.BaseAddress}/{key}/check");
            response.EnsureSuccessStatusCode();

            if (response.IsSuccessStatusCode)
            {
                var itemExists = Convert.ToBoolean(await response.Content.ReadAsStringAsync());
                return itemExists;
            }
            else
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred sending item to service[storage]");
                Logger.LogError(error);

                return false;
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred sending item to service[storage]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
    public async Task Save(string key, string data)
    {
        KeyValueItem redisItemDb = new KeyValueItem();
        redisItemDb.Key = key;
        redisItemDb.Value = data;

        try
        {
            Logger.LogInformation("Requested a new item to service[storage]");
            var response = await ServiceHttpClient.PostAsJsonAsync(ServiceHttpClient.BaseAddress, redisItemDb, new JsonSerializerOptions()
            {
                DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            });
            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred sending item to service[storage]");
                Logger.LogError(error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred sending item to service[storage]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
    public async Task Delete(string key)
    {
        try
        {
            Logger.LogInformation("Requested a new item to service[storage]");
            var response = await ServiceHttpClient.DeleteAsync($"{ServiceHttpClient.BaseAddress}/{key}");
            response.EnsureSuccessStatusCode();

            if (!response.IsSuccessStatusCode)
            {
                string error = await response.Content.ReadAsStringAsync();
                Logger.LogError($"An error has ocorred sending item to service[storage]");
                Logger.LogError(error);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"An exception has ocorred sending item to service[storage]");
            Logger.LogError(ex.Message);
            Logger.LogError(ex.StackTrace);

            throw;
        }
    }
}
