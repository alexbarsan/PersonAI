using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Infrastructure.Quotas;

public sealed class EfDreamQuotaService(
    DreamLensDbContext dbContext,
    IOptions<DreamQuotaOptions> options) : IDreamQuotaService
{
    public async Task<bool> CanSubmitDreamAsync(string userSubject, CancellationToken cancellationToken)
    {
        var limit = options.Value.DailyDreamSubmissions;
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
                && dream.CreatedAt < tomorrowStart,
            cancellationToken);

        return submissionCount < limit;
    }
}
