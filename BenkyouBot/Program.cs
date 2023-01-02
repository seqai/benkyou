using Benkyou;
using Benkyou.DAL;
using Benkyou.DAL.Services;
using BenkyouBot.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using File = System.IO.File;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Add services to the container.
builder.Services.AddOptions<TelegramOptions>().Bind(builder.Configuration.GetSection("TelegramOptions"));
builder.Services.RegisterBenkyoDAL(builder.Configuration.GetConnectionString("BenkyouDb"));

builder.Services.AddHostedService(sp =>
{
    var botClient = sp.GetRequiredService<BotClient>();
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>();
    var webHostEnvironment = sp.GetRequiredService<IWebHostEnvironment>();
    var hostApplicationLifetime = sp.GetRequiredService<IHostApplicationLifetime>();
    var logger = sp.GetRequiredService<ILogger<AppInitializer>>();

    var certificatePath = builder.Configuration.GetValue<string>("Kestrel:Certificates:Default:Path");
    var rawCertificate = File.ReadAllBytes(certificatePath);

    return new AppInitializer(botClient, options, rawCertificate, webHostEnvironment, hostApplicationLifetime, logger);
});
builder.Services.AddTransient(sp =>
{
    var options = sp.GetRequiredService<IOptions<TelegramOptions>>().Value;
    return new BotClient(options.BotToken);
});
builder.Services.AddScoped<ImportService>();
builder.Services.AddTransient<RecordExtractionService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthorization();

app.MapControllers();

// Migration and data seeding
using (var serviceScope = app.Services.CreateScope())
{
    var dbContext = serviceScope.ServiceProvider.GetRequiredService<BenkyouDbContext>();
    dbContext.Database.Migrate();
    var userService = serviceScope.ServiceProvider.GetRequiredService<UserService>();

    var options = serviceScope.ServiceProvider.GetRequiredService<IOptions<TelegramOptions>>().Value;
    var adminId = options.AdminId;
    var adminName = options.AdminName;
    if (userService.GetUserByTelegramId(adminId).GetAwaiter().GetResult() is null)
    {
        userService.CreateUser(adminId, adminName).GetAwaiter().GetResult();
    }
}

app.Run();

internal class AppInitializer : IHostedService
{
    private readonly ILogger<AppInitializer> _logger;

    private readonly BotClient _botClient;
    private readonly TelegramOptions _telegramOptions;

    private InputFile? _certInputFile;

    public AppInitializer(BotClient botClient, IOptions<TelegramOptions> telegramOptions, byte[] certificate, IWebHostEnvironment env, IHostApplicationLifetime appLifetime, ILogger<AppInitializer> logger)
    {
        _botClient = botClient;
        _telegramOptions = telegramOptions.Value;
        _logger = logger;

        _certInputFile = new InputFile(certificate, "public_key.pem");
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _botClient.DeleteWebhookAsync(cancellationToken: cancellationToken, dropPendingUpdates: true);
        await _botClient.SetWebhookAsync(_telegramOptions.WebHookUrl, _certInputFile, cancellationToken: cancellationToken);
        _certInputFile = null;
        
        _logger.LogInformation("Webhook is set: {WebHookUrl}", _telegramOptions.WebHookUrl);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await _botClient.DeleteWebhookAsync(cancellationToken: cancellationToken);
    }
}

