using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.AdminMetrics;

public sealed class GetAdminMetricsHandler(DreamLensDbContext dbContext, IEntitlementService entitlementService)
{
    public async Task<AdminMetricsResponse> HandleAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken)
    {
        var end = to ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var start = from ?? end.AddDays(-29);
        if (start > end) throw new ArgumentException("The from date must not be after the to date.");

        var startAt = start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endExclusive = end.AddDays(1).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var dreams = await dbContext.Dreams.AsNoTracking()
            .Where(dream => dream.CreatedAt >= startAt && dream.CreatedAt < endExclusive)
            .Select(dream => new { dream.UserSubject, dream.Status })
            .ToArrayAsync(cancellationToken);
        var ledgerRows = await dbContext.AiCostLedger.AsNoTracking()
            .Where(row => row.CreatedAt >= startAt && row.CreatedAt < endExclusive)
            .Select(row => new { row.OperationType, row.Status, row.EstimatedCostUsd, row.LatencyMilliseconds })
            .ToArrayAsync(cancellationToken);

        var activeSubjects = dreams.Select(dream => dream.UserSubject).Distinct(StringComparer.Ordinal).ToArray();
        var premiumActiveUsers = activeSubjects.Count(subject => entitlementService.GetEntitlement(subject).Tier == EntitlementTier.Premium);
        var submitted = dreams.Length;
        var completed = dreams.Count(dream => string.Equals(dream.Status, "completed", StringComparison.Ordinal));
        var aiCost = Math.Round(ledgerRows.Sum(row => row.EstimatedCostUsd), 6);
        var byOperation = ledgerRows
            .GroupBy(row => row.OperationType, StringComparer.Ordinal)
            .OrderByDescending(group => group.Sum(row => row.EstimatedCostUsd))
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group =>
            {
                var latencies = group.Select(row => row.LatencyMilliseconds).OrderBy(value => value).ToArray();
                return new AdminMetricCostBreakdownResponse(
                    group.Key,
                    latencies.Length,
                    group.Count(row => string.Equals(row.Status, "completed", StringComparison.Ordinal)),
                    Math.Round(group.Sum(row => row.EstimatedCostUsd), 6),
                    latencies.Length == 0 ? 0 : (long)Math.Round(latencies.Average()),
                    latencies.Length == 0 ? 0 : latencies[(int)Math.Ceiling(latencies.Length * 0.95) - 1]);
            })
            .ToArray();

        return new AdminMetricsResponse(
            start,
            end,
            activeSubjects.Length,
            premiumActiveUsers,
            Percent(premiumActiveUsers, activeSubjects.Length),
            submitted,
            completed,
            Percent(completed, submitted),
            aiCost,
            activeSubjects.Length == 0 ? 0 : Math.Round(aiCost / activeSubjects.Length, 6),
            byOperation,
            Unavailable("RevenueCat purchase validation is not connected."),
            Unavailable("AWS Cost Explorer ingestion is not connected."),
            Unavailable("Revenue and AWS cost are required before gross margin can be calculated."));
    }

    private static decimal Percent(int numerator, int denominator) => denominator == 0 ? 0 : Math.Round(numerator * 100m / denominator, 1);

    private static AdminMetricUnavailableValueResponse Unavailable(string reason) => new("unavailable", reason);
}
