using ERMS.Application.Interfaces;
using ERMS.Infrastructure.Files;
using ERMS.Infrastructure.Persistence;
using ERMS.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace ERMS.Infrastructure;
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, string contentRoot)
    {
        var provider = configuration["Database:Provider"] ?? "Sqlite";
        services.AddDbContext<AppDbContext>(options =>
        {
            if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            else
                options.UseSqlite(configuration.GetConnectionString("SqliteConnection") ?? "Data Source=erms.db");
        });
        services.AddScoped<IErmsRepository, ErmsRepository>().AddSingleton<IPasswordService, PasswordService>().AddSingleton<ITokenService, TokenService>();
        var uploadRoot = Path.GetFullPath(Path.Combine(contentRoot, "..", "..", "uploads"));
        services.AddSingleton<IFileStorage>(new LocalFileStorage(uploadRoot)); return services;
    }
}
