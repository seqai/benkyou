namespace Benkyou.DAL.Entities;

public class Record
{
    public int Id { get; set; }
    public Guid UserId { get; set; }
    public string Content { get; set; }
    public RecordType RecordType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; }
    public int Score { get; set; }
    public bool Ignored { get; set; }
    public virtual ICollection<RecordHit> Hits { get; set; }
    public virtual ICollection<Tag> Tags { get; set; }
}