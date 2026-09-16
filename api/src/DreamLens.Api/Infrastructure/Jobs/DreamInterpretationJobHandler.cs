using System.Diagnostics;
using System.Text.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Features.Safety;
using DreamLens.Api.Infrastructure.Embeddings;
using DreamLens.Api.Infrastructure.Images;
using DreamLens.Api.Infrastructure.Observability;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PersonaKit.Context;
using PersonaKit.Pipeline;
using PersonaKit.Providers.Usage;

namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class DreamInterpretationJobHandler(
    DreamLensDbContext dbContext,
    IStringEncryptor encryptor,
    IInterpretationPipeline interpretationPipeline,
    IOptions<EmbeddingOptions> embeddingOptions,
    IOptions<ImageGenerationOptions> imageGenerationOptions,
    IOptions<ImagePromptSafetyOptions> imagePromptSafetyOptions,
    IOptions<UsageCostOptions> usageCostOptions,
    IOptions<DeepInterpretationOptions> deepInterpretationOptions,
    IOptions<SensitiveSafetyOptions> sensitiveSafetyOptions,
    SensitiveSafetyEventFactory sensitiveSafetyEventFactory,
    AsyncJobService asyncJobService)
    : IAsyncJobHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public string JobType => AsyncJobTypes.DreamInterpretation;

    public async Task HandleAsync(AsyncJobMessage message, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Deserialize<DreamInterpretationJobPayload>(message.PayloadJson, JsonOptions)
            ?? throw new InvalidOperationException("Dream interpretation job payload is invalid.");
        var dream = await dbContext.Dreams.SingleOrDefaultAsync(
            candidate => candidate.Id == payload.DreamId && candidate.UserSubject == message.UserSubject,
            cancellationToken)
            ?? throw new InvalidOperationException("Dream was not found.");
        if (dream.Status is DreamStatuses.Completed or DreamStatuses.Canceled)
        {
            return;
        }

        var profile = await dbContext.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == message.UserSubject, cancellationToken);
        if (profile is null || !profile.ConsentAiProcessing)
        {
            dream.Status = DreamStatuses.Failed;
            dream.ErrorMessage = "Your interpretation could not be completed because AI processing consent is no longer available.";
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        dream.Status = DreamStatuses.Processing;
        dream.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        var traits = JsonSerializer.Deserialize<ProfileTraitsDto>(encryptor.Decrypt(profile.EncryptedTraitsJson), JsonOptions)
            ?? ProfileTraitsDto.Empty;
        var started = Stopwatch.GetTimestamp();
        InterpretationResponse interpretation;
        try
        {
            interpretation = await interpretationPipeline.InterpretAsync(
                new InterpretationRequest(
                    payload.PersonaId,
                    new ContextBuildRequest(
                        Guid.NewGuid().ToString(),
                        NormalizeLocale(profile.Language),
                        new ContextPersona(payload.PersonaId, payload.PersonaVersion),
                        new ContextUserSource(
                            profile.UserSubject,
                            null,
                            profile.PreferredName,
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
                            dream.Text,
                            dream.Mood,
                            dream.SleepQuality,
                            DreamMapper.ReadTags(dream),
                            dream.OccurredAt)),
                    new InterpretationExecutionOptions(payload.Model, payload.MaxOutputTokens, 0.8f)),
                cancellationToken);
        }
        catch
        {
            await RecordUnexpectedFailureAsync(dream, payload, message.UserSubject, Stopwatch.GetElapsedTime(started), cancellationToken);
            throw;
        }

        var latency = Stopwatch.GetElapsedTime(started);
        dbContext.AiCostLedger.Add(CreateLedger(dream, interpretation, latency, payload));
        if (interpretation.Status != InterpretationStatus.Completed || interpretation.Result is null)
        {
            dream.Status = DreamStatuses.Failed;
            dream.ErrorMessage = interpretation.ErrorMessage ?? "Your interpretation could not be completed. Please try again.";
            await dbContext.SaveChangesAsync(cancellationToken);
            DreamLensMeters.ProviderFailures.Add(1, new KeyValuePair<string, object?>("provider", "DeepSeek"));
            return;
        }

        await dbContext.Entry(dream).ReloadAsync(cancellationToken);
        if (dream.Status == DreamStatuses.Canceled)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return;
        }

        var result = MapResult(interpretation.Result);
        dream.Title = DreamTitleGenerator.FromInterpretation(interpretation.Result.RawJson, result.Summary, dream.Text);
        dream.Status = DreamStatuses.Completed;
        dream.ResultJson = JsonSerializer.Serialize(result, JsonOptions);
        dream.ErrorMessage = null;
        dbContext.DreamFacts.AddRange(DreamFactExtractor.Extract(dream, interpretation.Result.RawJson));
        var safetyEvents = sensitiveSafetyEventFactory.Create(dream, dream.Text, result.Safety);
        dbContext.SensitiveDreamSafetyEvents.AddRange(safetyEvents);
        var notifications = safetyEvents
            .Where(safetyEvent => safetyEvent.ReviewRequired)
            .Select(safetyEvent => new SensitiveReviewNotification
            {
                SafetyEventId = safetyEvent.Id,
                DreamId = safetyEvent.DreamId,
                SubjectPseudonym = safetyEvent.SubjectPseudonym,
                Category = safetyEvent.Category,
                Confidence = safetyEvent.Confidence,
                Route = "privacy-review"
            })
            .ToArray();
        dbContext.SensitiveReviewNotifications.AddRange(notifications);
        DreamLensMeters.SensitiveSafetyReviewsPending.Add(notifications.Length);

        DreamImageSafetyRecord? imageSafety = null;
        if (imagePromptSafetyOptions.Value.Enabled
            && (imageGenerationOptions.Value.Free.Enabled || imageGenerationOptions.Value.Premium.Enabled))
        {
            imageSafety = new DreamImageSafetyRecord
            {
                DreamId = dream.Id,
                UserSubject = dream.UserSubject,
                Provider = imagePromptSafetyOptions.Value.Provider,
                Model = imagePromptSafetyOptions.Value.Model
            };
            dbContext.DreamImageSafety.Add(imageSafety);
        }
        await dbContext.SaveChangesAsync(cancellationToken);

        if (profile.ConsentHistoryUse && embeddingOptions.Value.Enabled)
        {
            await asyncJobService.EnqueueAsync(
                $"{AsyncJobTypes.DreamEmbedding}:{dream.Id}:{embeddingOptions.Value.Version}",
                AsyncJobTypes.DreamEmbedding,
                dream.UserSubject,
                dream.Id,
                new DreamEmbeddingJobHandler.DreamEmbeddingJobPayload(dream.Id),
                cancellationToken);
        }

        if (imageSafety is not null)
        {
            await asyncJobService.EnqueueAsync(
                $"{AsyncJobTypes.DreamImageSafety}:{dream.Id}:{imagePromptSafetyOptions.Value.Model}",
                AsyncJobTypes.DreamImageSafety,
                dream.UserSubject,
                imageSafety.Id,
                new DreamImageSafetyJobHandler.DreamImageSafetyJobPayload(imageSafety.Id),
                cancellationToken);
        }
    }

    private async Task RecordUnexpectedFailureAsync(
        DreamRecord dream,
        DreamInterpretationJobPayload payload,
        string userSubject,
        TimeSpan latency,
        CancellationToken cancellationToken)
    {
        dream.Status = DreamStatuses.Pending;
        dream.ErrorMessage = null;
        dbContext.AiCostLedger.Add(new AiCostLedgerRecord
        {
            UserSubject = userSubject,
            DreamId = dream.Id,
            Provider = "DeepSeek",
            Model = payload.Model,
            PersonaId = payload.PersonaId,
            OperationType = payload.IsPremium ? "dream.premium-interpretation" : "dream.interpretation",
            Status = "failed",
            FailureKind = "UnhandledException",
            AttemptCount = 1,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = 0
        });
        await dbContext.SaveChangesAsync(cancellationToken);
        DreamLensMeters.ProviderFailures.Add(1, new KeyValuePair<string, object?>("provider", "DeepSeek"));
    }

    private DreamResultResponse MapResult(InterpretationResult result)
    {
        using var document = JsonDocument.Parse(result.RawJson);
        var safety = document.RootElement.TryGetProperty("safety", out var safetyElement)
            ? SensitiveSafetyParser.Parse(safetyElement, sensitiveSafetyOptions.Value)
            : null;
        return new DreamResultResponse(
            result.Summary,
            result.Sections.Select(section => new DreamSectionResponse(section.Kind, section.Title, section.Content)).ToArray(),
            result.FollowUpQuestions,
            safety);
    }

    private AiCostLedgerRecord CreateLedger(
        DreamRecord dream,
        InterpretationResponse interpretation,
        TimeSpan latency,
        DreamInterpretationJobPayload payload)
    {
        var inputTokens = interpretation.Run?.InputTokens;
        var outputTokens = interpretation.Run?.OutputTokens;
        var inputCost = payload.IsPremium
            ? deepInterpretationOptions.Value.InputCostPerMillionTokensUsd
            : usageCostOptions.Value.InputCostPerMillionTokens;
        var outputCost = payload.IsPremium
            ? deepInterpretationOptions.Value.OutputCostPerMillionTokensUsd
            : usageCostOptions.Value.OutputCostPerMillionTokens;
        return new AiCostLedgerRecord
        {
            UserSubject = dream.UserSubject,
            DreamId = dream.Id,
            Provider = "DeepSeek",
            Model = payload.Model,
            PersonaId = payload.PersonaId,
            OperationType = payload.IsPremium ? "dream.premium-interpretation" : "dream.interpretation",
            Status = interpretation.Status == InterpretationStatus.Completed ? "completed" : "failed",
            FailureKind = interpretation.Run?.FailureKind?.ToString(),
            AttemptCount = interpretation.Run?.AttemptCount ?? 0,
            InputTokens = inputTokens,
            OutputTokens = outputTokens,
            TotalTokens = inputTokens + outputTokens,
            LatencyMilliseconds = Math.Max(0, (long)latency.TotalMilliseconds),
            EstimatedCostUsd = (inputTokens ?? 0) * inputCost / 1_000_000m
                + (outputTokens ?? 0) * outputCost / 1_000_000m
        };
    }

    private static string NormalizeLocale(string language) =>
        string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? "en-US" : language;

    public sealed record DreamInterpretationJobPayload(
        Guid DreamId,
        bool IsPremium,
        string PersonaId,
        string PersonaVersion,
        string Model,
        int? MaxOutputTokens);
}
