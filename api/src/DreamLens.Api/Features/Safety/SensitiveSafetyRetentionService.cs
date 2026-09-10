using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Safety;

public sealed class SensitiveSafetyRetentionService(IServiceScopeFactory scopeFactory, ILogger<SensitiveSafetyRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await PurgeExpiredAsync(stoppingToken);
            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }

    private async Task PurgeExpiredAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DreamLensDbContext>();
        var expired = await dbContext.SensitiveDreamSafetyEvents
            .Where(review => review.ExpiresAt <= DateTimeOffset.UtcNow)
            .ToArrayAsync(cancellationToken);
        if (expired.Length == 0)
        {
            return;
        }

        var eventIds = expired.Select(review => review.Id).ToArray();
        var notifications = await dbContext.SensitiveReviewNotifications
            .Where(notification => eventIds.Contains(notification.SafetyEventId))
            .ToArrayAsync(cancellationToken);
        var audits = await dbContext.SensitiveReviewAccessAudits
            .Where(audit => eventIds.Contains(audit.SafetyEventId))
            .ToArrayAsync(cancellationToken);
        dbContext.SensitiveReviewNotifications.RemoveRange(notifications);
        dbContext.SensitiveReviewAccessAudits.RemoveRange(audits);
        dbContext.SensitiveDreamSafetyEvents.RemoveRange(expired);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Purged {Count} expired sensitive safety review events.", expired.Length);
    }
}
