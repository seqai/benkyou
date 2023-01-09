using Benkyou.DAL.Entities;
using Microsoft.EntityFrameworkCore;

namespace Benkyou.DAL.Services;

public class UserService
{
    private readonly BenkyouDbContext _dbContext;

    public UserService(BenkyouDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<User?> GetUserByTelegramId(long telegramId)
    {
        return await _dbContext.Users
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId);
    }

    public async Task<User> CreateUser(long telegramId, string username)
    {
        var user = new User
        {
            TelegramId = telegramId,
            Username = username
        }; 
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();
        return user;
    }

    public async Task UpdateDefaults(User user, RecordType recordType, int autoTagValidityMinutes)
    {
        user.DefaultRecordType = recordType;
        user.AutoTagValidityMinutes = autoTagValidityMinutes;
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAutoTag(User user, string tag)
    {
        user.AutoTag = tag;
        user.AutoTagValidFrom = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();
    }
}