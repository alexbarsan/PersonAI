using System.Text.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Jobs;
using DreamLens.Api.Infrastructure.Persistence;
using DreamLens.Api.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DreamLens.Api.Features.Insights;

public sealed class GetDreamObservationHandler(
    DreamLensDbContext dbContext,
    ICurrentUser currentUser,
    IStringEncryptor encryptor,
    IOptions<DreamPatternRelationshipOptions> relationshipOptions,
    IOptions<DreamJournalSynthesisOptions> synthesisOptions)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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
        var allDreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject && dream.Status == "completed")
            .ToArrayAsync(cancellationToken);
        var allDreamIds = allDreams.Select(dream => dream.Id).ToArray();
        var facts = await dbContext.DreamFacts
            .AsNoTracking()
            .Where(fact => fact.UserSubject == currentUser.Subject
                && fact.FactType == normalizedType
                && fact.NormalizedValue == normalizedValue
                && allDreamIds.Contains(fact.DreamId))
            .OrderByDescending(fact => fact.CreatedAt)
            .ToArrayAsync(cancellationToken);
        if (facts.Length == 0)
        {
            return null;
        }

        var dreamIds = facts.Select(fact => fact.DreamId).Distinct().ToArray();
        var dreams = allDreams
            .Where(dream => dreamIds.Contains(dream.Id))
            .ToDictionary(dream => dream.Id);
        var allFacts = await dbContext.DreamFacts
            .AsNoTracking()
            .Where(fact => fact.UserSubject == currentUser.Subject
                && allDreamIds.Contains(fact.DreamId))
            .ToArrayAsync(cancellationToken);
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
            .Take(25)
            .ToArray();
        var confidenceRows = facts.Where(fact => fact.ExtractionConfidence is not null).ToArray();
        var interpretation = await ReadInterpretationAsync(normalizedType, normalizedValue, synthesisOptions.Value.PromptVersion, cancellationToken);
        var monthlyOccurrences = BuildMonthlyOccurrences(facts, dreams);
        var observedDates = facts
            .Where(fact => dreams.ContainsKey(fact.DreamId))
            .Select(fact => ReadObservedAt(dreams[fact.DreamId]))
            .ToArray();
        if (observedDates.Length == 0)
        {
            return null;
        }
        var relationships = DreamPatternRelationshipCalculator.Calculate(
            allFacts.Select(fact => new DreamPatternFact(fact.DreamId, fact.FactType, fact.NormalizedValue, fact.DisplayValue)),
            allDreams.Length,
            normalizedType,
            normalizedValue,
            relationshipOptions.Value);

        return new DreamObservationResponse(
            normalizedType,
            facts[0].DisplayValue,
            facts.Select(fact => fact.DreamId).Distinct().Count(),
            confidenceRows.Length == 0 ? null : Math.Round(confidenceRows.Average(fact => fact.ExtractionConfidence!.Value), 2),
            facts.Select(fact => fact.SourceField).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
            evidence,
            observedDates.Min(),
            observedDates.Max(),
            interpretation,
            relationships.Relationships.Select(relationship => new DreamPatternRelationshipResponse(
                relationship.PatternId,
                relationship.PatternType,
                relationship.Name,
                relationship.JointDreamCount,
                relationship.SourceDreamCount,
                relationship.TotalPatternDreamCount,
                relationship.CoOccurrenceRate,
                relationship.BaseRate,
                relationship.Lift,
                relationship.EvidenceLevel)).ToArray(),
            new DreamPatternRelationshipReadinessResponse(
                relationships.CompletedDreamCount,
                relationships.SourceDreamCount,
                Math.Max(2, relationshipOptions.Value.MinimumSourceDreamCount),
                Math.Max(2, relationshipOptions.Value.MinimumJointDreamCount),
                relationships.HasSufficientSourceEvidence),
            DreamPatternMeaningCatalog.GetResearchLenses(normalizedType),
            monthlyOccurrences,
            CalculateTrendDirection(monthlyOccurrences));
    }

    private async Task<DreamPatternInterpretationResponse?> ReadInterpretationAsync(
        string type,
        string normalizedValue,
        string requiredPromptVersion,
        CancellationToken cancellationToken)
    {
        var synthesis = await dbContext.DreamJournalSyntheses.AsNoTracking()
            .SingleOrDefaultAsync(item => item.UserSubject == currentUser.Subject, cancellationToken);
        if (synthesis is null)
        {
            return null;
        }

        if (!string.Equals(synthesis.PromptVersion, requiredPromptVersion, StringComparison.Ordinal))
        {
            return null;
        }

        var document = JsonSerializer.Deserialize<DreamJournalSynthesisDocument>(
            encryptor.Decrypt(synthesis.EncryptedResultJson),
            JsonOptions);
        var pattern = document?.Patterns?.FirstOrDefault(item =>
            string.Equals(item.Type, type, StringComparison.OrdinalIgnoreCase)
            && DreamFactNormalization.Normalize(item.Value) == normalizedValue);
        return pattern is null
            ? null
            : new DreamPatternInterpretationResponse(
                pattern.Reflection,
                synthesis.PromptVersion,
                synthesis.GeneratedAt,
                pattern.EvidenceDreamIds);
    }

    private static DreamPatternMonthlyCountResponse[] BuildMonthlyOccurrences(
        IEnumerable<DreamFactRecord> facts,
        IReadOnlyDictionary<Guid, DreamRecord> dreams)
    {
        var observedMonths = facts
            .Where(fact => dreams.ContainsKey(fact.DreamId))
            .Select(fact => ReadObservedAt(dreams[fact.DreamId]))
            .Select(date => new DateOnly(date.Year, date.Month, 1))
            .GroupBy(month => month)
            .ToDictionary(group => group.Key, group => group.Count());
        if (observedMonths.Count == 0)
        {
            return [];
        }

        var currentMonth = new DateOnly(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var lastMonth = observedMonths.Keys.Max() > currentMonth ? observedMonths.Keys.Max() : currentMonth;
        var firstMonth = observedMonths.Keys.Min();
        var earliestDisplayedMonth = lastMonth.AddMonths(-11);
        if (firstMonth > earliestDisplayedMonth)
        {
            earliestDisplayedMonth = firstMonth;
        }

        return Enumerable.Range(0, ((lastMonth.Year - earliestDisplayedMonth.Year) * 12) + lastMonth.Month - earliestDisplayedMonth.Month + 1)
            .Select(offset => earliestDisplayedMonth.AddMonths(offset))
            .Select(month => new DreamPatternMonthlyCountResponse(month, observedMonths.GetValueOrDefault(month)))
            .ToArray();
    }

    private static string CalculateTrendDirection(IReadOnlyList<DreamPatternMonthlyCountResponse> months)
    {
        if (months.Count < 4)
        {
            return "not_enough_data";
        }

        var window = Math.Min(3, months.Count / 2);
        var recent = months.TakeLast(window).Average(item => item.Count);
        var previous = months.Skip(months.Count - (window * 2)).Take(window).Average(item => item.Count);
        if (recent > previous + 0.25)
        {
            return "increasing";
        }

        return recent < previous - 0.25 ? "decreasing" : "steady";
    }

    private static DateOnly ReadObservedAt(DreamRecord dream) => DateOnly.TryParse(dream.OccurredAt, out var occurredAt)
        ? occurredAt
        : DateOnly.FromDateTime(dream.CreatedAt.UtcDateTime);
}
