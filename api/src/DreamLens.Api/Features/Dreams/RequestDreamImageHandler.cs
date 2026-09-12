using System.Text.Json;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Dreams;

public sealed class RequestDreamImageHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    IOptions<ImageGenerationOptions> imageOptions,
    IOptions<ImagePromptSafetyOptions> promptSafetyOptions,
    IImageGenerationRouteResolver routeResolver,
    IDreamImagePromptComposer promptComposer,
    IStringEncryptor encryptor,
    AsyncJobService? asyncJobService = null)
{
    public async Task<RequestDreamImageResult> HandleAsync(
        Guid dreamId,
        RequestDreamImageRequest request,
        CancellationToken cancellationToken)
    {
        var entitlement = entitlementService.GetEntitlement(currentUser.Subject);
        var route = routeResolver.Resolve(entitlement.Tier);
        if (!route.Enabled || asyncJobService is null)
        {
            return RequestDreamImageResult.Unavailable();
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

        var idempotencyKey = $"{AsyncJobTypes.DreamImage}:{dreamId}:{style}:{route.Tier}:{route.Provider}:{route.Model}:{route.Quality}:{imageOptions.Value.PromptVersion}";
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

        if (!entitlement.QuotaExempt && !await CanQueueImageAsync(route.Tier, cancellationToken))
        {
            return RequestDreamImageResult.QuotaExceeded(route.Tier);
        }

        var facts = await dbContext.DreamFacts
            .AsNoTracking()
            .Where(fact => fact.DreamId == dreamId && fact.UserSubject == currentUser.Subject)
            .ToArrayAsync(cancellationToken);
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);
        var traits = profile is null
            ? ProfileTraitsDto.Empty
            : JsonSerializer.Deserialize<ProfileTraitsDto>(encryptor.Decrypt(profile.EncryptedTraitsJson)) ?? ProfileTraitsDto.Empty;

        var safety = await ResolveSafetyAsync(dream, asyncJobService, cancellationToken);
        var prompt = promptComposer.Compose(
            dream,
            facts,
            traits,
            profile?.ConsentAiProcessing == true,
            profile?.ConsentAiProcessing == true && profile.ConsentSensitiveTraits,
            safety,
            style,
            imageOptions.Value.PromptVersion);
        var image = new DreamImageRecord
        {
            DreamId = dreamId,
            UserSubject = currentUser.Subject,
            Status = DreamImageStatuses.Pending,
            Style = style,
            EncryptedPrompt = encryptor.Encrypt(prompt.Text),
            PromptVersion = prompt.Version,
            PromptMode = prompt.Mode.ToString().ToLowerInvariant(),
            ModerationCategoriesJson = JsonSerializer.Serialize(prompt.ModerationCategories),
            Provider = route.Provider,
            Model = route.Model,
            Tier = route.Tier.ToString().ToLowerInvariant(),
            Width = route.Width,
            Height = route.Height,
            EstimatedCostUsd = route.EstimatedCostUsd,
            Quality = route.Quality
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

    private async Task<ImagePromptSafetyResult> ResolveSafetyAsync(
        DreamRecord dream,
        AsyncJobService asyncJobService,
        CancellationToken cancellationToken)
    {
        if (!promptSafetyOptions.Value.Enabled)
        {
            return new ImagePromptSafetyResult(
                DreamImagePromptMode.Standard,
                "Disabled",
                promptSafetyOptions.Value.Model,
                []);
        }

        var classification = await dbContext.DreamImageSafety
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.DreamId == dream.Id && candidate.UserSubject == currentUser.Subject,
                cancellationToken);
        if (classification?.Status == DreamImageSafetyStatuses.Completed)
        {
            var categories = JsonSerializer.Deserialize<string[]>(classification.CategoriesJson) ?? [];
            var mode = Enum.TryParse<DreamImagePromptMode>(classification.PromptMode, true, out var storedMode)
                ? storedMode
                : DreamImagePromptMode.Symbolic;
            return new ImagePromptSafetyResult(mode, classification.Provider, classification.Model, categories);
        }

        if (classification is null)
        {
            classification = new DreamImageSafetyRecord
            {
                DreamId = dream.Id,
                UserSubject = currentUser.Subject,
                Provider = promptSafetyOptions.Value.Provider,
                Model = promptSafetyOptions.Value.Model
            };
            dbContext.DreamImageSafety.Add(classification);
            await dbContext.SaveChangesAsync(cancellationToken);
            await asyncJobService.EnqueueAsync(
                $"{AsyncJobTypes.DreamImageSafety}:{dream.Id}:{promptSafetyOptions.Value.Model}",
                AsyncJobTypes.DreamImageSafety,
                currentUser.Subject,
                classification.Id,
                new DreamImageSafetyJobHandler.DreamImageSafetyJobPayload(classification.Id),
                cancellationToken);
        }

        var category = classification.Status == DreamImageSafetyStatuses.Failed
            ? "safety-unavailable"
            : "classification-pending";
        return new ImagePromptSafetyResult(
            DreamImagePromptMode.Symbolic,
            classification.Provider,
            classification.Model,
            [category],
            UsedFallback: true);
    }

    private async Task<bool> CanQueueImageAsync(EntitlementTier tier, CancellationToken cancellationToken)
    {
        var limit = tier == EntitlementTier.Premium
            ? imageOptions.Value.PremiumDailyLimit
            : imageOptions.Value.FreeDailyLimit;
        if (limit <= 0)
        {
            return false;
        }

        var todayStart = new DateTimeOffset(DateTimeOffset.UtcNow.Date, TimeSpan.Zero);
        var tomorrowStart = todayStart.AddDays(1);
        var requestsToday = await dbContext.DreamImages.AsNoTracking().CountAsync(
            image => image.UserSubject == currentUser.Subject
                && image.CreatedAt >= todayStart
                && image.CreatedAt < tomorrowStart,
            cancellationToken);
        return requestsToday < limit;
    }
}

public sealed record RequestDreamImageResult(int StatusCode, DreamImageResponse? Image, Dictionary<string, string[]>? Errors)
{
    public static RequestDreamImageResult Accepted(DreamImageResponse image) => new(StatusCodes.Status202Accepted, image, null);
    public static RequestDreamImageResult NotFound() => new(StatusCodes.Status404NotFound, null, null);
    public static RequestDreamImageResult NotEntitled() => new(StatusCodes.Status403Forbidden, null, new Dictionary<string, string[]> { ["entitlement"] = ["Dream images require premium access."] });
    public static RequestDreamImageResult QuotaExceeded(EntitlementTier tier) => new(StatusCodes.Status429TooManyRequests, null, new Dictionary<string, string[]> { ["quota"] = [$"You have reached today's {tier.ToString().ToLowerInvariant()} dream image limit."] });
    public static RequestDreamImageResult Unavailable() => new(StatusCodes.Status503ServiceUnavailable, null, new Dictionary<string, string[]> { ["imageGeneration"] = ["Dream image generation is not available yet."] });
    public static RequestDreamImageResult InvalidStyle() => new(StatusCodes.Status400BadRequest, null, new Dictionary<string, string[]> { ["style"] = ["The requested image style is not supported."] });
}
