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
        var facts = await dbContext.DreamFacts.AsNoTracking()
            .Where(fact => fact.UserSubject == subject && dreamIds.Contains(fact.DreamId))
            .ToArrayAsync(cancellationToken);
        var images = await dbContext.DreamImages.AsNoTracking()
            .Where(image => image.UserSubject == subject && dreamIds.Contains(image.DreamId))
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
                    .ToArray()))
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
