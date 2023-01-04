using Benkyou.Infrastructure;

namespace Benkyou.DAL.Entities;

public enum RecordType
{
    [EnumStringAlias("k")]
    Kanji,
    [EnumStringAlias("v", "vocab", "vocab", "word", "w")]
    Vocabulary,
    [EnumStringAlias("g")]
    Grammar,
    [EnumStringAlias("s")]
    Sentence,
    Any
}