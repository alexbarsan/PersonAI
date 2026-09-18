using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamJournalSynthesisScheduler(
    IOptions<AsyncJobWorkerOptions> workerOptions,
    IOptions<DreamJournalSynthesisOptions> synthesisOptions,
    IServiceScopeFactory scopeFactory,
    ILogger<DreamJournalSynthesisScheduler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!workerOptions.Value.Enabled || !synthesisOptions.Value.Enabled)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var service = scope.ServiceProvider.GetRequiredService<DreamJournalSynthesisService>();
                var enqueued = await service.EnqueueStaleAsync(stoppingToken);
                logger.LogInformation("Journal synthesis scheduler enqueued {JobCount} jobs.", enqueued);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Journal synthesis scheduler failed.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(Math.Clamp(synthesisOptions.Value.SchedulerIntervalMinutes, 15, 1440)),
                stoppingToken);
        }
    }
}
