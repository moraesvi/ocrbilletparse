using OCRBilletParse.Common;
using OCRBilletParse.Common.Model;

namespace OCRBilletParse.Interface;
public interface IReceiptTransformLogic
{
    Task<string> Send(ImageParam imageParam);
    Task<GenericKeyValueItem<BillTotalParseModel>> Check(string transactionId);
}
