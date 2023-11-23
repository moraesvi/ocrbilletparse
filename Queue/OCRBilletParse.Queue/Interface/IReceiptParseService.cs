using OCRBilletParse.Common.Model;

namespace OCRBilletParse.Queue.Interface;
public interface IReceiptParseService
{
    Task<BillTotalParseModel> Parse(ImageParam imageParam);
}
