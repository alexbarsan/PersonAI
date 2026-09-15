using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class DreamInterpretationLifecycleHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    AsyncJobService asyncJobService)
{
    public async Task<DreamResponse?> RetryAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        var dream = await dbContext.Dreams.SingleOrDefaultAsync(
            candidate => candidate.Id == dreamId && candidate.UserSubject == currentUser.Subject,
            cancellationToken);
        if (dream is null || dream.Status != DreamStatuses.Failed)
        {
            return null;
        }

        var job = await dbContext.AsyncJobs
            .Where(candidate => candidate.UserSubject == currentUser.Subject
                && candidate.TargetId == dreamId
                && candidate.JobType == AsyncJobTypes.DreamInterpretation)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (job is null)
        {
            return null;
        }

        dream.Status = DreamStatuses.Pending;
        dream.ErrorMessage = null;
        var restarted = await asyncJobService.RestartAsync(job.Id, currentUser.Subject, cancellationToken);
        if (restarted is null)
        {
            return null;
        }

        return DreamMapper.Map(dream, processing: DreamProcessingResponse.FromJob(restarted));
    }

    public async Task<DreamResponse?> CancelAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        var dream = await dbContext.Dreams.SingleOrDefaultAsync(
            candidate => candidate.Id == dreamId && candidate.UserSubject == currentUser.Subject,
            cancellationToken);
        if (dream is null || dream.Status is not (DreamStatuses.Pending or DreamStatuses.Processing))
        {
            return null;
        }

        dream.Status = DreamStatuses.Canceled;
        dream.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);
        var job = await dbContext.AsyncJobs.AsNoTracking()
            .Where(candidate => candidate.UserSubject == currentUser.Subject
                && candidate.TargetId == dreamId
                && candidate.JobType == AsyncJobTypes.DreamInterpretation)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var processing = job is null
            ? null
            : DreamProcessingResponse.FromJob(job) with { CanCancel = false, CanRetry = false };
        return DreamMapper.Map(dream, processing: processing);
    }
}
