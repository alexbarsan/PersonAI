using System.Diagnostics;
using System.Text.Json;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Monetization;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonaKit.Context;
using PersonaKit.Pipeline;

namespace DreamLens.Api.Features.Dreams;

public sealed class DeepInterpretationHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IEntitlementService entitlementService,
    IStringEncryptor encryptor,
    IInterpretationPipeline interpretationPipeline,
    GetSimilarDreamsHandler similarDreamsHandler,
    IOptions<DeepInterpretationOptions> options,
    ILogger<DeepInterpretationHandler> logger)
{
    private const string PersonaId = "deep-dream-interpreter";
    private const string PersonaVersion = "1.0.0";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<DeepInterpretationResponse?> GetAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        var record = await dbContext.DreamDeepInterpretations.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.DreamId == dreamId && candidate.UserSubject == currentUser.Subject,
                cancellationToken);
        return record is null ? null : Map(record);
    }

    public async Task<DeepInterpretationResult> CreateAsync(Guid dreamId, CancellationToken cancellationToken)
    {
        if (!options.Value.Enabled)
        {
            return DeepInterpretationResult.Failure(StatusCodes.Status503ServiceUnavailable, "service", "Deep Interpretation is temporarily unavailable.");
        }

        if (entitlementService.GetEntitlement(currentUser.Subject).Tier != EntitlementTier.Premium)
        {
            return DeepInterpretationResult.Failure(StatusCodes.Status403Forbidden, "entitlement", "Deep Interpretation requires Premium.");
        }

        var stage = "load saved result";
        try
        {
            var existing = await GetAsync(dreamId, cancellationToken);
            if (existing is not null)
            {
                return DeepInterpretationResult.Success(existing);
            }

            stage = "load dream";
            var dream = await dbContext.Dreams.AsNoTracking()
            .SingleOrDefaultAsync(
                candidate => candidate.Id == dreamId && candidate.UserSubject == currentUser.Subject,
                cancellationToken);
            if (dream is null)
            {
                return DeepInterpretationResult.Failure(StatusCodes.Status404NotFound, "dream", "Dream was not found.");
            }

            if (dream.Status != "completed" || string.IsNullOrWhiteSpace(dream.ResultJson))
            {
                return DeepInterpretationResult.Failure(StatusCodes.Status409Conflict, "dream", "A completed interpretation is required first.");
            }

            stage = "load profile";
            var profile = await dbContext.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == currentUser.Subject, cancellationToken);
            if (profile is null)
            {
                return DeepInterpretationResult.Failure(StatusCodes.Status409Conflict, "profile", "Complete your profile before requesting Deep Interpretation.");
            }

            if (!profile.ConsentAiProcessing || !profile.ConsentHistoryUse)
            {
                return DeepInterpretationResult.Failure(StatusCodes.Status409Conflict, "consent", "AI processing and dream history consent are required.");
            }

            stage = "check quota";
            var today = DateTimeOffset.UtcNow.Date;
            var completedToday = await dbContext.AiCostLedger.AsNoTracking().CountAsync(
            row => row.UserSubject == currentUser.Subject
                && row.OperationType == "dream.deep-interpretation"
                && row.Status == "completed"
                && row.CreatedAt >= today,
            cancellationToken);
            if (completedToday >= Math.Max(0, options.Value.DailyLimit))
            {
                return DeepInterpretationResult.Failure(StatusCodes.Status429TooManyRequests, "quota", "You have reached today's Deep Interpretation limit.");
            }

            stage = "retrieve related dreams";
            var similar = await similarDreamsHandler.HandleAsync(dreamId, options.Value.RetrievalLimit, cancellationToken)
            ?? new SimilarDreamsResponse(dreamId, []);
            var relatedIds = similar.Matches.Select(match => match.Id).ToArray();
            var recentThemes = await dbContext.DreamFacts.AsNoTracking()
            .Where(fact => fact.UserSubject == currentUser.Subject
                && relatedIds.Contains(fact.DreamId)
                && fact.FactType == "theme")
            .GroupBy(fact => fact.NormalizedValue)
            .OrderByDescending(group => group.Count())
            .Take(8)
            .Select(group => group.First().DisplayValue)
            .ToArrayAsync(cancellationToken);
            var interactionCount = await dbContext.Dreams.AsNoTracking().CountAsync(
            candidate => candidate.UserSubject == currentUser.Subject && candidate.Status == "completed",
            cancellationToken);
            stage = "build context";
            var traits = JsonSerializer.Deserialize<ProfileTraitsDto>(encryptor.Decrypt(profile.EncryptedTraitsJson), JsonOptions)
            ?? ProfileTraitsDto.Empty;
            var history = new ContextHistory(
            recentThemes,
            interactionCount,
            DreamMapper.ReadSummary(dream),
            similar.Matches.Select(match => new ContextHistoryItem(
                match.Summary ?? "",
                match.OccurredAt,
                match.Similarity)).ToArray());

            stage = "request interpretation";
            var started = Stopwatch.GetTimestamp();
            var interpretation = await interpretationPipeline.InterpretAsync(
            new InterpretationRequest(
                PersonaId,
                new ContextBuildRequest(
                    Guid.NewGuid().ToString(),
                    NormalizeLocale(profile.Language),
                    new ContextPersona(PersonaId, PersonaVersion),
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
                        new ContextConsent(true, profile.ConsentSensitiveTraits, true)),
                    history,
                    new DreamInput(
                        dream.Text,
                        dream.Mood,
                        dream.SleepQuality,
                        DreamMapper.ReadTags(dream),
                        dream.OccurredAt)),
                new InterpretationExecutionOptions(
                    options.Value.Model,
                    Math.Clamp(options.Value.MaxOutputTokens, 512, 16_384),
                    0.8f)),
            cancellationToken);
            var latency = Stopwatch.GetElapsedTime(started);
            dbContext.AiCostLedger.Add(CreateLedger(dream.Id, interpretation, latency));

            if (interpretation.Status != InterpretationStatus.Completed || interpretation.Result is null)
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return DeepInterpretationResult.Failure(
                    StatusCodes.Status503ServiceUnavailable,
                    "interpretation",
                    interpretation.ErrorMessage ?? "Deep Interpretation could not be completed.");
            }

            stage = "persist interpretation";
            var result = MapResult(interpretation.Result);
            var record = new DreamDeepInterpretationRecord
            {
                DreamId = dream.Id,
                UserSubject = currentUser.Subject,
                ResultJson = JsonSerializer.Serialize(result, JsonOptions),
                SourcesJson = JsonSerializer.Serialize(similar.Matches, JsonOptions),
                Provider = "DeepSeek",
                Model = options.Value.Model,
                PersonaVersion = PersonaVersion
            };
            dbContext.DreamDeepInterpretations.Add(record);
            await dbContext.SaveChangesAsync(cancellationToken);
            return DeepInterpretationResult.Success(Map(record));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Deep Interpretation failed during {Stage}.", stage);
            return DeepInterpretationResult.Failure(
                StatusCodes.Status503ServiceUnavailable,
                "interpretation",
                "Deep Interpretation is temporarily unavailable. Please try again.");
        }
    }

    private DeepInterpretationResponse Map(DreamDeepInterpretationRecord record) => new(
        record.Id,
        record.DreamId,
        JsonSerializer.Deserialize<DreamResultResponse>(record.ResultJson, JsonOptions)
            ?? throw new InvalidOperationException("Stored deep interpretation is invalid."),
        JsonSerializer.Deserialize<SimilarDreamResponse[]>(record.SourcesJson, JsonOptions) ?? [],
        record.Model,
        record.CreatedAt);

    private AiCostLedgerRecord CreateLedger(Guid dreamId, InterpretationResponse interpretation, TimeSpan latency)
    {
        var inputTokens = interpretation.Run?.InputTokens;
        var outputTokens = interpretation.Run?.OutputTokens;
        return new AiCostLedgerRecord
        {
            UserSubject = currentUser.Subject,
            DreamId = dreamId,
            Provider = "DeepSeek",
            Model = options.Value.Model,
            PersonaId = PersonaId,
            OperationType = "dream.deep-interpretation",
            Status = interpretation.Status == InterpretationStatus.Completed ? "completed" : "failed",
            FailureKind = interpretation.Run?.FailureKind?.ToString(),
            AttemptCount = interpretation.Run?.AttemptCount ?? 0,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = (inputTokens ?? 0) * options.Value.InputCostPerMillionTokensUsd / 1_000_000m
                + (outputTokens ?? 0) * options.Value.OutputCostPerMillionTokensUsd / 1_000_000m
        };
    }

    private static DreamResultResponse MapResult(InterpretationResult result)
    {
        using var document = JsonDocument.Parse(result.RawJson);
        var safetyElement = document.RootElement.GetProperty("safety");
        return new DreamResultResponse(
            result.Summary,
            result.Sections.Select(section => new DreamSectionResponse(section.Kind, section.Title, section.Content)).ToArray(),
            result.FollowUpQuestions,
            new DreamSafetyResponse(
                safetyElement.GetProperty("selfHarmRisk").GetString() ?? "none",
                safetyElement.GetProperty("notes").GetString() ?? ""));
    }

    private static string NormalizeLocale(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en-US" : language;
}
