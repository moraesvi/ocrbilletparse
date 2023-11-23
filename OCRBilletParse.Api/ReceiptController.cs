using ImageMagick;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Caching.Memory;
using OCRBilletParse.Common;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Interface;
using System.Text;
using System.Text.Json.Serialization;

namespace OCRBilletParse
{
    [Route("[controller]")]
    [ApiController]
    public class ReceiptController : ControllerBase
    {
        private string _text;
        private ILogger<ReceiptController> Logger { get; }
        private IMemoryCache MemoryCache { get; }
        private IReceiptTransformLogic ReceiptTransformLogic { get; }
        private IReceiptParserLogic ReceiptParserLogic { get; }

        static string key = "351c561a71d842db9e4ef12ce07a78f5";
        static string endpoint = "https://ocrbilletparse.cognitiveservices.azure.com/";
        public ReceiptController(ILogger<ReceiptController> logger, IMemoryCache cache, IReceiptTransformLogic receiptTransformLogic, IReceiptParserLogic receiptParserLogic)
        {
            Logger = logger;
            MemoryCache = cache;
            ReceiptTransformLogic = receiptTransformLogic;
            ReceiptParserLogic = receiptParserLogic;
        }
        [HttpGet]
        [Route("healthcheck")]
        public async Task<ActionResult> Healhcheck() => Ok(await Task.Run(() => $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}"));
        [HttpGet]
        [Route("getlastestparse")]
        public async Task<ActionResult> GetLastestParse() => await Task.Run(() => Ok(MemoryCache.Get("teste")));
        [HttpGet]
        [Route("{transactionId}/checktransaction")]
        public async Task<ActionResult> CheckTransaction(string transactionId)
        {
            var parsedItem = await ReceiptTransformLogic.Check(transactionId);
            MemoryCache.Set("teste", parsedItem);

            return Ok(parsedItem);
        }
        public BillTotalParseModel Parse(string text)
        {
            if (string.IsNullOrEmpty(text))
                throw new InvalidOperationException();
            var billTotalParse = new BillTotalParseModel();

            var lstUntilNumberValue = new List<BillUntilValue>();
            var hashLinesProcessed = new HashSet<string>();

            var lstLine = text.Split('\n')
                              ?.Where(t => !t.Contains("--") && !(t.Length == 1 && t.FirstOrDefault() == '-') && !t.Contains(".."))
                              ?.Select(t => t.Trim())
                              ?.ToList();

            int countWordsBeforeTotalValue = 0;
            int countWordsBeforeValueWithDiscount = 0;

            int indexBillValue = 0;
            int indexToStart = 0;
            int indexTotalTextFound = 0;

            bool nextLineFinishes = false;
            bool lastNextValueIsNumber = false;
            bool serializeNextNumber = false;
            bool indexFound = false;
            bool foundTotalText = false;
            bool foundValueWithDiscountText = false;
            bool nextLineHasText = false;
            bool nextLineHasDiscountValue = false;
            bool previousLineHadText = false;

            while (!indexFound)
            {
                indexToStart = lstLine.ToList().FindIndex(line => line.Split(' ').Any(word => string.Equals(word, "cod", StringComparison.OrdinalIgnoreCase) || string.Equals(word, "codigo", StringComparison.OrdinalIgnoreCase)
                                                                                              || string.Equals(word, "qtd", StringComparison.OrdinalIgnoreCase)));
                if (indexToStart < 0)
                {
                    indexToStart = lstLine.ToList().FindIndex(line => line.Split('|').Any(word => string.Equals(word, "cod", StringComparison.OrdinalIgnoreCase) || string.Equals(word, "codigo", StringComparison.OrdinalIgnoreCase)
                                                                                                  || string.Equals(word, "qtd", StringComparison.OrdinalIgnoreCase)));
                    if (indexToStart < 0)
                    {
                        indexToStart = lstLine.FindIndex(ln => ln.IndexOf("qtd", StringComparison.OrdinalIgnoreCase) >= 0 || ln.IndexOf("uni", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (indexToStart < 0)
                        {
                            indexToStart = lstLine.FindIndex(ln => (ln.StartsWith("1") || ln.StartsWith("01") || ln.StartsWith("001")) && !GetValueNumber(ln.Split(' ')).IsNumber() && !ln.Split(' ').LastOrDefault().OnlyNumbers().IsNumber());
                            if (indexToStart < 0)
                            {
                                indexToStart = lstLine.FindIndex(ln => ln.Contains("=="));
                            }
                            if (indexToStart > 0)
                            {
                                indexFound = true;
                                break;
                            }
                        }
                    }
                }

                if (indexToStart > 0)
                {
                    if (lstLine[indexToStart + 1].Contains("--"))
                    {
                        int lastIndexToStart = lstLine.ToList().FindLastIndex(line => line.Split(' ').Any(word => word.Contains("--")));
                        if (lastIndexToStart > 0)
                        {
                            indexToStart = lastIndexToStart;
                        }
                    }
                    else if (lstLine.Exists(word => word.Contains("==")))
                    {
                        int lastIndexToStart = lstLine.ToList().FindLastIndex(word => word.Contains("=="));
                        if (lastIndexToStart > 0)
                        {
                            indexToStart = lastIndexToStart;
                        }
                    }
                    indexToStart += 1;
                    indexFound = true;
                }
            }

            //if (indexToStart == -1)
            //    throw new InvalidOperationException();

            lstLine = lstLine?.Skip(indexToStart)
                             ?.ToList() ?? new List<string>();

            int totalValueLastIndex = lstLine.FindLastIndex(l =>
            {
                if (l.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("tota", StringComparison.OrdinalIgnoreCase) >= 0 || l.IndexOf("tot", StringComparison.OrdinalIgnoreCase) >= 0)
                    if (!(l.IndexOf("vl", StringComparison.OrdinalIgnoreCase) >= 0) && !(l.IndexOf("itens", StringComparison.OrdinalIgnoreCase) >= 0) && !(l.IndexOf("iten", StringComparison.OrdinalIgnoreCase) >= 0) && !(l.IndexOf("item", StringComparison.OrdinalIgnoreCase) >= 0)
                        && !(l.IndexOf("subtotal", StringComparison.OrdinalIgnoreCase) >= 0) && !(l.IndexOf("federal", StringComparison.OrdinalIgnoreCase) >= 0))
                        return true;
                return false;
            });

            for (int index = 0; index < lstLine.Count; index++)
            {
                var billParseModel = new BillParseModel();
                var lineValues = lstLine[index].Split(' ');
                string line = lstLine[index];

                if (lstUntilNumberValue.Count > 0)
                {
                    string lineValue = GetValueNumber(line.Split(' '));
                    if (lineValue.IsNumber() && !lineValue.HasParentheses() && lineValue.HasDecimalPlaces())
                    {
                        int nextNumber = 1;
                        foreach (var item in lstUntilNumberValue.ToList())
                        {
                            lineValue = lstLine[item.CountUntil + nextNumber];
                            lineValue = GetValueNumber(lineValue.Split(' '));
                            if (!lineValue.IsNumber() && !item.IsDiscount)
                                break;

                            decimal priceVal = ConvertHelper.ToDecimal(lineValue);
                            var billRegToUpdate = billTotalParse.BillParseModel.Where(b => b.Index == $"{item.IndexBillValue}".PadLeft(2, '0'))
                                                                               .FirstOrDefault();
                            if (billRegToUpdate == null)
                                continue;

                            if (!item.IsDiscount && billRegToUpdate.Price == 0)
                            {
                                billRegToUpdate.Price = priceVal;
                                billRegToUpdate.TotalPrice = priceVal;
                            }
                            else if (item.IsDiscount)
                            {
                                if (priceVal > 0)
                                {
                                    billRegToUpdate.Discount = priceVal;
                                    billRegToUpdate.Discount *= billRegToUpdate.Discount < 0 ? -1 : 1;
                                }
                                else
                                {
                                    lstUntilNumberValue.Remove(item);
                                }
                            }

                            nextNumber++;
                        }
                    }
                    else
                    {
                        foreach (var item in lstUntilNumberValue)
                        {
                            item.CountUntil += 1;
                        }
                    }
                }

                if (foundTotalText && !nextLineFinishes)
                {
                    if (line.IndexOf("a pagar", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        foundValueWithDiscountText = true;
                        countWordsBeforeValueWithDiscount++;
                    }

                    if (line.HasChar() || line.Contains("-") || line.Contains(".."))
                    {
                        countWordsBeforeTotalValue++;
                        continue;
                    }

                    if ((index + countWordsBeforeTotalValue) > lstLine.Count)
                    {
                        countWordsBeforeTotalValue -= indexTotalTextFound;
                    }

                    lineValues = lstLine[index + countWordsBeforeTotalValue].Split(' ');

                    while (string.IsNullOrEmpty(lineValues.FirstOrDefault()))
                    {
                        countWordsBeforeTotalValue -= 1;
                        lineValues = lstLine[index + countWordsBeforeTotalValue].Split(' ');
                    }

                    int lineNumberIndex = index;
                    while (string.Join(" ", lineValues).HasChar())
                    {
                        if (lineNumberIndex == lstLine.Count - 1)
                            break;

                        string lnVal = lstLine[lineNumberIndex];
                        if (!lnVal.IsNumber())
                        {
                            lineValues = lstLine[lineNumberIndex + 1].Split(' ');
                            continue;
                        }
                        lineNumberIndex--;
                    }

                    countWordsBeforeValueWithDiscount = countWordsBeforeTotalValue - countWordsBeforeValueWithDiscount;

                    billTotalParse.TotalItems = (short)billTotalParse.BillParseModel.Count;
                    billTotalParse.Price = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                    billTotalParse.TotalPrice = billTotalParse.Price;

                    decimal itemsPrice = billTotalParse.BillParseModel.Sum(b => b.Price);
                    billTotalParse.BillParseModel.ForEach(val => val.TotalPrice -= val.Discount);
                    decimal itemsTotalPrice = billTotalParse.BillParseModel.Sum(b => b.TotalPrice);

                    if ((itemsPrice == 0 || itemsTotalPrice == 0) && billTotalParse.TotalItems > 0)
                    {
                        int nextNumber = 0;
                        foreach (var item in lstUntilNumberValue)
                        {
                            bool foundValidNumber = false;
                            int foundNumber = 1;
                            nextNumber++;
                            while (!foundValidNumber)
                            {
                                string value = lstLine[item.CountUntil + nextNumber];
                                if (!value.IsNumber())
                                {
                                    nextNumber++;
                                    continue;
                                }
                                if (foundNumber == item.IndexBillValue)
                                {
                                    lineValues = lstLine[item.CountUntil + nextNumber].Split(' ');

                                    decimal priceV = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                                    var billRegToUpd = billTotalParse.BillParseModel.Where(b => b.Index == $"{item.IndexBillValue}".PadLeft(2, '0'))
                                                                                    .FirstOrDefault();
                                    if (!item.IsDiscount)
                                    {
                                        billRegToUpd.Price = priceV;
                                        billRegToUpd.TotalPrice = priceV;
                                    }
                                    else
                                    {
                                        billRegToUpd.Discount = priceV;
                                        billRegToUpd.Discount *= billRegToUpd.Discount < 0 ? -1 : 1;
                                    }

                                    foundValidNumber = true;
                                }
                                foundNumber++;
                            }
                        }

                        itemsPrice = billTotalParse.BillParseModel.Sum(b => b.Price);
                        billTotalParse.BillParseModel.ForEach(val => val.TotalPrice -= val.Discount);
                        itemsTotalPrice = billTotalParse.BillParseModel.Sum(b => b.TotalPrice);
                    }

                    if ((billTotalParse.Price != itemsPrice || billTotalParse.BillParseModel.Any(item => item.Discount > 0)) && billTotalParse.TotalItems > 0)
                    {
                        billTotalParse.Discount = itemsPrice - itemsTotalPrice;
                        billTotalParse.Price = itemsPrice;
                        billTotalParse.TotalPrice = itemsTotalPrice;
                    }

                    foundTotalText = false;
                    nextLineFinishes = billTotalParse.Discount > 0 || !lstLine.Exists(ln => ln.IndexOf("a pagar", StringComparison.OrdinalIgnoreCase) >= 0);
                    countWordsBeforeTotalValue = 0;

                    continue;
                }

                if (foundValueWithDiscountText)
                {
                    lineValues = lstLine[index + countWordsBeforeValueWithDiscount].Split(' ');

                    if (!string.Join(" ", lineValues).HasChar())
                        billTotalParse.TotalPrice = ConvertHelper.ToDecimal(lineValues.LastOrDefault().OnlyNumbers());

                    billTotalParse.Discount = billTotalParse.Price - billTotalParse.TotalPrice;

                    foundValueWithDiscountText = false;
                    nextLineFinishes = true;
                    continue;
                }

                if (line.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("tota", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("tot", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("deb", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("debi", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("valor a pagar", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (!(line.IndexOf("vl", StringComparison.OrdinalIgnoreCase) >= 0) && !(line.IndexOf("itens", StringComparison.OrdinalIgnoreCase) >= 0) && !(line.IndexOf("iten", StringComparison.OrdinalIgnoreCase) >= 0) && !(line.IndexOf("item", StringComparison.OrdinalIgnoreCase) >= 0) && !(line.IndexOf("sub", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        if (totalValueLastIndex > 0 && totalValueLastIndex == index)
                        {
                            foundTotalText = true;
                            indexTotalTextFound = index;
                            continue;
                        }
                        else if (totalValueLastIndex <= 0)
                        {
                            foundTotalText = true;
                            indexTotalTextFound = index;
                            continue;
                        }
                    }
                }

                if ((line.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("tota", StringComparison.OrdinalIgnoreCase) >= 0
                     || line.IndexOf("tot", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("deb", StringComparison.OrdinalIgnoreCase) >= 0
                     || line.IndexOf("debi", StringComparison.OrdinalIgnoreCase) >= 0) && lineValues.LastOrDefault().IsNumber())
                {
                    billTotalParse.TotalItems = (short)billTotalParse.BillParseModel.Count;
                    billTotalParse.Price = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                    billTotalParse.TotalPrice = billTotalParse.Price;

                    if (billTotalParse.TotalItems == 1 && billTotalParse.TotalPrice > 0)
                    {
                        var billParse = billTotalParse.BillParseModel.FirstOrDefault();
                        billParse.Price = billTotalParse.TotalPrice;
                        billParse.TotalPrice = billTotalParse.TotalPrice;
                    }

                    nextLineFinishes = !lstLine.Exists(ln => ln.IndexOf("a pagar", StringComparison.OrdinalIgnoreCase) >= 0);
                    continue;
                }

                if ((line.IndexOf("desc", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("descon", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("desconto", StringComparison.OrdinalIgnoreCase) >= 0) && lineValues.LastOrDefault().IsNumber() && !lineValues.GetArrayValue(lineValues.Count() - 2).OnlyNumbers().IsNumber()
                    && nextLineFinishes)
                {
                    billTotalParse.Discount = ConvertHelper.ToDecimal(lineValues.LastOrDefault().OnlyNumbers());
                    billTotalParse.TotalPrice = ConvertHelper.ToDecimal(billTotalParse.Price) - ConvertHelper.ToDecimal(billTotalParse.Discount);

                    break;
                }

                if (serializeNextNumber)
                {
                    var nextLine = lstLine[index + 1].Split(' ');
                    var billRegToUpdate = billTotalParse.BillParseModel.Where(b => b.Index == $"{indexBillValue}".PadLeft(2, '0'))
                                                                       .FirstOrDefault();
                    if (billRegToUpdate != null)
                    {
                        string nextPrice = GetValueNumber(nextLine);
                        billRegToUpdate.Price = ConvertHelper.ToDecimal(nextPrice);
                        billRegToUpdate.TotalPrice = ConvertHelper.ToDecimal(nextPrice);
                    }

                    serializeNextNumber = false;
                }

                if (lastNextValueIsNumber)
                {
                    string nextLine = lstLine[index + 1];
                    var nextLineValues = nextLine.Split(' ');
                    var lastValue = nextLine.Split(' ').LastOrDefault();

                    if ((!nextLine.HasChar() && nextLine.OnlyNumbers().IsNumber() || (nextLine.IndexOf("kg", StringComparison.OrdinalIgnoreCase) >= 0 || nextLine.IndexOf("un", StringComparison.OrdinalIgnoreCase) >= 0) && nextLine.CountChars() == 2) && !lineValues.Contains("()"))
                    {
                        if (ConvertHelper.ToDecimal(nextLine) < 0)
                        {
                            string lineVal = string.Join(" ", lstLine[index - 1]);
                            if (lineVal.IndexOf("desc", StringComparison.OrdinalIgnoreCase) >= 0 || lineVal.IndexOf("descon", StringComparison.OrdinalIgnoreCase) >= 0 || lineVal.IndexOf("desconto", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                decimal prVal = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                                decimal discount = ConvertHelper.ToDecimal(GetValueNumber(nextLine.Split(' ')));

                                var billRegToUpd = billTotalParse.BillParseModel.Where(b => b.Index == $"{indexBillValue}".PadLeft(2, '0'))
                                                                                .FirstOrDefault();
                                billRegToUpd.Price = prVal;
                                billRegToUpd.TotalPrice = prVal;
                                billRegToUpd.Discount = discount;
                                billRegToUpd.Discount *= billRegToUpd.Discount < 0 ? -1 : 1;

                                lastNextValueIsNumber = false;
                                continue;
                            }
                        }

                        if (nextLineValues.Count() == 1 && GetValueNumber(nextLineValues).IsNumber() && nextLineValues.HasDecimalPlaces())
                        {
                            lastNextValueIsNumber = true;
                            continue;
                        }
                        else if ((nextLine.IndexOf("kg", StringComparison.OrdinalIgnoreCase) >= 0 || nextLine.IndexOf("un", StringComparison.OrdinalIgnoreCase) >= 0) && nextLineValues.Count() == 1)
                        {
                            nextLine = lstLine[index + 2];
                            nextLineValues = nextLine.Split(' ')
                                                     .ToArray();
                            if (nextLineValues.Count() == 1 && GetValueNumber(nextLineValues).IsNumber())
                            {
                                lastNextValueIsNumber = true;
                                continue;
                            }
                        }
                        else if (lineValues.FirstOrDefault().IsNumber() && (lineValues.LastOrDefault().HasChar() || decimal.IsInteger(ConvertHelper.ToDecimal(lineValues.LastOrDefault()))))
                        {
                            lastNextValueIsNumber = true;
                            continue;
                        }
                    }

                    if (lineValues.Contains("()"))
                    {
                        lineValues = lineValues.SkipWhile(t => !t.Contains("()"))
                                               .Skip(1)
                                               .ToArray();
                    }
                    else if (line.HasParentheses())
                    {
                        lastValue = line.Split(' ').LastOrDefault();
                        lineValues = nextLine.Split(' ');

                        bool labelDescExists = lineValues.Any(val => val.OnlyChars().Length > 2);

                        if (lastValue.IsNumber() && !lastValue.HasParentheses() && lineValues.LastOrDefault().IsNumber() && !labelDescExists)
                        {
                            hashLinesProcessed.Add(nextLine);
                        }
                        else
                        {
                            string lineVal = lstLine[index - 1];
                            lineValues = lineVal.HasChar() && !lineVal.Split(' ').LastOrDefault().IsNumber().HasDecimalPlaces() ? lstLine[index].Split(' ') : lstLine[index - 1].Split(' ');
                        }
                    }
                    else if (lastValue.IsNumber() && ConvertHelper.ToDecimal(GetValueNumber(lastValue.Split(' '))).HasDecimalPlaces() && !lastValue.HasParentheses())
                    {
                        lineValues = nextLine.Split(' ');
                        hashLinesProcessed.Add(nextLine);
                    }

                    decimal priceVal = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                    priceVal = priceVal.HasDecimalPlaces() ? priceVal : 0;
                    var billRegToUpdate = billTotalParse.BillParseModel.Where(b => b.Index == $"{indexBillValue}".PadLeft(2, '0'))
                                                                       .FirstOrDefault();

                    if (priceVal == 0)
                    {
                        lstUntilNumberValue.Add(new BillUntilValue() { IndexBillValue = indexBillValue, CountUntil = index });

                        if (!lineValues.LastOrDefault().IsNumber() || ConvertHelper.ToDecimal(lineValues.LastOrDefault()).HasDecimalPlaces())
                        {
                            if (lineValues.FirstOrDefault().IsNumber() && (lineValues.LastOrDefault().HasChar() || decimal.IsInteger(ConvertHelper.ToDecimal(lineValues.LastOrDefault()))))
                            {
                                string itemName = string.Join(" ", lineValues.SkipWhile(v => v.IsNumber()));

                                billParseModel.Index = $"{indexBillValue + 1}".PadLeft(2, '0');
                                billParseModel.Name = itemName;

                                billTotalParse.BillParseModel.Add(billParseModel);
                                lastNextValueIsNumber = true;

                                indexBillValue++;

                                lstUntilNumberValue.Add(new BillUntilValue() { IndexBillValue = indexBillValue, CountUntil = index });
                            }
                        }

                        string lineVal = string.Join(" ", lineValues);
                        if (lineVal.IndexOf("desc", StringComparison.OrdinalIgnoreCase) >= 0 || lineVal.IndexOf("descon", StringComparison.OrdinalIgnoreCase) >= 0 || lineVal.IndexOf("desconto", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            lstUntilNumberValue.Add(new BillUntilValue() { IndexBillValue = indexBillValue, CountUntil = index, IsDiscount = true });
                        }
                    }

                    billRegToUpdate.Price = priceVal;
                    billRegToUpdate.TotalPrice = priceVal;

                    lastNextValueIsNumber = false;

                    if (line.HasChar() && !line.Split(' ').LastOrDefault().IsNumber())
                    {
                        lineValues = line.Split(' ');
                    }
                    else
                    {
                        continue;
                    }
                }

                if (nextLineHasText)
                {
                    billParseModel.Index = $"{indexBillValue + 1}".PadLeft(2, '0');
                    billParseModel.Name = line;

                    var lineTextData = lstLine[index + 1].Split(' ');
                    nextLineHasText = lstLine[index + 1].HasChar() && !(lineTextData.FirstOrDefault().IsNumber() && lineTextData.LastOrDefault().IsNumber());

                    if (!nextLineHasText)
                    {
                        billTotalParse.BillParseModel.Add(billParseModel);
                        previousLineHadText = true;
                    }
                }

                if (nextLineHasDiscountValue)
                {
                    string nextLine = lstLine[index + 1];
                    if (!nextLine.HasChar() && lstLine[index + 1].OnlyNumbers().IsNumber())
                    {
                        decimal discount = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                        if (discount < 0)
                        {
                            discount *= discount < 0 ? -1 : 1;
                            var billRegToUpdate = billTotalParse.BillParseModel.Where(b => b.Index == $"{indexBillValue}".PadLeft(2, '0'))
                                                                               .FirstOrDefault();
                            billRegToUpdate.Discount = billRegToUpdate.Discount <= 0 ? billTotalParse.Price - discount : billRegToUpdate.Discount;
                            nextLineHasDiscountValue = false;
                            continue;
                        }

                        nextLineHasDiscountValue = true;
                        continue;
                    }
                    else
                    {
                        decimal discount = ConvertHelper.ToDecimal(GetValueNumber(lineValues));
                        if (discount != 0)
                        {
                            discount *= discount < 0 ? -1 : 1;
                            var billRegToUpdate = billTotalParse.BillParseModel.Where(b => b.Index == $"{indexBillValue}".PadLeft(2, '0'))
                                                                               .FirstOrDefault();
                            billRegToUpdate.Discount = billRegToUpdate.Discount <= 0 || billRegToUpdate.Discount <= discount ? discount : billRegToUpdate.Discount;
                        }
                        else
                        {
                            lstUntilNumberValue.Add(new BillUntilValue() { IndexBillValue = indexBillValue, CountUntil = index + 1, IsDiscount = true });
                            nextLineHasDiscountValue = false;
                        }
                        nextLineHasDiscountValue = false;
                    }
                }

                if (nextLineFinishes)
                    break;

                if (!lineValues.LastOrDefault().IsNumber() || decimal.IsInteger(ConvertHelper.ToDecimal(lineValues.LastOrDefault())))
                {
                    if (lineValues.FirstOrDefault().IsNumber() && (lineValues.LastOrDefault().HasChar() || decimal.IsInteger(ConvertHelper.ToDecimal(lineValues.LastOrDefault()))))
                    {
                        const int validItemCodeSize = 12;
                        var linesFormat = lineValues.Select(v => v.RemoveSpecialChars());
                        if (linesFormat.All(v => v.IsNumber() || string.IsNullOrEmpty(v)))
                        {
                            if (lineValues.FirstOrDefault().StartsWith($"{indexBillValue + 1}".PadLeft(3, '0')))
                                nextLineHasText = true;
                            continue;
                        }

                        if ((line.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("tota", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("tora", StringComparison.OrdinalIgnoreCase) >= 0
                            || line.IndexOf("ite", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("otal", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("kg", StringComparison.OrdinalIgnoreCase) >= 0
                            || line.IndexOf("un", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("uni", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("pc", StringComparison.OrdinalIgnoreCase) >= 0)
                            && line.CountChars() <= 3)
                        {
                            bool isValidItemCode = lineValues.Count() >= 1 && lineValues[1].IsNumber() && lineValues[1].Count() == validItemCodeSize ? true : false;
                            if (!isValidItemCode)
                                continue;
                        }

                        string itemName = string.Join(" ", lineValues.SkipWhile(v => v.IsNumber()));

                        billParseModel.Index = $"{indexBillValue + 1}".PadLeft(2, '0');
                        billParseModel.Name = itemName;
                        billTotalParse.BillParseModel.Add(billParseModel);

                        string nextLine = lstLine[index + 1];
                        var nextLineArray = nextLine.Split(' ');

                        lastNextValueIsNumber = (nextLineArray.LastOrDefault().IsNumber() || decimal.IsInteger(ConvertHelper.ToDecimal(nextLineArray.LastOrDefault()))) && !nextLineArray.LastOrDefault().HasParentheses();
                        if (!lastNextValueIsNumber)
                        {
                            nextLine = lstLine[index + 2];
                            serializeNextNumber = !nextLine.HasChar() && nextLine.OnlyNumbers().IsNumber();
                        }

                        indexBillValue++;
                        continue;
                    }
                }

                if (line.IndexOf("desco", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("descon", StringComparison.OrdinalIgnoreCase) >= 0 || line.IndexOf("desconto", StringComparison.OrdinalIgnoreCase) >= 0
                    || line.IndexOf("a pagar", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if ((((float)index) / lstLine.Count()) > 0.02 && !(line.IndexOf("descontos", StringComparison.OrdinalIgnoreCase) >= 0) && !(line.IndexOf("valor", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        nextLineHasDiscountValue = true;
                    }
                    continue;
                }

                string lineFormat = string.Concat(lineValues);
                if (!lineValues.FirstOrDefault().IsNumber() || lineFormat.IsNumber())
                {
                    if (index == lstLine.Count - 1)
                        continue;
                    var nextLine = lstLine[index + 1].Split(' ');
                    if (line.AllChars())
                        continue;

                    bool isLineValid = nextLine.LastOrDefault().IsNumber() && billTotalParse.TotalPrice == 0 && !(lineFormat.IndexOf("valor", StringComparison.OrdinalIgnoreCase) >= 0) && !(lineFormat.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0)
                                       && !(lineFormat.IndexOf("acréscimo", StringComparison.OrdinalIgnoreCase) >= 0) && !(lineFormat.IndexOf("acrescimo", StringComparison.OrdinalIgnoreCase) >= 0) && !(lineFormat.IndexOf("kg", StringComparison.OrdinalIgnoreCase) >= 0)
                                       && !(lineFormat.IndexOf("uni", StringComparison.OrdinalIgnoreCase) >= 0) && !(lineFormat.IndexOf("pc", StringComparison.OrdinalIgnoreCase) >= 0) && !(lineFormat.IndexOf("un", StringComparison.OrdinalIgnoreCase) >= 0)
                                       && (ConvertHelper.ToDecimal(GetValueNumber(lineValues)).HasDecimalPlaces() || ConvertHelper.ToDecimal(GetValueNumber(nextLine.LastOrDefault().Split(' '))).HasDecimalPlaces());

                    string sNextLine = string.Concat(nextLine);
                    if (sNextLine.IndexOf("uni", StringComparison.OrdinalIgnoreCase) >= 0 && sNextLine.CountChars() == 3)
                    {
                        nextLine = lstLine[index + 2].Split(' ');
                        isLineValid = nextLine.LastOrDefault().IsNumber() && billTotalParse.TotalPrice == 0 && !(lineFormat.IndexOf("valor", StringComparison.OrdinalIgnoreCase) >= 0)
                                      && ConvertHelper.ToDecimal(GetValueNumber(nextLine)).HasDecimalPlaces();
                    }

                    if (!isLineValid)
                    {
                        continue;
                    }
                }
                if (hashLinesProcessed.Contains(line))
                    continue;

                line = string.Concat(line.Split(' ').Where(ln => !string.Equals("R$", ln, StringComparison.OrdinalIgnoreCase))
                                                    .Select(ln => string.Concat(ln, " ")));

                string textFromBill = new string(line.Where(l => char.IsLetter(l) || char.IsWhiteSpace(l)).ToArray()).Trim();
                decimal price = ConvertHelper.ToDecimal(GetValueNumber(lineValues));

                if (string.IsNullOrEmpty(textFromBill))
                    continue;

                var containsInvalidText = textFromBill.Split(' ')
                                                      .ToList()
                                                      .Exists(t =>
                                                      {
                                                          return t.IndexOf("total", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("tota", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("tora", StringComparison.OrdinalIgnoreCase) >= 0
                                                          || t.IndexOf("ite", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("otal", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("kg", StringComparison.OrdinalIgnoreCase) >= 0
                                                          || t.IndexOf("un", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("uni", StringComparison.OrdinalIgnoreCase) >= 0 || t.IndexOf("pc", StringComparison.OrdinalIgnoreCase) >= 0
                                                          || t.IndexOf("qtde", StringComparison.OrdinalIgnoreCase) >= 0;
                                                      });
                if (containsInvalidText)
                    continue;

                if (previousLineHadText)
                {
                    if (price == 0)
                    {
                        int unitIndex = lineValues.ToList().FindIndex(val => string.Equals(val, "R$", StringComparison.OrdinalIgnoreCase));
                        if (unitIndex > 0)
                        {
                            price = ConvertHelper.ToDecimal(GetValueNumber(lineValues[unitIndex + 1].Split(' ')));
                        }
                        else
                        {
                            price = ConvertHelper.ToDecimal(GetValueNumber(lineValues.LastOrDefault().Split(' ')));
                        }
                    }

                    billParseModel = billTotalParse.BillParseModel.Where(b => b.Index == $"{indexBillValue + 1}".PadLeft(2, '0'))
                                                                  .FirstOrDefault();
                    billParseModel.Price = price;
                    billParseModel.TotalPrice = price;
                    previousLineHadText = false;
                }
                else
                {
                    billParseModel.Index = $"{indexBillValue + 1}".PadLeft(2, '0');
                    billParseModel.Name = textFromBill;
                    billParseModel.Price = price;
                    billParseModel.TotalPrice = price;

                    if (lineFormat.AllChars())
                        lastNextValueIsNumber = true;

                    billTotalParse.BillParseModel.Add(billParseModel);
                }

                indexBillValue++;
            }

            return billTotalParse;
        }
        [HttpPost]
        [Route("imagetext")]
        public async Task<ActionResult> GetImageText([FromBody] ImageParam imageParam)
        {
            if (imageParam == null)
                return BadRequest();

            var imageData = Convert.FromBase64String(imageParam.Base64Img);
            using ComputerVisionClient client = Authenticate(endpoint, key);

            if (imageParam.RotateImage90)
                imageData = MobileDeviceFix(imageData);

            var textHeaders = await client.ReadInStreamAsync(new MemoryStream(imageData));
            string operationLocation = textHeaders.OperationLocation;
            Thread.Sleep(500);

            const int numberOfCharsInOperationId = 36;
            string operationId = operationLocation.Substring(operationLocation.Length - numberOfCharsInOperationId);
            ReadOperationResult results = null;
            bool gettingData = true;

            while (gettingData)
            {
                results = await client.GetReadResultAsync(Guid.Parse(operationId));
                gettingData = results.Status == OperationStatusCodes.Running || results.Status == OperationStatusCodes.NotStarted;
            }

            StringBuilder sb = new StringBuilder();
            var textUrlFileResults = results.AnalyzeResult.ReadResults;
            foreach (ReadResult page in textUrlFileResults)
            {
                foreach (Line line in page.Lines)
                {
                    sb.AppendLine(line.Text);
                }
            }

            MemoryCache.Set("teste", imageParam.Base64Img);

            try
            {
                var billParseResult = Parse(sb.ToString());
                return Ok(new ParseResult<BillTotalParseModel>() { Ok = true, Result = billParseResult });
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }
        [HttpPost]
        [Route("queueservice")]
        public async Task<ActionResult> SendImageText([FromBody] ImageParam imageParam)
        {
            string transactionId = await ReceiptTransformLogic.Send(imageParam);
            return Ok(transactionId);
        }
        [HttpPost]
        [Route("parse")]
        public async Task<ActionResult> ParseImageData([FromBody] ImageParam imageParam)
        {
            var parsedItem = await ReceiptParserLogic.Parse(imageParam);
            return Ok(parsedItem);
        }
        private string GetValueNumber(string[] values)
        {
            if (values == null)
                return string.Empty;

            string concatVal = string.Join("", values);

            if (values.Count() > 1)
            {
                if (values.All(val => val.OnlyNumbers().IsNumber()))
                {
                    StringBuilder sbVal = new StringBuilder();
                    for (int index = 0; index < values.Count(); index++)
                    {
                        string val = values[index];

                        if (index == values.Count() - 1)
                        {
                            if (!string.IsNullOrEmpty(sbVal.ToString()))
                                sbVal.Append(",");
                        }
                        else if (index > 0)
                        {
                            sbVal.Append(".");
                        }

                        sbVal.Append(val);
                    }

                    return sbVal.ToString();
                }
                else if (values.LastOrDefault().Split(' ').All(val => val.OnlyNumbers().IsNumber()))
                {
                    StringBuilder sbVal = new StringBuilder();
                    var newValues = values.LastOrDefault().Split(' ');
                    for (int index = 0; index < newValues.Count(); index++)
                    {
                        string val = newValues[index];

                        if (index == newValues.Count() - 1)
                        {
                            if (!string.IsNullOrEmpty(sbVal.ToString()))
                                sbVal.Append(",");
                        }
                        else if (index > 0)
                        {
                            sbVal.Append(".");
                        }

                        sbVal.Append(val);
                    }

                    return sbVal.ToString();
                }
            }

            return concatVal;
        }
        private byte[] MobileDeviceFix(byte[] byteImage)
        {
            //Image image = Image.Load(new MemoryStream(byteImage));
            //image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            //image.Resize(image.Width - (int)(image.Width * 0.4), image.Height - (int)(image.Height * 0.4));

            //using var imageData = new MemoryStream();
            //image.Save(imageData);


            var image = new MagickImage(byteImage);
            image.Rotate(90);
            //image.Deskew((Percentage)50);
            ////image.Despeckle(); 
            //image.Resize(image.Width - (int)(image.Width * 0.9), image.Height - (int)(image.Height * 0.9));
            ////image.Format = MagickFormat.Png32;

            ////QuantizeSettings qs = new QuantizeSettings();
            ////qs.ColorSpace = ColorSpace.Gray;
            ////image.Quantize(qs);
            ////image.ColorType = ColorType.Grayscale;
            ////image.ContrastStretch(new Percentage(0), new Percentage(0));

            ////image.AutoLevel();
            ////image.Negate();
            ////image.AdaptiveThreshold(30, 30, 10);
            ////image.Negate();

            ////return imageData.ToArray();
            //return image.ToByteArray();

            return image.ToByteArray();
        }
        public ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
            {
                Endpoint = endpoint
            };
            return client;
        }
    }
    public class ParseParam
    {
        public string Text { get; set; }
    }
    public class ParseResult<TResult> where TResult : class
    {
        public bool Ok { get; set; }
        public TResult Result { get; set; }
    }
    public class BillParseModel
    {
        public string Index { get; set; }
        public string Name { get; set; }
        public string Unit { get; set; }
        public decimal Price { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal Discount { get; set; }
    }
    public class BillTotalParseModel
    {
        public List<BillParseModel> BillParseModel { get; set; } = new List<BillParseModel>();
        public short TotalItems { get; set; }
        public string Currency { get; set; }
        [JsonPropertyName("sumTotalPrice")]
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPrice { get; set; }
    }
    public class BillUntilValue
    {
        public int IndexBillValue { get; set; }
        public int CountUntil { get; set; }
        public bool IsDiscount { get; set; }
    }
}
