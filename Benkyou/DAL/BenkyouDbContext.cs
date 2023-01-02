using Benkyou.DAL.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Benkyou.DAL;

public class BenkyouDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<Record> Records { get; set; }

    public BenkyouDbContext(DbContextOptions<BenkyouDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Records)
            .WithOne(r => r.User)
            .IsRequired()
            .HasForeignKey(r => r.UserId);
        
        modelBuilder.Entity<Record>().HasKey(r => r.RecordId);
        modelBuilder.Entity<User>().HasKey(u => u.UserId);
        modelBuilder.Entity<User>().HasIndex(u => u.TelegramId).IsUnique();
        modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<Record>().HasIndex(r => new { r.UserId, r.Content }).IsUnique();
    }
}

public class BenkyoDbContextFactory : IDesignTimeDbContextFactory<BenkyouDbContext>
{
    public BenkyouDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BenkyouDbContext>();
        var connectionString = args
            .Where(a => a.StartsWith("connectionString=", StringComparison.InvariantCultureIgnoreCase))
            .Select(a => a.Replace("connectionString=", "", StringComparison.InvariantCultureIgnoreCase))
            .FirstOrDefault();
        optionsBuilder.UseNpgsql(connectionString);

        return new BenkyouDbContext(optionsBuilder.Options);
    }
}
