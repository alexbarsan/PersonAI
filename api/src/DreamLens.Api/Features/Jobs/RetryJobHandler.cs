using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;

namespace DreamLens.Api.Features.Jobs;

public sealed class RetryJobHandler(AsyncJobService asyncJobService, ICurrentUser currentUser)
{
    public async Task<JobStatusResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await asyncJobService.RetryFailedAsync(id, currentUser.Subject, cancellationToken);
        return job is null
            ? null
            : new JobStatusResponse(job.Id, job.JobType, job.Status, job.AttemptCount, job.CreatedAt, job.CompletedAt, job.LastError);
    }
}
