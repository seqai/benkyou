using Benkyou.Infrastructure;

namespace BenkyouBot.Model;

public enum ImportType
{
    Unknown,
    [EnumStringAlias("tg")]
    TelegramHistory,
    Csv
}