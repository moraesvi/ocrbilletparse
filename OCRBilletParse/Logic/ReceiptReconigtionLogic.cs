using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OCRBilletParse.Common.Model;
using OCRBilletParse.Interface;

namespace OCRBilletParse.Logic
{
    public class ReceiptReconigtionLogic : IReceiptParserLogic
    {
        private ILogger<ReceiptReconigtionLogic> Logger { get; }
        private IConfiguration Config { get; }
        private ImageRecognitionService AzureRecognitionService { get; }
        private IBrazilianImageRecognitionParseService ImageRecognitionParseService { get; }
        public ReceiptReconigtionLogic(ILogger<ReceiptReconigtionLogic> logger, IConfiguration config, ImageRecognitionService azureRecognitionService, IBrazilianImageRecognitionParseService imageRecognitionParseService)
        {
            Logger = logger;
            Config = config;
            AzureRecognitionService = azureRecognitionService;
            ImageRecognitionParseService = imageRecognitionParseService;
        }
        public async Task<BillTotalParseModel> Parse(ImageParam imageParam)
        {
            try
            {
                var imageData = Convert.FromBase64String(imageParam.Base64Img);
                Logger.LogInformation("Requested a new receipt for parse");

                string imageText = await AzureRecognitionService.GetText(imageData);
                var receiptData = ImageRecognitionParseService.Parse(imageText);

                return receiptData;
            }
            catch (Exception ex)
            {
                Logger.LogError($"An exception has ocorred doing the receipt parse");
                Logger.LogError(ex.Message);
                Logger.LogError(ex.StackTrace);

                throw;
            }
        }
    }
}
