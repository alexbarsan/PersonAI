using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.AdminOperations;

public sealed class GetAdminOperationsHandler(
    DreamLensDbContext dbContext,
    IOperationsQueueMonitor queueMonitor)
{
    public async Task<AdminOperationsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var queue = await queueMonitor.GetSnapshotAsync(cancellationToken);
        var jobs = await dbContext.AsyncJobs.AsNoTracking().ToArrayAsync(cancellationToken);
        var classifications = await dbContext.DreamImageSafety.AsNoTracking().ToArrayAsync(cancellationToken);
        var images = await dbContext.DreamImages.AsNoTracking().ToArrayAsync(cancellationToken);
        var voices = await dbContext.VoiceCaptures.AsNoTracking().ToArrayAsync(cancellationToken);
        var dreams = await dbContext.Dreams.AsNoTracking().ToArrayAsync(cancellationToken);
        var privacyRequests = await dbContext.AnonymizationRequests.AsNoTracking().ToArrayAsync(cancellationToken);
        var safetyReviews = await dbContext.SensitiveDreamSafetyEvents.AsNoTracking().ToArrayAsync(cancellationToken);
        var recentLedger = await dbContext.AiCostLedger.AsNoTracking()
            .Where(row => row.CreatedAt >= now.AddHours(-24))
            .ToArrayAsync(cancellationToken);
        var acknowledgements = await dbContext.OperationsActionAudits.AsNoTracking()
            .Where(audit => audit.Action == "acknowledge")
            .GroupBy(audit => new { audit.Source, audit.TargetId })
            .Select(group => new { group.Key.Source, group.Key.TargetId, At = group.Max(audit => audit.CreatedAt) })
            .ToArrayAsync(cancellationToken);
        var acknowledged = acknowledgements.ToDictionary(item => (item.Source, item.TargetId), item => item.At);

        var issues = jobs
            .Where(job => job.Status == AsyncJobStatuses.Failed
                || job.Status == AsyncJobStatuses.Pending && job.UpdatedAt <= now.AddMinutes(-5)
                || job.Status == AsyncJobStatuses.Processing && job.LockedUntil < now)
            .Select(job => ToJobIssue(job, now, acknowledged.ContainsKey(("job", job.Id))))
            .Concat(classifications
                .Where(item => item.Status == DreamImageSafetyStatuses.Failed)
                .Where(item => !jobs.Any(job => job.TargetId == item.Id && job.Status == AsyncJobStatuses.Failed))
                .Select(item => ToClassificationIssue(
                    item,
                    jobs.SingleOrDefault(job => job.TargetId == item.Id && job.JobType == AsyncJobTypes.DreamImageSafety),
                    now,
                    acknowledged.ContainsKey(("image-safety", item.Id)))))
            .Concat(dreams
                .Where(dream => dream.Status == "failed")
                .Select(dream => new AdminOperationsIssueResponse(
                    dream.Id,
                    null,
                    "dream",
                    "dream.interpretation",
                    dream.Status,
                    1,
                    AgeSeconds(dream.CreatedAt, now),
                    SanitizeFailure(dream.ErrorMessage),
                    acknowledged.ContainsKey(("dream", dream.Id)),
                    false,
                    dream.CreatedAt)))
            .OrderByDescending(issue => issue.Status == AsyncJobStatuses.Failed)
            .ThenByDescending(issue => issue.AgeSeconds)
            .Take(100)
            .ToArray();

        return new AdminOperationsResponse(
            now,
            new AdminOperationsQueueResponse(queue.Status, queue.Available, queue.InFlight, queue.Delayed, queue.DeadLetter, queue.Error),
            new AdminOperationsJobSummaryResponse(
                jobs.Count(job => job.Status == AsyncJobStatuses.Pending),
                jobs.Count(job => job.Status == AsyncJobStatuses.Processing),
                jobs.Count(job => job.Status == AsyncJobStatuses.Failed),
                OldestAge(jobs.Where(job => job.Status == AsyncJobStatuses.Pending).Select(job => job.CreatedAt), now)),
            [
                Workload("image-safety", classifications.Select(item => new WorkState(item.Status, item.UpdatedAt)), now),
                Workload("dream-image", images.Select(item => new WorkState(item.Status, item.UpdatedAt)), now),
                Workload("voice", voices.Select(item => new WorkState(item.Status, item.UpdatedAt)), now),
                Workload("dream-interpretation", dreams.Select(item => new WorkState(item.Status, item.CreatedAt)), now),
                Workload("privacy-request", privacyRequests.Select(item => new WorkState(item.Status, item.RequestedAt)), now),
                Workload("safety-review", safetyReviews.Select(item => new WorkState(item.Status == "open" ? "pending" : item.Status, item.DetectedAt)), now)
            ],
            issues,
            recentLedger
                .GroupBy(row => new { row.Provider, row.OperationType })
                .OrderByDescending(group => group.Count(row => row.Status == "failed"))
                .ThenByDescending(group => group.Count())
                .Select(group =>
                {
                    var latencies = group.Select(row => row.LatencyMilliseconds).OrderBy(value => value).ToArray();
                    return new AdminOperationsProviderResponse(
                        group.Key.Provider,
                        group.Key.OperationType,
                        group.Count(),
                        group.Count(row => row.Status == "failed"),
                        Math.Round(group.Sum(row => row.EstimatedCostUsd), 6),
                        latencies.Length == 0 ? 0 : (long)Math.Round(latencies.Average()),
                        Percentile95(latencies));
                })
                .ToArray());
    }

    private static AdminOperationsIssueResponse ToJobIssue(AsyncJobRecord job, DateTimeOffset now, bool acknowledged) => new(
        job.Id,
        job.Id,
        "job",
        job.JobType,
        job.Status,
        job.AttemptCount,
        AgeSeconds(job.UpdatedAt, now),
        SanitizeFailure(job.LastError),
        acknowledged,
        true,
        job.UpdatedAt);

    private static AdminOperationsIssueResponse ToClassificationIssue(
        DreamImageSafetyRecord classification,
        AsyncJobRecord? job,
        DateTimeOffset now,
        bool acknowledged) => new(
            classification.Id,
            job?.Id,
            "image-safety",
            AsyncJobTypes.DreamImageSafety,
            classification.Status,
            job?.AttemptCount ?? 0,
            AgeSeconds(classification.UpdatedAt, now),
            classification.FailureKind,
            acknowledged,
            job is not null,
            classification.UpdatedAt);

    private static AdminOperationsWorkloadResponse Workload(
        string source,
        IEnumerable<WorkState> states,
        DateTimeOffset now)
    {
        var items = states.ToArray();
        var active = items.Where(item => item.Status is "pending" or "processing" or "generating" or "transcribing").ToArray();
        return new AdminOperationsWorkloadResponse(
            source,
            items.Count(item => item.Status == "pending"),
            items.Count(item => item.Status is "processing" or "generating" or "transcribing"),
            items.Count(item => item.Status == "failed"),
            OldestAge(active.Select(item => item.UpdatedAt), now));
    }

    private static long? OldestAge(IEnumerable<DateTimeOffset> values, DateTimeOffset now)
    {
        var items = values.ToArray();
        return items.Length == 0 ? null : AgeSeconds(items.Min(), now);
    }

    private static long AgeSeconds(DateTimeOffset value, DateTimeOffset now) =>
        Math.Max(0, (long)(now - value).TotalSeconds);

    private static long Percentile95(long[] values) =>
        values.Length == 0 ? 0 : values[(int)Math.Ceiling(values.Length * 0.95) - 1];

    private static string? SanitizeFailure(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var singleLine = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return singleLine[..Math.Min(singleLine.Length, 240)];
    }

    private sealed record WorkState(string Status, DateTimeOffset UpdatedAt);
}
