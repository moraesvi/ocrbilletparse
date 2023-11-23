namespace OCRBilletParse.Common.Model;
public class BillTotalParseModel
{
    public List<BillParseModel> BillParseModel { get; set; } = new List<BillParseModel>();
    public short TotalItems { get; set; }
    public string Currency { get; set; }
    public decimal Price { get; set; }
    public decimal Discount { get; set; }
    public decimal TotalPrice { get; set; }
}
