using OCRBilletParse.Common;
using OCRBilletParse.Common.Model;

namespace OCRBilletParse.Queue.Interface;
public interface IBillParseQueueLogic
{
    Task<ResultKeyValueItem<KeyValueItem>> CheckItemWasProcessed(string transactionId);
    string SendToQueue(ImageParam imageParam);
    Task SaveNoSqlDatabase(string key, string data);
}
