using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Dreams;

public sealed class GetDreamImageHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IPrivateAssetStore assetStore)
{
    public async Task<DreamImageResponse?> HandleAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        return await GetCurrentAsync(dreamId, cancellationToken);
    }

    public async Task<DreamImageResponse?> WaitForChangeAsync(
        Guid dreamId,
        DateTimeOffset? after,
        int timeoutSeconds,
        CancellationToken cancellationToken)
    {
        var timeout = TimeSpan.FromSeconds(Math.Clamp(timeoutSeconds, 1, 25));
        var deadline = DateTimeOffset.UtcNow.Add(timeout);
        while (true)
        {
            var current = await GetCurrentAsync(dreamId, cancellationToken);
            if (current is null
                || after is null
                || current.UpdatedAt > after
                || current.Status is DreamImageStatuses.Completed or DreamImageStatuses.Failed
                || DateTimeOffset.UtcNow >= deadline)
            {
                return current;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(750), cancellationToken);
        }
    }

    private async Task<DreamImageResponse?> GetCurrentAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        var image = await dbContext.DreamImages
            .AsNoTracking()
            .Where(candidate => candidate.DreamId == dreamId && candidate.UserSubject == currentUser.Subject)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (image is null)
        {
            return null;
        }

        var job = await dbContext.AsyncJobs
            .AsNoTracking()
            .Where(job => job.TargetId == image.Id && job.UserSubject == currentUser.Subject && job.JobType == AsyncJobTypes.DreamImage)
            .Select(job => new { job.Id, job.QueueWaitMilliseconds })
            .SingleOrDefaultAsync(cancellationToken);
        var downloadUrl = image.Status == DreamImageStatuses.Completed && !string.IsNullOrWhiteSpace(image.AssetKey)
            ? assetStore.CreateReadUrl(image.AssetKey)
            : null;
        return DreamImageMapper.Map(image, job?.Id, downloadUrl, job?.QueueWaitMilliseconds);
    }
}
