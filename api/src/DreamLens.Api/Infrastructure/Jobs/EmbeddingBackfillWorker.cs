using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class EmbeddingBackfillWorker(
    IOptions<EmbeddingBackfillOptions> options,
    IServiceScopeFactory scopeFactory,
    ILogger<EmbeddingBackfillWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var service = scope.ServiceProvider.GetRequiredService<EmbeddingBackfillService>();
        var enqueued = await service.EnqueueMissingAsync(stoppingToken);
        logger.LogInformation("Embedding backfill enqueued {JobCount} jobs.", enqueued);
    }
}
