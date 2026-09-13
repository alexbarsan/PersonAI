using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DreamLens.Api.Features.Content;

public sealed class DailyDreamContentSeedService(
    IServiceScopeFactory scopeFactory,
    ILogger<DailyDreamContentSeedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var seeder = scope.ServiceProvider.GetRequiredService<DailyDreamContentSeeder>();
        await seeder.EnsureCoverageAsync(DateOnly.FromDateTime(DateTime.UtcNow), cancellationToken);
        logger.LogInformation("Daily dream content coverage is seeded through the next thirteen months.");
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
