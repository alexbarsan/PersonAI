using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Monetization;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Quotas;

public sealed class EfDreamQuotaService(
    DreamLensDbContext dbContext,
    IEntitlementService entitlementService) : IDreamQuotaService
{
    public async Task<bool> CanSubmitDreamAsync(string userSubject, CancellationToken cancellationToken)
    {
        var limit = entitlementService.GetEntitlement(userSubject).DailyDreamLimit;
        if (limit <= 0)
        {
            return false;
        }

        var start = DateTimeOffset.UtcNow.Date;
        var todayStart = new DateTimeOffset(start, TimeSpan.Zero);
        var tomorrowStart = todayStart.AddDays(1);
        var submissionCount = await dbContext.Dreams.CountAsync(
            dream => dream.UserSubject == userSubject
                && dream.CreatedAt >= todayStart
                && dream.CreatedAt < tomorrowStart
                && dream.Status != "failed",
            cancellationToken);

        return submissionCount < limit;
    }
}
