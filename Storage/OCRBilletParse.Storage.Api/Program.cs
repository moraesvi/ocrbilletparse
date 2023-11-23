    using OCRBilletParse.Storage.Infraestructure;
using OCRBilletParse.Storage.Interface;
using OCRBilletParse.Storage.Logic;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

builder.Services.AddControllers();
builder.Services.AddSingleton<INoSqlStorageInfraestructure, RedisStorageInfraestructure>();
builder.Services.AddSingleton<INoSqlStorageLogic, RedisStorageLogic>();

var app = builder.Build();
app.MapControllers();
app.Run();
