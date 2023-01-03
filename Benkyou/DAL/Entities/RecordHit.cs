namespace Benkyou.DAL.Entities;

public class RecordHit
{
    public int RecordHitId { get; set; }
    public int RecordId { get; set; }
    public Record Record { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public bool Ignored { get; set; }
    public int HitScore { get; set; }
}