using System.Diagnostics.CodeAnalysis;

namespace Benkyou.DAL.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public long TelegramId { get; set; }
    public RecordType DefaultRecordType { get; set; } = RecordType.Any;
    public virtual ICollection<Record> Records { get; set; }
    public virtual ICollection<Tag> Tags { get; set; }
    public string? AutoTag { get; set; }
    public DateTime AutoTagValidFrom { get; set; }
    public int AutoTagValidityMinutes { get; set; } = 60;

    [MemberNotNullWhen(true, nameof(AutoTag))]
    public bool IsAutoTagValid(DateTime dateTime) => !string.IsNullOrWhiteSpace(AutoTag) && dateTime - AutoTagValidFrom < TimeSpan.FromMinutes(AutoTagValidityMinutes);
}