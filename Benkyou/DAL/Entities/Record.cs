namespace Benkyou.DAL.Entities;

public class Record
{
    public int RecordId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; }
    public RecordType RecordType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; }
    public int Score { get; set; }
}

public enum RecordType
{
    Kanji,
    Vocabulary,
    Grammar,
    Sentence,
    Any
}