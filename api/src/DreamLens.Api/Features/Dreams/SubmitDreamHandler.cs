using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Quotas;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonaKit.Providers.DeepSeek;

namespace DreamLens.Api.Features.Dreams;

public sealed class SubmitDreamHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    IDreamQuotaService quotaService,
    IOptions<DeepSeekOptions> deepSeekOptions,
    IOptions<DeepInterpretationOptions> deepInterpretationOptions,
    IOptions<PrimaryInterpretationOptions> primaryInterpretationOptions,
    DreamInterpretationJobHandler interpretationJobHandler,
    AsyncJobService asyncJobService)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<SubmitDreamResult> HandleAsync(
        SubmitDreamRequest request,
        CancellationToken cancellationToken)
    {
        var errors = Validate(request);
        if (errors.Count > 0)
        {
            return SubmitDreamResult.Invalid(errors);
        }

        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);
        if (profile is null)
        {
            return SubmitDreamResult.Invalid(new Dictionary<string, string[]>
            {
                ["profile"] = ["Profile must be completed before submitting dreams."]
            });
        }

        if (!profile.ConsentAiProcessing)
        {
            return SubmitDreamResult.Invalid(new Dictionary<string, string[]>
            {
                ["consent"] = ["AI processing consent is required before submitting dreams."]
            });
        }

        if (!await quotaService.CanSubmitDreamAsync(currentUser.Subject, cancellationToken))
        {
            return SubmitDreamResult.QuotaExceeded();
        }

        var dreamText = request.Text!.Trim();
        var entitlement = entitlementService.GetEntitlement(currentUser.Subject);
        var isPremium = entitlement.Tier == EntitlementTier.Premium;
        var route = new DreamInterpretationJobHandler.DreamInterpretationJobPayload(
            Guid.Empty,
            isPremium,
            isPremium ? "premium-dream-interpreter" : "dream-interpreter",
            isPremium ? "1.0.0" : "1.1.0",
            isPremium ? deepInterpretationOptions.Value.Model : deepSeekOptions.Value.Model,
            isPremium ? Math.Clamp(deepInterpretationOptions.Value.MaxOutputTokens, 512, 16_384) : null);
        var record = new DreamRecord
        {
            UserSubject = currentUser.Subject,
            Text = dreamText,
            Title = DreamTitleGenerator.Create(null, null, dreamText),
            Mood = Normalize(request.Mood),
            SleepQuality = request.SleepQuality,
            TagsJson = JsonSerializer.Serialize(NormalizeArray(request.Tags), JsonOptions),
            OccurredAt = Normalize(request.OccurredAt),
            Status = DreamStatuses.Pending
        };
        dbContext.Dreams.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);

        var payload = route with { DreamId = record.Id };
        if (!primaryInterpretationOptions.Value.AsyncEnabled)
        {
            await interpretationJobHandler.HandleAsync(
                new AsyncJobMessage(Guid.Empty, AsyncJobTypes.DreamInterpretation, record.UserSubject, JsonSerializer.Serialize(payload, JsonOptions)),
                cancellationToken);
            return record.Status == DreamStatuses.Completed
                ? SubmitDreamResult.Completed(DreamMapper.Map(record))
                : SubmitDreamResult.Failed(DreamMapper.Map(record));
        }

        var job = await asyncJobService.EnqueueAsync(
            $"{AsyncJobTypes.DreamInterpretation}:{record.Id}",
            AsyncJobTypes.DreamInterpretation,
            record.UserSubject,
            record.Id,
            payload,
            cancellationToken);

        return SubmitDreamResult.Accepted(DreamMapper.Map(record, processing: DreamProcessingResponse.FromJob(job)));
    }

    private static Dictionary<string, string[]> Validate(SubmitDreamRequest request)
    {
        var errors = new Dictionary<string, string[]>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(request.Text) || request.Text.Trim().Length < 10)
        {
            errors["text"] = ["Dream text must be at least 10 characters."];
        }
        else if (request.Text.Trim().Length > 4000)
        {
            errors["text"] = ["Dream text must be 4000 characters or fewer."];
        }

        if (request.SleepQuality is < 1 or > 5)
        {
            errors["sleepQuality"] = ["Sleep quality must be between 1 and 5."];
        }

        return errors;
    }

    private static string[] NormalizeArray(string[]? values) => values?
        .Select(Normalize)
        .Where(value => value is not null)
        .Cast<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(16)
        .ToArray() ?? [];

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
