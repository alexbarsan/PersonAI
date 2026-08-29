using System.Text.Json;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class AsyncJobService(
    DreamLensDbContext dbContext,
    IAsyncJobQueue queue)
{
    public async Task<AsyncJobRecord> EnqueueAsync(
        string idempotencyKey,
        string jobType,
        string userSubject,
        Guid? targetId,
        object payload,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.AsyncJobs
            .SingleOrDefaultAsync(job => job.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == AsyncJobStatuses.Failed)
            {
                await RequeueAsync(existing, cancellationToken);
            }

            return existing;
        }

        var job = new AsyncJobRecord
        {
            IdempotencyKey = idempotencyKey,
            JobType = jobType,
            UserSubject = userSubject,
            TargetId = targetId,
            PayloadJson = JsonSerializer.Serialize(payload)
        };

        dbContext.AsyncJobs.Add(job);
        await dbContext.SaveChangesAsync(cancellationToken);

        await queue.PublishAsync(
            new AsyncJobMessage(job.Id, job.JobType, job.UserSubject, job.PayloadJson),
            cancellationToken);

        return job;
    }

    public async Task<AsyncJobRecord?> RetryFailedAsync(
        Guid jobId,
        string userSubject,
        CancellationToken cancellationToken)
    {
        var job = await dbContext.AsyncJobs.SingleOrDefaultAsync(
            candidate => candidate.Id == jobId
                && candidate.UserSubject == userSubject
                && candidate.Status == AsyncJobStatuses.Failed,
            cancellationToken);

        if (job is null)
        {
            return null;
        }

        await RequeueAsync(job, cancellationToken);
        return job;
    }

    private async Task RequeueAsync(AsyncJobRecord job, CancellationToken cancellationToken)
    {
        job.Status = AsyncJobStatuses.Pending;
        job.AttemptCount = 0;
        job.AvailableAt = DateTimeOffset.UtcNow;
        job.LockedUntil = null;
        job.CompletedAt = null;
        job.LastError = null;
        job.UpdatedAt = DateTimeOffset.UtcNow;
        await dbContext.SaveChangesAsync(cancellationToken);

        await queue.PublishAsync(
            new AsyncJobMessage(job.Id, job.JobType, job.UserSubject, job.PayloadJson),
            cancellationToken);
    }
}
