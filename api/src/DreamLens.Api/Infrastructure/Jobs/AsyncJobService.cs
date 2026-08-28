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
}
