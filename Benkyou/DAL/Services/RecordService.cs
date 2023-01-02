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

    public async Task<IEnumerable<Record>> GetRecordsAsync(long userId)
    {
        var user = await _dbContext.Users.FindAsync(userId);
        if (user is null)
        {
            throw new ArgumentException($"User with id {userId} not found");
        }

        return user.Records;
    }

    public async Task<IReadOnlyList<Record>> GetTopRecordsByDate(long userId, int count, RecordType recordType, DateOnly from, DateOnly to)
    {
        return await _dbContext.Records
            .Where(r => r.UserId == userId &&
                        (recordType == RecordType.Any || r.RecordType == recordType) &&
                         r.UpdatedAt >= from.ToDateTime(TimeOnly.MinValue).ToUniversalTime() &&
                         r.UpdatedAt <= to.ToDateTime(TimeOnly.MinValue).ToUniversalTime())
            .OrderByDescending(r => r.Score)
            .ThenByDescending(r => r.UpdatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<Record?> GetRecordByContent(long userId, string content, RecordType type)
    {
        return await _dbContext.Records
            .FirstOrDefaultAsync(r => r.UserId == userId && r.Content == content && r.RecordType == type);
    }

    public async Task UpdateRecord(Record existingRecord, DateTime updatedDate, int updatedScore)
    {
        var record = _dbContext.Records.Find(existingRecord.RecordId);
        if (record is null)
        {
            throw new ArgumentException($"Record with id {existingRecord.RecordId} not found");
        }
        record.UpdatedAt = updatedDate;
        record.Score = updatedScore;
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddRecord(Record record)
    {
        _dbContext.Records.Add(record);
        await _dbContext.SaveChangesAsync();
    }
}
