using Benkyou.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Benkyou.DAL.Services;

public class TagService
{
    private readonly BenkyouDbContext _context;

    public TagService(BenkyouDbContext context)
    {
        _context = context;
    }

    public async Task<Tag> TagRecordsAsync(int userId, string name, IReadOnlyCollection<Record> records)
    {
        var tag = _context.Tags.Include(t => t.Records).FirstOrDefault(t => t.UserId == userId && t.Name == name);
        if (tag != null)
        {
            return await TagRecordsAsync(tag, records);
        }
        
        tag = new Tag
        {
            UserId = userId,
            Name = name,
            Description = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Records = records.ToList()
        };
        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<Tag> TagRecordsAsync(Tag tag, IReadOnlyCollection<Record> records)
    {
        foreach (var record in records)
        {
            if (!tag.Records.Contains(record))
            {
                tag.Records.Add(record);
            }
        }
        
        tag.UpdatedAt = DateTime.UtcNow;
        _context.Tags.Update(tag);
        await _context.SaveChangesAsync();
        return tag;
    }

    public async Task<List<Tag>> GetTagsAsync(int userId)
    {
        return await _context.Tags.Where(t => t.UserId == userId).ToListAsync();
    }

}