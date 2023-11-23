using OCRBilletParse.Interface;
using OCRBilletParse.Logic;
using OCRBilletParse.Services;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IBrazilianImageRecognitionParseService, BrazilianReceiptParseService>();
builder.Services.AddSingleton<ImageRecognitionService, AzureRecognitionService>();
builder.Services.AddSingleton<IReceiptParserLogic, ReceiptReconigtionLogic>();
builder.Services.AddHttpClient<IReceiptTransformLogic, ReceiptQueueService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:QueueApi:Uri"]);
}).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
  .AddPolicyHandler(GetRetryPolicy());

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseAuthorization();
app.MapControllers();
app.UseCors(x => x.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
app.Run();

IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
