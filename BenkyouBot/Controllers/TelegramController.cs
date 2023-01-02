using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Benkyou.DAL.Entities;
using Benkyou.DAL.Services;
using BenkyouBot.Services;
using Microsoft.AspNetCore.Mvc;
using Telegram.BotAPI;
using Telegram.BotAPI.AvailableMethods;
using Telegram.BotAPI.AvailableTypes;
using Telegram.BotAPI.GettingUpdates;
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
        if (state.IsImporting && updateMessage.Document is not null)
        {
            await _botClient.SendMessageAsync(user.TelegramId, "Importing... (this might take some time)", cancellationToken: cancellationToken);
            RunImport(user, state, updateMessage.Document, cancellationToken);
        }
        else
        {
            await _recordExtractionService.ProcessMessage(user, updateMessage);
        }
    }

    private async Task RunImport(User user, UserState state, Document updateMessageDocument, CancellationToken cancellationToken)
    {
        State[user.TelegramId] = state with { IsImporting = false, Args = string.Empty };
        using var scope = _serviceProvider.CreateScope();
        var importService = scope.ServiceProvider.GetRequiredService<ImportService>();
        await importService.HandleImport(user, state.Args, updateMessageDocument, cancellationToken);
    }

    private async Task HandleCommand(User user, string message, CancellationToken cancellationToken)
    {
        var tokens = message.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var command = tokens[0].ToLower();
        var args = tokens.Length > 1 ? tokens[1] : string.Empty;
        switch (command)
        {
            case "/cancel":
                State.Remove(user.TelegramId, out _);
                break;
            case "/top":
                await HandleTopCommand(user, args, cancellationToken);
                break;
            case "/import":
                await _botClient.SendMessageAsync(user.TelegramId, "Send me the file to import", cancellationToken: cancellationToken);
                State[user.TelegramId] = new UserState(true, args);
                break;
        }
    }

    private async Task HandleTopCommand(User user, string args, CancellationToken cancellationToken)
    {
        try
        {
            var tokens = args.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var count = tokens.Length > 0 ? int.Parse(tokens[0]) : 10;
            var type = tokens.Length > 1 ? Enum.TryParse(tokens[1], true, out RecordType recordType) ? recordType : RecordType.Any : RecordType.Any;
            var from = tokens.Length > 2 ? DateOnly.ParseExact(tokens[2], "yyyy-MM-dd") : DateOnly.MinValue;
            var to = tokens.Length > 3 ? DateOnly.ParseExact(tokens[3], "yyyy-MM-dd") : DateOnly.MaxValue;

            var top = await _recordService.GetTopRecordsByDate(user.UserId, count, type, from, to);
            var sb = new StringBuilder();
            foreach (var record in top)
            {
                sb.AppendLine($"{record.Score}\t{record.UpdatedAt:yyyy-MM-dd}\t{record.Content}");
            }
            await _botClient.SendMessageAsync(user.TelegramId, sb.ToString(), cancellationToken: cancellationToken);

        }
        catch (Exception e)
        {
            await _botClient.SendMessageAsync(user.TelegramId, "Error while handling top command", cancellationToken: cancellationToken);
        }

    }

    [HttpGet("test")]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("Test");
        return Ok("Test");
    }


    private record UserState(bool IsImporting, string Args) {
        public static UserState None = new(false, string.Empty);
    }
}