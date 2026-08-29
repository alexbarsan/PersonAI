using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Dreams;

public sealed class RequestDreamImageHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    IOptions<ImageGenerationOptions> imageOptions,
    AsyncJobService? asyncJobService = null)
{
    public async Task<RequestDreamImageResult> HandleAsync(
        Guid dreamId,
        RequestDreamImageRequest request,
        CancellationToken cancellationToken)
    {
        if (!imageOptions.Value.Enabled || asyncJobService is null)
        {
            return RequestDreamImageResult.Unavailable();
        }

        if (!entitlementService.GetEntitlement(currentUser.Subject).DeepAnalysisEnabled)
        {
            return RequestDreamImageResult.NotEntitled();
        }

        var dream = await dbContext.Dreams
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == dreamId
                    && candidate.UserSubject == currentUser.Subject
                    && candidate.Status == "completed",
                cancellationToken);
        if (dream is null)
        {
            return RequestDreamImageResult.NotFound();
        }

        var style = ImageStyles.Normalize(request.Style, imageOptions.Value.DefaultStyle);
        if (style is null)
        {
            return RequestDreamImageResult.InvalidStyle();
        }

        var idempotencyKey = $"{AsyncJobTypes.DreamImage}:{dreamId}:{style}";
        var existingJob = await dbContext.AsyncJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(job => job.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existingJob is not null)
        {
            var existingImage = existingJob.TargetId is null
                ? null
                : await dbContext.DreamImages.AsNoTracking().SingleOrDefaultAsync(image => image.Id == existingJob.TargetId, cancellationToken);
            return existingImage is null
                ? RequestDreamImageResult.Unavailable()
                : RequestDreamImageResult.Accepted(DreamImageMapper.Map(existingImage, existingJob.Id, null));
        }

        var image = new DreamImageRecord
        {
            DreamId = dreamId,
            UserSubject = currentUser.Subject,
            Status = DreamImageStatuses.Pending,
            Style = style
        };
        dbContext.DreamImages.Add(image);
        await dbContext.SaveChangesAsync(cancellationToken);
        var job = await asyncJobService.EnqueueAsync(
            idempotencyKey,
            AsyncJobTypes.DreamImage,
            currentUser.Subject,
            image.Id,
            new DreamImageJobHandler.DreamImageJobPayload(image.Id),
            cancellationToken);

        return RequestDreamImageResult.Accepted(DreamImageMapper.Map(image, job.Id, null));
    }
}

public sealed record RequestDreamImageResult(int StatusCode, DreamImageResponse? Image, Dictionary<string, string[]>? Errors)
{
    public static RequestDreamImageResult Accepted(DreamImageResponse image) => new(StatusCodes.Status202Accepted, image, null);
    public static RequestDreamImageResult NotFound() => new(StatusCodes.Status404NotFound, null, null);
    public static RequestDreamImageResult NotEntitled() => new(StatusCodes.Status403Forbidden, null, new Dictionary<string, string[]> { ["entitlement"] = ["Dream images require premium access."] });
    public static RequestDreamImageResult Unavailable() => new(StatusCodes.Status503ServiceUnavailable, null, new Dictionary<string, string[]> { ["imageGeneration"] = ["Dream image generation is not available yet."] });
    public static RequestDreamImageResult InvalidStyle() => new(StatusCodes.Status400BadRequest, null, new Dictionary<string, string[]> { ["style"] = ["The requested image style is not supported."] });
}
