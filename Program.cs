using VSMSWebClient.Data;
using Microsoft.EntityFrameworkCore;
using VSMSWebClient.Services;

var builder = WebApplication.CreateBuilder(args);

// Adding SQLite
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=requests.db"));


builder.Services.AddControllers();

builder.Services.AddSingleton<IniFileService>();
builder.Services.AddScoped<DataTransferService>();
builder.Services.AddScoped<RequestLoggerService>();
builder.Services.AddScoped<RequestRepositoryService>();
builder.Services.AddHostedService<BackgroundDataSenderService>();

builder.Services.AddHttpClient();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddFile("logs/ServerMainInfo-{Date}.txt");

int port = 8081;
builder.WebHost.UseUrls($"http://*:{port}", $"http://0.0.0.0:{port}");

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated(); // Creates databases and tables if there are none
}

//app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
