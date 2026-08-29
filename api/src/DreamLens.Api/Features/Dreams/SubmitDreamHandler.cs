using System.Diagnostics;
using System.Text.Json;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Observability;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Quotas;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonaKit.Context;
using PersonaKit.Pipeline;
using PersonaKit.Providers.DeepSeek;
using PersonaKit.Providers.Usage;

namespace DreamLens.Api.Features.Dreams;

public sealed class SubmitDreamHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor,
    IInterpretationPipeline interpretationPipeline,
    IDreamQuotaService quotaService,
    IOptions<EmbeddingOptions> embeddingOptions,
    IOptions<DeepSeekOptions> deepSeekOptions,
    IOptions<UsageCostOptions> usageCostOptions,
    AsyncJobService? asyncJobService = null)
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
            DreamLensMeters.QuotaRejections.Add(1);
            return SubmitDreamResult.QuotaExceeded();
        }

        var traits = JsonSerializer.Deserialize<ProfileTraitsDto>(encryptor.Decrypt(profile.EncryptedTraitsJson), JsonOptions)
            ?? ProfileTraitsDto.Empty;
        var dreamText = request.Text!.Trim();
        var started = Stopwatch.GetTimestamp();
        var interpretation = await interpretationPipeline.InterpretAsync(
            new InterpretationRequest(
                "dream-interpreter",
                new ContextBuildRequest(
                    Guid.NewGuid().ToString(),
                    NormalizeLocale(profile.Language),
                    new ContextPersona("dream-interpreter", "1.1.0"),
                    new ContextUserSource(
                        profile.UserSubject,
                        null,
                        null,
                        profile.Age,
                        profile.Sex,
                        profile.GenderIdentity,
                        profile.Language,
                        profile.Timezone,
                        new ContextTraits(
                            traits.Fears,
                            traits.Allergies,
                            traits.Interests,
                            traits.Occupation,
                            traits.RelationshipStatus,
                            traits.CulturalBackground,
                            traits.SleepPattern,
                            traits.StressLevel,
                            traits.RecentLifeEvents),
                        new ContextConsent(
                            profile.ConsentAiProcessing,
                            profile.ConsentSensitiveTraits,
                            profile.ConsentHistoryUse)),
                    null,
                    new DreamInput(
                        dreamText,
                        Normalize(request.Mood),
                        request.SleepQuality,
                        NormalizeArray(request.Tags),
                        Normalize(request.OccurredAt)))),
            cancellationToken);
        var latency = Stopwatch.GetElapsedTime(started);

        var result = interpretation.Result is null ? null : MapResult(interpretation.Result);
        var record = new DreamRecord
        {
            UserSubject = currentUser.Subject,
            Text = dreamText,
            Mood = Normalize(request.Mood),
            SleepQuality = request.SleepQuality,
            TagsJson = JsonSerializer.Serialize(NormalizeArray(request.Tags), JsonOptions),
            OccurredAt = Normalize(request.OccurredAt),
            Status = interpretation.Status == InterpretationStatus.Completed ? "completed" : "failed",
            ResultJson = result is null ? null : JsonSerializer.Serialize(result, JsonOptions),
            ErrorMessage = interpretation.ErrorMessage
        };

        dbContext.Dreams.Add(record);
        if (record.Status == "completed" && interpretation.Result is not null)
        {
            dbContext.DreamFacts.AddRange(DreamFactExtractor.Extract(record, interpretation.Result.RawJson));
        }
        dbContext.AiCostLedger.Add(CreateLedgerRecord(record, interpretation, latency));
        if (record.Status == "failed")
        {
            DreamLensMeters.ProviderFailures.Add(1, new KeyValuePair<string, object?>("provider", "DeepSeek"));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        if (profile.ConsentHistoryUse && embeddingOptions.Value.Enabled && asyncJobService is not null)
        {
            await asyncJobService.EnqueueAsync(
                $"{AsyncJobTypes.DreamEmbedding}:{record.Id}:{embeddingOptions.Value.Version}",
                AsyncJobTypes.DreamEmbedding,
                record.UserSubject,
                record.Id,
                new DreamEmbeddingJobHandler.DreamEmbeddingJobPayload(record.Id),
                cancellationToken);
        }

        return SubmitDreamResult.Valid(DreamMapper.Map(record, result));
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

    private static DreamResultResponse MapResult(InterpretationResult result)
    {
        return new DreamResultResponse(
            result.Summary,
            result.Sections.Select(section => new DreamSectionResponse(section.Kind, section.Title, section.Content)).ToArray(),
            result.FollowUpQuestions);
    }

    private AiCostLedgerRecord CreateLedgerRecord(
        DreamRecord dream,
        InterpretationResponse interpretation,
        TimeSpan latency)
    {
        var run = interpretation.Run;
        var inputTokens = run?.InputTokens;
        var outputTokens = run?.OutputTokens;

        return new AiCostLedgerRecord
        {
            UserSubject = currentUser.Subject,
            DreamId = dream.Id,
            Provider = "DeepSeek",
            Model = deepSeekOptions.Value.Model,
            PersonaId = run?.PersonaId ?? "dream-interpreter",
            OperationType = "dream.interpretation",
            Status = interpretation.Status == InterpretationStatus.Completed ? "completed" : "failed",
            FailureKind = run?.FailureKind?.ToString(),
            AttemptCount = run?.AttemptCount ?? 0,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = EstimateCost(inputTokens, outputTokens)
        };
    }

    private decimal EstimateCost(int? inputTokens, int? outputTokens)
    {
        var options = usageCostOptions.Value;
        var input = (inputTokens ?? 0) * options.InputCostPerMillionTokens / 1_000_000m;
        var output = (outputTokens ?? 0) * options.OutputCostPerMillionTokens / 1_000_000m;
        return input + output;
    }

    private static string NormalizeLocale(string language)
    {
        return string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en-US" : language;
    }

    private static string[] NormalizeArray(string[]? values)
    {
        return values?
            .Select(Normalize)
            .Where(value => value is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(16)
            .ToArray() ?? [];
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
