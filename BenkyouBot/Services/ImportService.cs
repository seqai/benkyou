using BenkyouBot.Model;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using User = Benkyou.DAL.Entities.User;

namespace BenkyouBot.Services;

public class ImportService 
{
    private readonly ILogger<ImportService> _logger;
    private readonly BotClient _botClient;
    private readonly RecordExtractionService _recordExtractionService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ImportService(ILogger<ImportService> logger, BotClient botClient, RecordExtractionService recordExtractionService, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _botClient = botClient;
        _recordExtractionService = recordExtractionService;
        _httpClientFactory = httpClientFactory;
    }
    public async Task HandleImport(User user, ImportParameters parameters, Document updateMessageDocument, CancellationToken cancellationToken)
    {
        try
        {
            if (parameters.Type == ImportType.Unknown)
            {
                await _botClient.SendMessageAsync(user.TelegramId, "Unknown import type", cancellationToken: cancellationToken);
                return;

            }
            
            var fileInfo = await _botClient.GetFileAsync(updateMessageDocument.FileId, cancellationToken);
            var fileUrl = $"https://api.telegram.org/file/bot{_botClient.Token}/{fileInfo.FilePath}";
            var httpClient = _httpClientFactory.CreateClient();
            var fileContent = await httpClient.GetByteArrayAsync(fileUrl, cancellationToken);

            switch (parameters.Type)
            {
                case ImportType.TelegramHistory:
                    await _recordExtractionService.ImportTgHistory(user, fileContent, cancellationToken, parameters.AddScore);
                    break;
                case ImportType.Csv:
                    await _recordExtractionService.ImportCsvHistory(
                        user, 
                        fileContent, 
                        parameters.AddScore,
                        parameters.ContentColumnIndex,
                        parameters.RecordTypeColumnIndex,
                        parameters.DateColumnIndex,
                        parameters.AssumedDate,
                        cancellationToken);
                    break;
                default:
                    await _botClient.SendMessageAsync(user.TelegramId, "Unsupported import type", cancellationToken: cancellationToken);
                    return;
            }

            await _botClient.SendMessageAsync(user.TelegramId, "Import finished", cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            await _botClient.SendMessageAsync(user.TelegramId, "Error while importing", cancellationToken: cancellationToken);
            _logger.LogError(e, "Error while handling import");
        }
    }
}