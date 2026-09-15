using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetDreamHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<DreamResponse?> HandleAsync(Guid id, CancellationToken cancellationToken)
    {
        var dream = await dbContext.Dreams
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == id && candidate.UserSubject == currentUser.Subject,
                cancellationToken);

        if (dream is null)
        {
            return null;
        }

        var job = await dbContext.AsyncJobs.AsNoTracking()
            .Where(candidate => candidate.UserSubject == currentUser.Subject
                && candidate.TargetId == dream.Id
                && candidate.JobType == AsyncJobTypes.DreamInterpretation)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        var processing = job is null ? null : DreamProcessingResponse.FromJob(job);
        if (processing is not null && dream.Status == DreamStatuses.Failed)
        {
            processing = processing with { CanRetry = true, CanCancel = false };
        }

        return DreamMapper.Map(dream, processing: processing);
    }
}
