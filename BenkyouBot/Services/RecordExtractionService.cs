using System.Globalization;
using System.Text;
using System.Text.Json;
using Benkyou.DAL;
using Benkyou.DAL.Entities;
using Benkyou.DAL.Services;
using Newtonsoft.Json;
using Telegram.BotAPI.AvailableTypes;
using User = Benkyou.DAL.Entities.User;

namespace BenkyouBot.Services;

public class RecordExtractionService
{
    private readonly RecordService _recordService;
    private readonly BenkyouDbContext _dbContext;

    public RecordExtractionService(RecordService recordService)
    {
        _recordService = recordService;
    }

    public async Task ProcessMessage(User user, Message message)
    {
        foreach (var (content, type) in ExtractRecords(message.Text ?? string.Empty))
        {
            await CreateOrUpdateRecord(user, true, content, type, DateTime.UtcNow);
        }
    }

    public async Task ImportTgHistory(User user, byte[] updateMessageDocument, CancellationToken cancellationToken, bool addScore)
    {
        var root = JsonDocument.Parse(updateMessageDocument).RootElement;
        var messages = root.GetProperty("messages").EnumerateArray();
        var records = new List<(DateTime date, string content, RecordType type)>();
        foreach (var message in messages)
        {
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

        foreach (var (date, content, type) in records)
        {
            await CreateOrUpdateRecord(user, addScore, content, type, date);
        }
    }



    public async Task ImportJishoHistory(User user, byte[] updateMessageDocument, CancellationToken cancellationToken, bool addScore, DateOnly assumedDate)
    {
        throw new NotImplementedException();
    }
    private async Task CreateOrUpdateRecord(User user, bool addScore, string content, RecordType type, DateTime date)
    {
        var existingRecord = await _recordService.GetRecordByContent(user.UserId, content, type);
        if (existingRecord is not null)
        {
            if (!addScore)
            {
                return;
            }

            await _recordService.UpdateRecord(existingRecord,
                date > existingRecord.UpdatedAt ? date : existingRecord.UpdatedAt, existingRecord.Score + 1);
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
        }
    }
    
    private IReadOnlyList<(string content, RecordType type)> ExtractRecords(string text)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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