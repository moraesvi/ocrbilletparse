using OCRBilletParse.Interface;
using OCRBilletParse.Logic;
using OCRBilletParse.Queue.Factory;
using OCRBilletParse.Queue.Infraestructure;
using OCRBilletParse.Queue.Interface;
using OCRBilletParse.Queue.Logic;
using OCRBilletParse.Queue.Services;
using OCRBilletParse.Queue.Worker;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddSingleton<IBillParseQueueInfraestructure, BillParseQueueInfraestructure>();
builder.Services.AddSingleton<IBillParseQueueLogic, BillParseQueueLogic>();
builder.Services.AddSingleton((service) => new RabbitMqConnectionFactory(service.GetRequiredService<IConfiguration>(), "BillParse"));
builder.Services.AddHostedService<BillParseQueue>();
builder.Services.AddHttpClient<INoSqlStorageService, RedisStorageService>(client => 
{
    client.BaseAddress = new Uri(builder.Configuration["Services:StorageApi:Uri"]);
}).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
  .AddPolicyHandler(GetRetryPolicy());
builder.Services.AddHttpClient<IReceiptParseService, ReceiptParseService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:ReceiptApi:Uri"]);
}).SetHandlerLifetime(TimeSpan.FromMinutes(5))  //Set lifetime to five minutes
  .AddPolicyHandler(GetRetryPolicy());

var app = builder.Build();
app.MapControllers();
app.Run();

IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound)
        .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
