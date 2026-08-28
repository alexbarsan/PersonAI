using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Jobs;

public sealed class GetJobHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<JobStatusResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await dbContext.AsyncJobs.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == id && candidate.UserSubject == currentUser.Subject,
            cancellationToken);

        return job is null
            ? null
            : new JobStatusResponse(job.Id, job.JobType, job.Status, job.AttemptCount, job.CreatedAt, job.CompletedAt, job.LastError);
    }
}
