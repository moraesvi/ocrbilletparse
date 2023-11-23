using OCRBilletParse.Common;

namespace OCRBilletParse.Queue.Interface;

public interface INoSqlStorageService
{
    Task<KeyValueItem> Get(string key);
    Task<bool> Check(string key);
    Task Save(string key, string data);
    Task Delete(string key);
}
