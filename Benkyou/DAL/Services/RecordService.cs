using Benkyou.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Benkyou.DAL.Services;

public class RecordService
{
    private readonly BenkyouDbContext _dbContext;

    public RecordService(BenkyouDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task CreateRecordsAsync(long userId, IEnumerable<Record> records)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null)
        {
            throw new ArgumentException($"User with id {userId} not found");
        }

        foreach (var record in records)
        {
            record.User = user;
            _dbContext.Records.Add(record);
        }

        await _dbContext.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Record>> GetRecords(Guid userId, RecordType recordType, DateOnly from, DateOnly to, bool showIgnored)
    {
        var query = _dbContext.Records
            .Where(r => r.UserId == userId &&
                         r.UpdatedAt >= from.ToDateTime(TimeOnly.MinValue).ToUniversalTime() &&
                         r.UpdatedAt <= to.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

        if (recordType != RecordType.Any)
        {
            query = query.Where(r => r.RecordType == recordType);
        }

        if (!showIgnored)
        {
            query = query.Where(r => !r.Ignored);
        }

        return await query
            .ToListAsync();
    }

    public async Task<IReadOnlyList<Record>> GetTopRecordsByDate(Guid userId, int count, RecordType recordType, DateOnly from, DateOnly to, bool showIgnored)
    {
        var query = _dbContext.Records
            .Where(r => r.UserId == userId &&
                         r.UpdatedAt >= from.ToDateTime(TimeOnly.MinValue).ToUniversalTime() &&
                         r.UpdatedAt <= to.ToDateTime(TimeOnly.MinValue).ToUniversalTime());

        if (recordType != RecordType.Any)
        {
            query = query.Where(r => r.RecordType == recordType);
        }

        if (!showIgnored)
        {
            query = query.Where(r => !r.Ignored);
        }

        return await query
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.UpdatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Record?> GetRecordByContent(Guid userId, string content, RecordType type)
    {
        return await _dbContext.Records
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Content == content && r.RecordType == type);
    }

    public async Task UpdateRecord(Record existingRecord, DateTime updatedDate, int updatedScore, bool ignored, bool addHit)
    {
        var record = await _dbContext.Records
            .Include(r => r.Hits)
            .FirstOrDefaultAsync(r => r.Id == existingRecord.Id);

        if (record is null)
        {
            throw new ArgumentException($"Record with id {existingRecord.Id} not found");
        }
        record.UpdatedAt = updatedDate;
        record.Score = updatedScore;
        record.Ignored = ignored;
        if (addHit)
        {
            record.Hits.Add(new RecordHit
            {
                CreatedAt = updatedDate,
                Ignored = ignored,
                HitScore = updatedScore
            });
        }
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRecord(Record record)
    {
        _dbContext.Records.Add(record);
        await _dbContext.SaveChangesAsync();
    }
}
