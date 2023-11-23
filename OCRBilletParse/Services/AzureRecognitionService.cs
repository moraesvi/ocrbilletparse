using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using OCRBilletParse.Interface;
using System.Text;

namespace OCRBilletParse.Services
{
    public class AzureRecognitionService : ImageRecognitionService
    {
        static string key = "351c561a71d842db9e4ef12ce07a78f5";
        static string endpoint = "https://ocrbilletparse.cognitiveservices.azure.com/";
        public async Task<string> GetText(byte[] image) 
        {
            using ComputerVisionClient client = Authenticate(endpoint, key);

            var textHeaders = await client.ReadInStreamAsync(new MemoryStream(image));
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

            return sb.ToString();
        }
        private ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
            {
                Endpoint = endpoint
            };
            return client;
        }
    }
}
