using BenkyouBot.Controllers;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using User = Benkyou.DAL.Entities.User;

namespace BenkyouBot.Services;

public class ImportService 
{
    private readonly ILogger<TelegramController> _logger;
    private readonly BotClient _botClient;
    private readonly RecordExtractionService _recordExtractionService;

    public ImportService(ILogger<TelegramController> logger, BotClient botClient, RecordExtractionService recordExtractionService)
    {
        _logger = logger;
        _botClient = botClient;
        _recordExtractionService = recordExtractionService;
    }
    public async Task HandleImport(User user, string args, Document updateMessageDocument, CancellationToken cancellationToken)
    {
        try
        {
            var argsTokens = args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var type = argsTokens[0].ToLower();
            var addScore = false;
            var assumedDate = new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            foreach (var token in argsTokens.Skip(1))
            {
                if (token == "-score")
                {
                    addScore = true;
                }
                else if (token.StartsWith("-date"))
                {
                    var date = token.Split('=', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)[1];
                    assumedDate = DateOnly.ParseExact(date, "yyyy-MM-dd");
                }
            }

            var fileInfo = await _botClient.GetFileAsync(updateMessageDocument.FileId, cancellationToken);
            var fileUrl = $"https://api.telegram.org/file/bot{_botClient.Token}/{fileInfo.FilePath}";
            var fileContent = await new HttpClient().GetByteArrayAsync(fileUrl, cancellationToken);
            switch (type)
            {
                case "tg":
                    await _recordExtractionService.ImportTgHistory(user, fileContent, cancellationToken, addScore);
                    break;
                case "jisho":
                    await _recordExtractionService.ImportJishoHistory(user, fileContent, cancellationToken, addScore, assumedDate);
                    break;
                default:
                    await _botClient.SendMessageAsync(user.TelegramId, "Unknown import type", cancellationToken: cancellationToken);
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