using System.Globalization;
using System.Text.Json;
using Benkyou.DAL;
using Benkyou.DAL.Entities;
using Benkyou.DAL.Services;
using BenkyouBot.Infrastructure;
using Telegram.BotAPI.AvailableTypes;
using User = Benkyou.DAL.Entities.User;
using static Benkyou.Infrastructure.EnumHelpers;

namespace BenkyouBot.Services;

public class RecordExtractionService
{
    private readonly RecordService _recordService;
    private readonly BenkyouDbContext _dbContext;
    private readonly ILogger<RecordExtractionService> _logger;

    public RecordExtractionService(RecordService recordService, BenkyouDbContext dbContext, ILogger<RecordExtractionService> logger)
    {
        _recordService = recordService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(IReadOnlyCollection<Record> created, IReadOnlyCollection<Record> updated)> ProcessMessage(User user, Message message, CancellationToken cancellationToken)
    {
        try
        {
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            var createdRecords = new List<Record>();
            var updatedRecords = new List<Record>();
            foreach (var (content, type) in ExtractRecords(message.Text ?? string.Empty))
            {
                try
                {
                    var (record, created, updated) = await CreateOrUpdateRecord(user, true, content, type, DateTime.UtcNow);
                    if (created)
                    {
                        createdRecords.Add(record);
                    }
                    else if (updated)
                    {
                        updatedRecords.Add(record);
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to create or update record {Content} of type {Type}", content, type);
                    throw;
                }

            }
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
            return (createdRecords, updatedRecords);
        }
        catch
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task ImportTgHistory(User user, byte[] updateMessageDocument, CancellationToken cancellationToken, bool addScore)
    {
        var root = JsonDocument.Parse(updateMessageDocument).RootElement;
        var messages = root.GetProperty("messages").EnumerateArray();
        var records = new List<(DateTime date, string content, RecordType type)>();
        foreach (var message in messages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var date = DateTime.ParseExact(message.GetProperty("date").GetString(), "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal).ToUniversalTime();

            var textProperty = message.GetProperty("text");
            IEnumerable<JsonElement> textTokens = textProperty.ValueKind == JsonValueKind.String
                ? new[] { textProperty }
                : textProperty.EnumerateArray();
            foreach (var textToken in textTokens)
            {
                if (textToken.ValueKind == JsonValueKind.String)
                {
                    records.AddRange(ExtractRecords(textToken.GetString() ?? string.Empty).Select(x => (date, x.content, x.type)));
                }
                else if (textToken.ValueKind == JsonValueKind.Object)
                {
                    var type = textToken.GetProperty("type").GetString();
                    if (type != "link")
                    {
                        continue;
                    }
                    var href = textToken.TryGetProperty("href", out var hrefProperty) ? hrefProperty.GetString() : null;
                    if (href is null || !href.ToLowerInvariant().Contains("wanikani.com"))
                    {
                        continue;
                    }


                    var text = textToken.GetProperty("text").GetString() ?? string.Empty;
                    records.AddRange(ExtractRecords(text).Select(x => (date, x.content, x.type)));
                }
            }
        }

        try
        {
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            foreach (var (date, content, type) in records)
            {
                try
                {
                    await CreateOrUpdateRecord(user, addScore, content, type, date);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to import record {content} of type {type} on {date}", content, type, date);
                    throw;
                }
            }
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }


    public async Task ImportCsvHistory(
        User user, 
        byte[] updateMessageDocument, 
        bool addScore,
        int cotentColumnIndex, 
        int recordTypeColumnIndex, 
        int dateColumnIndex,
        DateOnly assumedDate, 
        CancellationToken cancellationToken)
    {
        var records = new List<(DateOnly date, string content, RecordType type)>();
        using var stream = new MemoryStream(updateMessageDocument);
        using var reader = new StreamReader(stream);
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            var values = line.Split(',');
            var content = values[cotentColumnIndex];
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            var date = assumedDate;
            if (dateColumnIndex > 0)
            {
                date = DateOnly.ParseExact(values[dateColumnIndex], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces);
            }
            else if (date == DateOnly.MinValue)
            {
                date = DateOnly.FromDateTime(DateTime.UtcNow);
            }

            var type = RecordType.Any;
            if (recordTypeColumnIndex > 0)
            {
                type = FromAlias(values[recordTypeColumnIndex], withDefaultName: true, withFallback: true, defaultValue: RecordType.Any);
            }

            if (type == RecordType.Any)
            {
                var extractedRecords = ExtractRecords(content);
                foreach (var (extractedContent, extractedType) in extractedRecords)
                {
                    records.Add((date, extractedContent, extractedType));
                }
            }
            else
            {
                records.Add((date, content, type));
            }
        }

        try
        {
            await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            foreach (var (date, content, type) in records)
            {
                try
                {
                    await CreateOrUpdateRecord(user, addScore, content, type,
                        date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc));
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Failed to import record {content} of type {type} on {date}", content, type, date);
                    throw;
                }
            }
            await _dbContext.Database.CommitTransactionAsync(cancellationToken);
        }
        catch
        {
            await _dbContext.Database.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    
    private async Task<(Record record, bool created, bool updated)> CreateOrUpdateRecord(User user, bool addScore, string content, RecordType type, DateTime date)
    {
        var existingRecord = await _recordService.GetRecordByContent(user.UserId, content, type);
        if (existingRecord is not null)
        {
            if (!addScore || existingRecord.Ignored)
            {
                return (existingRecord, false, false);
            }

            await _recordService.UpdateRecord(existingRecord,
                date > existingRecord.UpdatedAt ? date : existingRecord.UpdatedAt, existingRecord.Score + 1, existingRecord.Ignored);
            return (existingRecord, false, true);
        }
        else
        {
            var record = new Record
            {
                Content = content,
                RecordType = type,
                UserId = user.UserId,
                CreatedAt = date,
                UpdatedAt = date,
                Score = 1
            };
            await _recordService.AddRecord(record);
            return (record, true, false);
        }
    }
    
    private static IEnumerable<(string content, RecordType type)> ExtractRecords(string text)
    {
        var tokens = text.Tokenize();
        var records = new List<(string, RecordType)>();
        foreach (var token in tokens)
        {
            if (token.Any(c => !IsJapaneseChar(c)))
            {
                continue;
            }

            if (token.Length == 1 && IsKanji(token[0]))
            {
                records.Add((token, RecordType.Kanji));
            }
            else if (token.Length > 0)
            {
                records.Add((token, RecordType.Vocabulary));
                foreach (var kanji in token.Where(IsKanji))
                {
                    records.Add((kanji.ToString(), RecordType.Kanji));
                }
            }
        }
        return records;

    }

    // Hiragana 3040-309F
    // Katakana 30A0-30FF
    private static bool IsJapaneseChar(char c) => c >= 0x3040 && c <= 0x30FF || IsKanji(c);
    
    // CJK Unified Ideographs 3400-4DBF
    // CJK Unified Ideographs 4E00-9FFF
    // CJK Unified Ideographs F900-FAFF
    private static bool IsKanji(char c) => c >= 0x3400 && c <= 0x4DBF || c >= 0x4E00 && c <= 0x9FFF ||
                                    c >= 0xF900 && c <= 0xFAFF;

}