using Benkyou.DAL;
using Benkyou.DAL.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Benkyou;

public static class DbInfrastructure
{
    public static void RegisterBenkyoDAL(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<BenkyouDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<RecordService>();
        services.AddScoped<UserService>();
    }
    
}