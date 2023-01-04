namespace Benkyou.DAL.Entities;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; }
    public long TelegramId { get; set; }
    public RecordType DefaultRecordType { get; set; } = RecordType.Any;
    public virtual ICollection<Record> Records { get; set; }
}