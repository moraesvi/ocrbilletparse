using OCRBilletParse.Common.Model;

namespace OCRBilletParse.Interface
{
    public interface IBrazilianImageRecognitionParseService
    {
        BillTotalParseModel Parse(string imageText);
    }
}
