using OCRBilletParse.Common.Model;

namespace OCRBilletParse.Interface;
public interface IReceiptParserLogic
{
    Task<BillTotalParseModel> Parse(ImageParam imageParam);
}
