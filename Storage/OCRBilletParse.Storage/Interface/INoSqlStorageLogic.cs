using OCRBilletParse.Common;

namespace OCRBilletParse.Storage.Interface;
public interface INoSqlStorageLogic
{
    bool Exists(string key);
    KeyValueItem Get(string key);
    void Save(KeyValueItem redisItemDb);
    void Remove(string key);
}
