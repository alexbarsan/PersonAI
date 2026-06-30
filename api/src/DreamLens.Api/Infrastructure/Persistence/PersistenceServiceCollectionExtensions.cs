using DreamLens.Api.Features.Health;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DreamLensDb");

        services.AddSingleton<IDatabaseReadinessProbe, PostgresDatabaseReadinessProbe>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            services.AddDbContext<DreamLensDbContext>(options => options.UseNpgsql(connectionString));
        }

        return services;
    }
}
