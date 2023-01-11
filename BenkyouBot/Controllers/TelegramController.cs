using System.Collections.Concurrent;
using System.Text;
using Benkyou.DAL.Entities;
using Benkyou.DAL.Services;
using Benkyou.Infrastructure;
using BenkyouBot.Infrastructure;
using BenkyouBot.Services;
using BenkyouBot.Model;
using Microsoft.AspNetCore.Mvc;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
using static BenkyouBot.Infrastructure.CommandArgumentParsers;
using Npgsql.Replication.PgOutput.Messages;
using User = Benkyou.DAL.Entities.User;

namespace BenkyouBot.Controllers;

[ApiController]
[Route("api/telegram")]
public class TelegramController : ControllerBase
{
    private static readonly ConcurrentDictionary<long, UserState> State = new ConcurrentDictionary<long, UserState>();

    private readonly ILogger<TelegramController> _logger;
    private readonly BotClient _botClient;
    private readonly UserService _userService;
    private readonly RecordService _recordService;
    private readonly RecordExtractionService _recordExtractionService;
    private readonly IServiceProvider _serviceProvider;

    public TelegramController(ILogger<TelegramController> logger, BotClient botClient, UserService userService, RecordService recordService, RecordExtractionService recordExtractionService, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _botClient = botClient;
        _userService = userService;
        _recordService = recordService;
        _recordExtractionService = recordExtractionService;
        _serviceProvider = serviceProvider;
    }

    [HttpPost("update")]
    public async Task<IActionResult> Post([FromBody] Update update, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Update received: {Update}", update);
            if (update.Message is not null && update.Message.From != null)
            {
                var user = await _userService.GetUserByTelegramId(update.Message.From.Id);
                if (user is not null)
                {
                    _logger.LogInformation("Message from {UserId}: {Text}", update.Message.From.Id, update.Message.Text);
                    await HandleMessage(user, update.Message, cancellationToken);
                }
                else
                {
                    _logger.LogInformation("User {UserId} is not registered", update.Message.From.Id);
                }
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling update");
        }
        return Ok();
    }

    private async Task HandleMessage(User user, Message updateMessage, CancellationToken cancellationToken)
    {
        var message = updateMessage.Text;
        var state = State.GetValueOrDefault(user.TelegramId, UserState.None);

        if (message is not null && message.StartsWith("/"))
        {
            await HandleCommand(user, message, cancellationToken);
        } 
        else if (state.IsImporting && updateMessage.Document is not null)
        {
            await _botClient.SendMessageAsync(user.TelegramId, "Importing... (this might take some time because I am too lazy to implement proper merging now)", cancellationToken: cancellationToken);
            RunImport(user, state, updateMessage.Document, cancellationToken);
        }
        else
        {
            await ExtractRecords(user, user.DefaultRecordType, updateMessage.Text ?? string.Empty, cancellationToken);
        }
    }
    
    private async Task HandleCommand(User user, string message, CancellationToken cancellationToken)
    {
        var tokens = message.Tokenize(2);
        var command = tokens[0].ToLower();
        var args = tokens.Length > 1 ? tokens[1] : string.Empty;
        switch (command)
        {
            case "/cancel":
                State.Remove(user.TelegramId, out _);
                break;
            case "/ignore":
                await HandleIgnoreOrIncludeCommand(user, args, cancellationToken, include: false);
                break;
            case "/include":
                await HandleIgnoreOrIncludeCommand(user, args, cancellationToken, include: true);
                break;
            case "/score":
                await HandleScoreCommand(user, args, cancellationToken);
                break;
            case "/top":
                await HandleTopCommand(user, args, cancellationToken);
                break;
            case "/export":
                await HandleExportCommand(user, args, cancellationToken);
                break;
            case "/defaults":
                await HandleDefaultsCommand(user, args, cancellationToken);
                break;
            case "/autotag":
                await HandleAutoTagCommand(user, args, cancellationToken);
                break;
            case "/stopautotag":
                await HandleStopAutoTagCommand(user, args, cancellationToken);
                break;
            case "/import":
                var (parameters, helpMessage) = ParseImportCommandArguments(args.Tokenize());
                if (!string.IsNullOrWhiteSpace(helpMessage))
                {
                    await _botClient.SendMessageAsync(user.TelegramId, helpMessage, cancellationToken: cancellationToken);
                    return;
                }
                await _botClient.SendMessageAsync(user.TelegramId, "Send me the file to import", cancellationToken: cancellationToken);
                State[user.TelegramId] = new UserState(true, parameters);
                break;
        }

        var types = Enum.GetValues<RecordType>().Where(t => t != RecordType.Any);
        foreach (var type in types)
        {
            if (type.GetAliases().Any(a => command == $"/{a.ToLower()}"))
            {
                await ExtractRecords(user, type, args, cancellationToken);
                break;
            }
        }
    }

    private async Task ExtractRecords(User user, RecordType type, string input, CancellationToken cancellationToken)
    {
        var (createdRecords, updatedRecords) = await _recordExtractionService.ProcessMessage(user, input, type, cancellationToken);
        var sb = new StringBuilder();
        foreach (var record in createdRecords)
        {
            sb.AppendLine($"Created: {record.Content} ({record.RecordType})");
        }
        foreach (var record in updatedRecords)
        {
            sb.AppendLine($"Updated: {record.Content} ({record.RecordType}): {record.Score}");
        }
        if (sb.Length > 0)
        {
            await _botClient.SendMessageAsync(user.TelegramId, sb.ToString(), cancellationToken: cancellationToken);
        }
    }

    private async Task HandleScoreCommand(User user, string args, CancellationToken cancellationToken)
    {
        try
        {
            var (content, recordType, score, includeIgnored, updateTime, helpMessage) = ParseScoreCommandArguments(args.Tokenize());
            if (!string.IsNullOrWhiteSpace(helpMessage))
            {
                await _botClient.SendMessageAsync(user.TelegramId, helpMessage, cancellationToken: cancellationToken);
                return;
            }

            foreach (var item in content)
            {
                var existingRecord = await _recordService.GetRecordByContent(user.Id, item, recordType);
                if (existingRecord is not null && (includeIgnored || !existingRecord.Ignored))
                {
                    var updatedTime = updateTime ? DateTime.UtcNow : existingRecord.UpdatedAt;
                    await _recordService.UpdateRecord(existingRecord, updatedTime, score, existingRecord.Ignored, addHit: false);
                    await _botClient.SendMessageAsync(user.TelegramId, $"Updated record: {existingRecord.Content} ({existingRecord.RecordType}): {score}", cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessageAsync(user.TelegramId, $"Record {item} ({recordType}) does not exist or marked as ignored", cancellationToken: cancellationToken);
                }
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling ignore command");
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling top command", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleExportCommand(User user, string args, CancellationToken cancellationToken)
    {
        var (recordType, from, till, includeIgnored, helpMessage) = ParseExportCommandArguments(args.Tokenize());
        if (!string.IsNullOrWhiteSpace(helpMessage))
        {
            await _botClient.SendMessageAsync(user.TelegramId, helpMessage, cancellationToken: cancellationToken);
            return;
        }
        RunExport(user, recordType, from, till, includeIgnored, cancellationToken);
    }

    private async Task HandleIgnoreOrIncludeCommand(User user, string args, CancellationToken cancellationToken, bool include)
    {
        try
        {
            var (content, recordType, helpMessage) = ParseIgnoreOrIncludeCommandArguments(args.Tokenize());
            if (!string.IsNullOrWhiteSpace(helpMessage))
            {
                await _botClient.SendMessageAsync(user.TelegramId, helpMessage, cancellationToken: cancellationToken);
                return;
            }

            var ignored = !include;
            foreach (var item in content)
            {
                var existingRecord = await _recordService.GetRecordByContent(user.Id, item, recordType);
                if (existingRecord is not null)
                {
                    await _recordService.UpdateRecord(existingRecord, existingRecord.UpdatedAt, existingRecord.Score, ignored, addHit: false);
                    await _botClient.SendMessageAsync(user.TelegramId, $"Record {item} ({recordType}) is now {(ignored ? "ignored" : "included")}", cancellationToken: cancellationToken);
                }
                else
                {
                    await _botClient.SendMessageAsync(user.TelegramId, $"Record {item} ({recordType}) does not exist", cancellationToken: cancellationToken);
                }
            }

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling ignore command");
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling top command", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleTopCommand(User user, string args, CancellationToken cancellationToken)
    {
        try
        {
            var (count, recordType, from, till, showIgnored, flat, helpMessage) = ParseTopCommandArguments(args.Tokenize());
            if (!string.IsNullOrWhiteSpace(helpMessage))
            {
                await _botClient.SendMessageAsync(user.TelegramId, helpMessage, cancellationToken: cancellationToken);
                return;
            }
            
            var top = await _recordService.GetTopRecordsByDate(user.Id, count, recordType, from, till, showIgnored);
            var sb = new StringBuilder();
            foreach (var record in top)
            {
                if (flat)
                {
                    sb.Append($"{record.Content} ");
                }
                else
                {
                    sb.AppendLine($"{record.Score}\t{record.UpdatedAt:yyyy-MM-dd}\t{record.Content}");
                }
            }
            await _botClient.SendMessageAsync(user.TelegramId, sb.ToString().TrimEnd(), cancellationToken: cancellationToken);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling top command");
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling top command", cancellationToken: cancellationToken);
        }

    }

    private async Task HandleDefaultsCommand(User user, string args, CancellationToken cancellationToken)
    {
        try
        {
            var (recordType, autotagValidityMinutes, helpMessage) = ParseDefaultsCommandArguments(user, args.Tokenize());
            if (!string.IsNullOrWhiteSpace(helpMessage))
            {
                await _botClient.SendMessageAsync(user.TelegramId, helpMessage, cancellationToken: cancellationToken);
                return;
            }
            await _userService.UpdateDefaults(user, recordType, autotagValidityMinutes);
            await _botClient.SendMessageAsync(user.TelegramId, $"Defaults updated. Record type: {recordType}. Autotagging duration: {autotagValidityMinutes}", cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling defaults command");
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling defaults command", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleStopAutoTagCommand(User user, string args, CancellationToken cancellationToken)
    {
        try
        {
            await _userService.UpdateAutoTag(user, string.Empty);
            await _botClient.SendMessageAsync(user.TelegramId, "Autotagging stopped", cancellationToken: cancellationToken);
        }
        catch
        {
            _logger.LogError("Error while handling stop auto tag command");
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling stopautotag command", cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAutoTagCommand(User user, string args, CancellationToken cancellationToken)
    {
        try
        {
            var tag = args.Trim();
            if (string.IsNullOrWhiteSpace(tag))
            {
                await _botClient.SendMessageAsync(user.TelegramId, "Tag is empty", cancellationToken: cancellationToken);
                return;
            }
            await _userService.UpdateAutoTag(user, tag);
            await _botClient.SendMessageAsync(user.TelegramId, $"Tagging everything as {tag} for {user.AutoTagValidityMinutes} minutes", cancellationToken: cancellationToken);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error while handling autotag command");
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling autotag command", cancellationToken: cancellationToken);
        }
    }

    private async Task RunImport(User user, UserState state, Document updateMessageDocument, CancellationToken cancellationToken)
    {
        if (state.ImportParameters == null)
        {
            _logger.LogError("Import parameters for input are null");
            return;
        }
        
        State[user.TelegramId] = state with {IsImporting = false, ImportParameters = null};
        using var scope = _serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
        await importService.HandleImport(user, state.ImportParameters, updateMessageDocument, cancellationToken);
    }
    
    private async Task RunExport(User user, RecordType recordType, DateOnly from, DateOnly till, bool includeIgnored, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var export = scope.ServiceProvider.GetRequiredService<ExportService>();
        await export.HandleExport(user, recordType, from, till, includeIgnored, cancellationToken);
    }
}