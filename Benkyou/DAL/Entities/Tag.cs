namespace Benkyou.DAL.Entities;

public class Tag
{
    public int TagId { get; set; }
    public Guid UserId { get; set; }
    public int? AutoTagId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public User User { get; set; }
    public ICollection<Record> Records { get; set; }
}