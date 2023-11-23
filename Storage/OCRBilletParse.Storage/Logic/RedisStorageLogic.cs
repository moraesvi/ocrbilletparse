using OCRBilletParse.Common;
using OCRBilletParse.Storage.Interface;

namespace OCRBilletParse.Storage.Logic;
public class RedisStorageLogic : INoSqlStorageLogic
{
    private INoSqlStorageInfraestructure NoSqlStorage { get; }
    public RedisStorageLogic(INoSqlStorageInfraestructure noSqlStorage)
    {
        NoSqlStorage = noSqlStorage;
    }
    public bool Exists(string key) => NoSqlStorage.Get(key) != null;
    public KeyValueItem Get(string key)
    {
        var item = NoSqlStorage.Get(key);
        if (item == null)
            return new KeyValueItem();

        return new KeyValueItem()
        {
            Key = item.Key,
            Value = item.Value,
        };
    }
    public void Save(KeyValueItem redisItem)
    {
        NoSqlStorage.Save(new ItemDb() 
        {
            Key = redisItem.Key,
            Value = redisItem.Value,
        });
    }
    public void Remove(string key)
    {
        NoSqlStorage.Remove(key);
    }
}
