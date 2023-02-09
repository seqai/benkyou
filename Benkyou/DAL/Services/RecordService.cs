using Benkyou.DAL.Entities;
using Benkyou.DAL.Filters;
using Benkyou.Infrastructure;
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

    public async Task<QueryPage<Record>> GetRecords(Guid userId, RecordFilter filter, bool showIgnored, int skip = 0, int take = 0)
    {
        var query = _dbContext.Records
            .Include(r => r.Tags)
            .Include(r => r.Hits)
            .Where(r => r.UserId == userId &&
                        r.UpdatedAt >= filter.FromDate.ToUniversalTime() &&
                        r.UpdatedAt <= filter.ToDate.ToUniversalTime());

        if (filter.RecordTypes.Count > 0 && !filter.RecordTypes.Contains(RecordType.Any))
        {
            query = query.Where(r => filter.RecordTypes.Contains(r.RecordType));
        }

        if (!string.IsNullOrWhiteSpace(filter.Content))
        {
            query = query.Where(r => r.Content.Contains(filter.Content));
        }

        if (filter.Tags.Count > 0)
        {
            query = query.Where(r => r.Tags.Any(t => filter.Tags.Contains(t.Name)));
        }

        if (!showIgnored)
        {
            query = query.Where(r => !r.Ignored);
        }
        
        var count = await query.CountAsync();

        query = filter.SortField switch
        {
            RecordSortField.Content => query.OrderBy(r => r.Content, descending: filter.SortDescending),
            RecordSortField.Type => query.OrderBy(r => r.RecordType, descending: filter.SortDescending),
            RecordSortField.Created => query.OrderBy(r => r.CreatedAt, descending: filter.SortDescending),
            RecordSortField.Updated => query.OrderBy(r => r.UpdatedAt, descending: filter.SortDescending),
            RecordSortField.Hits => query.OrderBy(r => r.Hits.Count, descending: filter.SortDescending),
            RecordSortField.Tags => query.OrderBy(r => r.Tags, descending: filter.SortDescending),
            RecordSortField.Default => query,
            _ => throw new ArgumentOutOfRangeException()
        };

        if (skip > 0)
        {
            query = query.Skip(skip);
        }

        if (take > 0)
        {
            query = query.Take(take);
        }

        var items = await query.ToListAsync();
        return new QueryPage<Record>(items, count, skip, take);
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

    public async Task<bool> RemoveRecord(Record record)
    {
        var result = _dbContext.Records.Remove(record);
        await _dbContext.SaveChangesAsync();
        return result.State == EntityState.Deleted;
    }
}
