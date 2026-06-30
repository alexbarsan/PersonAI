using DreamLens.Api.Features.Health;
using Npgsql;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class PostgresDatabaseReadinessProbe(IConfiguration configuration) : IDatabaseReadinessProbe
{
    public async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
    {
        var connectionString = configuration.GetConnectionString("DreamLensDb");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return true;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(2));

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(timeout.Token);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
