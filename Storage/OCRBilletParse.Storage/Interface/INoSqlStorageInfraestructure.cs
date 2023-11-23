using OCRBilletParse.Common;

namespace OCRBilletParse.Storage.Interface;
public interface INoSqlStorageInfraestructure
{
    ItemDb Get(string key);
    void Save(ItemDb redisItemDb);
    void Remove(string key);
}
