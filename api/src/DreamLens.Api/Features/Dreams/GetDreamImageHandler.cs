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
        var image = await dbContext.DreamImages
            .AsNoTracking()
            .Where(candidate => candidate.DreamId == dreamId && candidate.UserSubject == currentUser.Subject)
            .OrderByDescending(candidate => candidate.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (image is null)
        {
            return null;
        }

        var jobId = await dbContext.AsyncJobs
            .AsNoTracking()
            .Where(job => job.TargetId == image.Id && job.UserSubject == currentUser.Subject && job.JobType == AsyncJobTypes.DreamImage)
            .Select(job => (Guid?)job.Id)
            .SingleOrDefaultAsync(cancellationToken);
        var downloadUrl = image.Status == DreamImageStatuses.Completed && !string.IsNullOrWhiteSpace(image.AssetKey)
            ? assetStore.CreateReadUrl(image.AssetKey)
            : null;
        return DreamImageMapper.Map(image, jobId, downloadUrl);
    }
}
