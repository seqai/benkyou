public class TelegramOptions
{
    public string BotToken { get; set; } = string.Empty;
    public string WebHookUrl { get; set; } = string.Empty;
    public long AdminId { get; set; } = -1;
    public string AdminName { get; set; }
}
