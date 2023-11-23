namespace OCRBilletParse.Common.Model;
public class BillParseModel
{
    public string Index { get; set; }
    public string Name { get; set; }
    public string Unit { get; set; }
    public decimal Price { get; set; }
    public decimal TotalPrice { get; set; }
    public decimal Discount { get; set; }
}
