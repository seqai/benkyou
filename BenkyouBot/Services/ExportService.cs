using Benkyou.DAL.Entities;
using Benkyou.DAL.Filters;
using Benkyou.DAL.Services;
using BenkyouBot.Controllers;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using User = Benkyou.DAL.Entities.User;

namespace BenkyouBot.Services;

public class ExportService 
{
    private const string ExportFileName = "export.csv";
    private const string Separator = ",";

    private readonly ILogger<TelegramController> _logger;
    private readonly RecordService _recordService;
    private readonly BotClient _botClient;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExportService(ILogger<TelegramController> logger, RecordService recordService, BotClient botClient, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _recordService = recordService;
        _botClient = botClient;
        _httpClientFactory = httpClientFactory;
    }

    public async Task HandleExport(User user, RecordType recordType, DateOnly from, DateOnly till, bool showIgnored, CancellationToken cancellationToken)
    {
        var filter = new RecordFilter
        {
            RecordTypes = new [] { recordType },
            FromDate = from.ToDateTime(TimeOnly.MinValue),
            ToDate = till.ToDateTime(TimeOnly.MinValue),
        };
        
        var records = await _recordService.GetRecords(user.Id, filter, showIgnored);
        var csvStream = new MemoryStream();
        await using var writer = new StreamWriter(csvStream);
        foreach (var record in records)
        {
            await writer.WriteAsync(record.Content);
            await writer.WriteAsync(Separator);
            await writer.WriteAsync(record.RecordType.ToString());
            await writer.WriteAsync(Separator);
            await writer.WriteAsync(record.Score.ToString());
            await writer.WriteAsync(Separator);
            await writer.WriteAsync(record.CreatedAt.ToString("yyyy-MM-dd"));
            await writer.WriteAsync(Separator);
            await writer.WriteAsync(record.UpdatedAt.ToString("yyyy-MM-dd"));
            await writer.WriteAsync(Separator);
            if (showIgnored)
            {
                await writer.WriteAsync(record.Ignored.ToString());
                await writer.WriteAsync(Separator);
            }

            await writer.WriteLineAsync();
        }
        await writer.FlushAsync();
        csvStream.Position = 0;
        await _botClient.SendDocumentAsync(user.TelegramId, new InputFile(csvStream, ExportFileName), cancellationToken: cancellationToken);
    }
}