using System.Text.Json;
using DreamLens.Api.Features.Profile;
using DreamLens.Api.Infrastructure.Assets;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Privacy;

public sealed class ExportUserDataHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor,
    IPrivateAssetStore assetStore)
{
    public async Task<UserDataExportResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var subject = currentUser.Subject;
        var profile = await dbContext.UserProfiles.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.UserSubject == subject, cancellationToken);
        var dreams = await dbContext.Dreams.AsNoTracking()
            .Where(dream => dream.UserSubject == subject)
            .OrderBy(dream => dream.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var dreamIds = dreams.Select(dream => dream.Id).ToArray();
        var interpretationFeedback = await dbContext.DreamInterpretationFeedback.AsNoTracking()
            .Where(feedback => feedback.UserSubject == subject && dreamIds.Contains(feedback.DreamId))
            .ToArrayAsync(cancellationToken);
        var deepInterpretations = await dbContext.DreamDeepInterpretations.AsNoTracking()
            .Where(interpretation => interpretation.UserSubject == subject && dreamIds.Contains(interpretation.DreamId))
            .ToArrayAsync(cancellationToken);
        var facts = await dbContext.DreamFacts.AsNoTracking()
            .Where(fact => fact.UserSubject == subject && dreamIds.Contains(fact.DreamId))
            .ToArrayAsync(cancellationToken);
        var images = await dbContext.DreamImages.AsNoTracking()
            .Where(image => image.UserSubject == subject && dreamIds.Contains(image.DreamId))
            .ToArrayAsync(cancellationToken);
        var voiceCaptures = await dbContext.VoiceCaptures.AsNoTracking()
            .Where(capture => capture.UserSubject == subject)
            .OrderBy(capture => capture.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var ledgerRows = await dbContext.AiCostLedger.AsNoTracking()
            .Where(row => row.UserSubject == subject)
            .OrderBy(row => row.CreatedAt)
            .ToArrayAsync(cancellationToken);

        return new UserDataExportResponse(
            DateTimeOffset.UtcNow,
            profile is null ? new ProfileResponse(null, null, null, "en", "UTC", ProfileTraitsDto.Empty, ConsentDto.Empty) : GetProfileHandler.Map(profile, encryptor),
            dreams.Select(dream => new UserDataExportDream(
                dream.Id,
                dream.CreatedAt,
                dream.Text,
                dream.Mood,
                dream.SleepQuality,
                JsonSerializer.Deserialize<string[]>(dream.TagsJson) ?? [],
                dream.OccurredAt,
                dream.JournalNote,
                dream.Status,
                dream.ResultJson,
                dream.ErrorMessage,
                facts.Where(fact => fact.DreamId == dream.Id)
                    .Select(fact => new UserDataExportFact(fact.FactType, fact.DisplayValue, fact.Score, fact.ExtractionConfidence))
                    .ToArray(),
                images.Where(image => image.DreamId == dream.Id)
                    .Select(image => new UserDataExportImage(
                        image.Id,
                        image.Status,
                        image.Style,
                        image.Status == DreamImageStatuses.Completed && !string.IsNullOrWhiteSpace(image.AssetKey)
                            ? assetStore.CreateReadUrl(image.AssetKey)
                            : null,
                        image.CreatedAt))
                    .ToArray(),
                interpretationFeedback.Where(feedback => feedback.DreamId == dream.Id)
                    .Select(feedback => new UserDataExportInterpretationFeedback(
                        feedback.Rating,
                        JsonSerializer.Deserialize<string[]>(feedback.ReasonsJson) ?? [],
                        feedback.Details,
                        feedback.UpdatedAt))
                    .SingleOrDefault(),
                deepInterpretations.Where(interpretation => interpretation.DreamId == dream.Id)
                    .Select(interpretation => new UserDataExportDeepInterpretation(
                        interpretation.ResultJson,
                        interpretation.SourcesJson,
                        interpretation.Provider,
                        interpretation.Model,
                        interpretation.PersonaVersion,
                        interpretation.CreatedAt))
                    .SingleOrDefault()))
                .ToArray(),
            voiceCaptures.Select(capture => new UserDataExportVoiceCapture(
                capture.Id,
                capture.Status,
                capture.DurationSeconds,
                capture.RetainRecording,
                capture.Transcript,
                capture.RetainRecording && capture.Status == VoiceCaptureStatuses.Completed
                    ? assetStore.CreateReadUrl(capture.SourceAssetKey)
                    : null,
                capture.CreatedAt))
                .ToArray(),
            ledgerRows.Select(row => new UserDataExportCost(
                row.Id,
                row.DreamId,
                row.Provider,
                row.Model,
                row.OperationType,
                row.Status,
                row.AttemptCount,
                row.LatencyMilliseconds,
                row.EstimatedCostUsd,
                row.CreatedAt))
                .ToArray());
    }
}
