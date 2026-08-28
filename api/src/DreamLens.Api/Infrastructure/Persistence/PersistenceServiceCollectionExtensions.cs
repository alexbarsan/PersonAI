using DreamLens.Api.Features.Health;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Persistence;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddDreamLensPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = ResolveConnectionString(configuration);

        services.AddSingleton<IDatabaseReadinessProbe, PostgresDatabaseReadinessProbe>();

        if (!string.IsNullOrWhiteSpace(connectionString))
        {
        services.AddDbContext<DreamLensDbContext>(options => options.UseNpgsql(connectionString, npgsql => npgsql.UseVector()));
        }

        return services;
    }

    public static string? ResolveConnectionString(IConfiguration configuration)
    {
        return FirstNonEmpty(
            configuration.GetConnectionString("DreamLensDb"),
            RdsMasterUserSecretConnectionStringFactory.Create(configuration));
    }

    private static string? FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
