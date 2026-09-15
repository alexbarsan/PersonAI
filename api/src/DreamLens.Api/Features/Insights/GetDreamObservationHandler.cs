using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Insights;

public sealed class GetDreamObservationHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<DreamObservationResponse?> HandleAsync(
        string type,
        string value,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalizedType = type.Trim().ToLowerInvariant();
        var normalizedValue = DreamFactNormalization.Normalize(value);
        var facts = await dbContext.DreamFacts
            .AsNoTracking()
            .Where(fact => fact.UserSubject == currentUser.Subject
                && fact.FactType == normalizedType
                && fact.NormalizedValue == normalizedValue)
            .OrderByDescending(fact => fact.CreatedAt)
            .Take(25)
            .ToArrayAsync(cancellationToken);
        if (facts.Length == 0)
        {
            return null;
        }

        var dreamIds = facts.Select(fact => fact.DreamId).Distinct().ToArray();
        var dreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject && dreamIds.Contains(dream.Id))
            .ToDictionaryAsync(dream => dream.Id, cancellationToken);
        var evidence = facts
            .Where(fact => dreams.ContainsKey(fact.DreamId))
            .Select(fact =>
            {
                var dream = dreams[fact.DreamId];
                return new DreamObservationEvidenceResponse(
                    dream.Id,
                    DreamTitleGenerator.Create(dream.Title, DreamMapper.ReadSummary(dream), dream.Text),
                    ReadObservedAt(dream),
                    fact.Score,
                    fact.ExtractionConfidence,
                    fact.SourceField,
                    fact.SourceSchemaVersion,
                    fact.NormalizationVersion);
            })
            .OrderByDescending(item => item.ObservedAt)
            .ToArray();
        var confidenceRows = facts.Where(fact => fact.ExtractionConfidence is not null).ToArray();

        return new DreamObservationResponse(
            normalizedType,
            facts[0].DisplayValue,
            evidence.Select(item => item.DreamId).Distinct().Count(),
            confidenceRows.Length == 0 ? null : Math.Round(confidenceRows.Average(fact => fact.ExtractionConfidence!.Value), 2),
            facts.Select(fact => fact.SourceField).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            evidence);
    }

    private static DateOnly ReadObservedAt(DreamRecord dream) => DateOnly.TryParse(dream.OccurredAt, out var occurredAt)
        ? occurredAt
        : DateOnly.FromDateTime(dream.CreatedAt.UtcDateTime);
}
