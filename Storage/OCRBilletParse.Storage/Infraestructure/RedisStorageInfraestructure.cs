using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Common;
using OCRBilletParse.Storage.Interface;
using ServiceStack.Redis;
using System.Text;
using System.Text.Json;

namespace OCRBilletParse.Storage.Infraestructure;
public class RedisStorageInfraestructure : INoSqlStorageInfraestructure
{
    private ILogger<RedisStorageInfraestructure> Logger { get; }
    private IConfiguration Config { get; }
    private RedisClient RedisClient { get; }
    public RedisStorageInfraestructure(ILogger<RedisStorageInfraestructure> logger, IConfiguration config)
    {
        Logger = logger;
        Config = config;
        RedisClient = new RedisClient(config["Services:redis:ConnectionString"]);
    }
    public ItemDb Get(string key)
    {
        Logger.LogInformation("Getting redis data");
        var result = RedisClient.Get(key);
        if (result == null)
            return null;

        return JsonSerializer.Deserialize<ItemDb>(Encoding.UTF8.GetString(result), new JsonSerializerOptions()
        {
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        });
    }
    public void Save(ItemDb redisItemDb)
    {
        Logger.LogInformation("Saving redis data");
        RedisClient.Set(redisItemDb.Key, redisItemDb);
    }
    public void Remove(string key)
    {
        Logger.LogInformation("Removing redis data");
        RedisClient.Remove(key);
    }
}
