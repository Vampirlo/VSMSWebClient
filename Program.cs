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

var iniService = new IniFileService();
string portValue = iniService.ReadValue("VSMSWebClient", "port");
string localhostValue = iniService.ReadValue("VSMSWebClient", "localhost");

int port = 8081;
if (!string.IsNullOrEmpty(portValue) && int.TryParse(portValue, out int parsedPort))
{
    port = parsedPort;
}

bool useLocalhost = !string.IsNullOrEmpty(localhostValue) &&
                    bool.TryParse(localhostValue, out bool parsedLocalhost) &&
                    parsedLocalhost;

if (useLocalhost)
{
    builder.WebHost.UseUrls(
        $"https://0.0.0.0:{port}",
        $"https://localhost:{port}"
    );
}
else
{
    builder.WebHost.UseUrls($"https://0.0.0.0:{port}");
}

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.EnsureCreated(); // Creates databases and tables if there are none
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
