using System.Text.Json;
using DreamLens.Api.Features.Dreams;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Insights;

public sealed class GetInsightsHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    private const int MinimumRelationshipPopulation = 6;
    private const int MinimumRelationshipOccurrences = 3;
    private const decimal MinimumRelationshipConfidence = 0.60m;
    private const decimal MinimumRelationshipLift = 1.25m;
    private static readonly string[] FactTypeOrder = ["symbol", "emotion", "theme", "person", "location", "object", "scenario"];

    private static readonly IReadOnlyDictionary<string, string> FactGroupTitles = new Dictionary<string, string>
    {
        ["symbol"] = "Recurring symbols",
        ["emotion"] = "Frequent emotions",
        ["theme"] = "Recurring themes",
        ["person"] = "Recurring people",
        ["location"] = "Recurring locations",
        ["object"] = "Recurring objects",
        ["scenario"] = "Recurring scenarios"
    };

    public async Task<InsightsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var dreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject && dream.Status == "completed")
            .OrderByDescending(dream => dream.CreatedAt)
            .ToArrayAsync(cancellationToken);
        var dreamDates = dreams.ToDictionary(dream => dream.Id, ReadDreamDate);
        var dreamIds = dreams.Select(dream => dream.Id).ToArray();
        var facts = await dbContext.DreamFacts
            .AsNoTracking()
            .Where(fact => fact.UserSubject == currentUser.Subject && dreamIds.Contains(fact.DreamId))
            .ToArrayAsync(cancellationToken);

        var factGroups = BuildFactGroups(facts, dreams.Length, dreams.ToDictionary(dream => dream.Id));
        var recurringThemes = factGroups
            .SingleOrDefault(group => group.Type == "theme")?.Facts
            .Select(fact => new ThemeInsightResponse(fact.Value, fact.Count))
            .ToArray()
            ?? ReadLegacyThemes(dreams);
        var dates = dreamDates.Values.Where(date => date is not null).Select(date => date!.Value).ToArray();

        return new InsightsResponse(
            dreams.Length,
            CalculateCurrentStreakDays(dates),
            recurringThemes,
            dates.Length == 0 ? null : new InsightDateRangeResponse(dates.Min(), dates.Max()),
            factGroups,
            BuildTimingPatterns(facts, dreamDates),
            BuildRelationships(facts, dreams.Length, dreams.ToDictionary(dream => dream.Id)),
            BuildMonthlyDreamCounts(dates));
    }

    private static FactInsightGroupResponse[] BuildFactGroups(
        IEnumerable<DreamFactRecord> facts,
        int totalDreams,
        IReadOnlyDictionary<Guid, DreamRecord> dreamsById)
    {
        return facts
            .Where(fact => FactGroupTitles.ContainsKey(fact.FactType))
            .GroupBy(fact => fact.FactType)
            .OrderBy(group => Array.IndexOf(FactTypeOrder, group.Key))
            .Select(group => new FactInsightGroupResponse(
                group.Key,
                FactGroupTitles[group.Key],
                group.GroupBy(fact => fact.NormalizedValue)
                    .Select(values =>
                    {
                        var rows = values.ToArray();
                        var count = rows.Select(value => value.DreamId).Distinct().Count();
                        var scoredRows = rows.Where(value => value.Score is not null).ToArray();
                        var confidenceRows = rows.Where(value => value.ExtractionConfidence is not null).ToArray();
                        var observedDates = rows
                            .Select(value => dreamsById.TryGetValue(value.DreamId, out var dream) ? ReadDreamDate(dream) : null)
                            .Where(date => date is not null)
                            .Select(date => date!.Value)
                            .ToArray();
                        return new FactInsightResponse(
                            rows.OrderByDescending(value => value.DisplayValue.Length).First().DisplayValue,
                            count,
                            totalDreams == 0 ? 0 : Math.Round(count * 100m / totalDreams, 1),
                            scoredRows.Length == 0 ? null : Math.Round(scoredRows.Average(value => value.Score!.Value), 2),
                            confidenceRows.Length == 0 ? null : Math.Round(confidenceRows.Average(value => value.ExtractionConfidence!.Value), 2),
                            rows.Select(value => value.SourceField).Distinct(StringComparer.OrdinalIgnoreCase).Order(StringComparer.OrdinalIgnoreCase).ToArray(),
                            observedDates.Length == 0 ? null : observedDates.Max());
                    })
                    .OrderByDescending(fact => fact.Count)
                    .ThenBy(fact => fact.Value, StringComparer.OrdinalIgnoreCase)
                    .Take(8)
                    .ToArray()))
            .ToArray();
    }

    private static TimingPatternInsightResponse[] BuildTimingPatterns(
        IEnumerable<DreamFactRecord> facts,
        IReadOnlyDictionary<Guid, DateOnly?> dreamDates)
    {
        var datedDreams = dreamDates.Where(pair => pair.Value is not null).ToArray();
        var weekdayDreams = datedDreams.Count(pair => !IsWeekend(pair.Value!.Value));
        var weekendDreams = datedDreams.Length - weekdayDreams;
        if (datedDreams.Length < 3 || weekdayDreams == 0 || weekendDreams == 0)
        {
            return [];
        }

        return facts
            .Where(fact => FactGroupTitles.ContainsKey(fact.FactType) && dreamDates.TryGetValue(fact.DreamId, out var date) && date is not null)
            .GroupBy(fact => new { fact.FactType, fact.NormalizedValue })
            .Select(group =>
            {
                var uniqueDreamIds = group.Select(fact => fact.DreamId).Distinct().ToArray();
                var weekdayCount = uniqueDreamIds.Count(id => !IsWeekend(dreamDates[id]!.Value));
                var weekendCount = uniqueDreamIds.Length - weekdayCount;
                var weekdayRate = Math.Round(weekdayCount * 100m / weekdayDreams, 1);
                var weekendRate = Math.Round(weekendCount * 100m / weekendDreams, 1);
                return new
                {
                    group.Key.FactType,
                    Value = group.First().DisplayValue,
                    Occurrences = uniqueDreamIds.Length,
                    WeekdayCount = weekdayCount,
                    WeekendCount = weekendCount,
                    WeekdayRate = weekdayRate,
                    WeekendRate = weekendRate,
                    Ratio = weekendRate == 0 ? 0 : Math.Round(weekdayRate / weekendRate, 1)
                };
            })
            .Where(pattern => pattern.Occurrences >= 3 && pattern.WeekendRate > 0 && pattern.Ratio >= 1.5m)
            .OrderByDescending(pattern => pattern.Ratio)
            .ThenByDescending(pattern => pattern.Occurrences)
            .Take(5)
            .Select(pattern => new TimingPatternInsightResponse(
                pattern.FactType,
                pattern.Value,
                pattern.Occurrences,
                pattern.WeekdayCount,
                pattern.WeekendCount,
                pattern.WeekdayRate,
                pattern.WeekendRate,
                pattern.Ratio))
            .ToArray();
    }

    private static RelationshipInsightResponse[] BuildRelationships(
        IEnumerable<DreamFactRecord> facts,
        int totalDreams,
        IReadOnlyDictionary<Guid, DreamRecord> dreamsById)
    {
        if (totalDreams < MinimumRelationshipPopulation)
        {
            return [];
        }

        var qualified = facts
            .Where(fact => FactGroupTitles.ContainsKey(fact.FactType)
                && !string.Equals(fact.SourceField, "unknown", StringComparison.OrdinalIgnoreCase)
                && fact.ExtractionConfidence >= MinimumRelationshipConfidence
                && dreamsById.ContainsKey(fact.DreamId))
            .GroupBy(fact => new RelationshipFactKey(fact.FactType, fact.NormalizedValue))
            .SelectMany(group => group
                .GroupBy(fact => fact.DreamId)
                .Select(dreamFacts => dreamFacts
                    .OrderByDescending(fact => fact.ExtractionConfidence)
                    .ThenByDescending(fact => fact.DisplayValue.Length)
                    .First()))
            .ToArray();
        if (qualified.Length == 0)
        {
            return [];
        }

        var patternCounts = qualified
            .GroupBy(fact => new RelationshipFactKey(fact.FactType, fact.NormalizedValue))
            .ToDictionary(group => group.Key, group => group.Select(fact => fact.DreamId).Distinct().Count());
        var pairs = qualified
            .GroupBy(fact => fact.DreamId)
            .SelectMany(group =>
            {
                var items = group.ToArray();
                return items.SelectMany((first, firstIndex) => items
                    .Skip(firstIndex + 1)
                    .Where(second => !string.Equals(first.FactType, second.FactType, StringComparison.OrdinalIgnoreCase))
                    .Select(second => RelationshipPair.Create(first, second)));
            })
            .GroupBy(pair => pair.Key)
            .Select(group => new { group.Key, Evidence = group.ToArray() })
            .Select(group =>
            {
                var firstDreams = patternCounts[group.Key.First];
                var secondDreams = patternCounts[group.Key.Second];
                var sharedDreams = group.Evidence.Select(item => item.DreamId).Distinct().Count();
                return new { group.Key, group.Evidence, FirstDreams = firstDreams, SecondDreams = secondDreams, SharedDreams = sharedDreams };
            })
            .Where(group => group.SharedDreams >= MinimumRelationshipOccurrences
                && group.FirstDreams >= MinimumRelationshipOccurrences
                && group.SecondDreams >= MinimumRelationshipOccurrences)
            .Select(group => new
            {
                group.Key,
                group.Evidence,
                group.FirstDreams,
                group.SecondDreams,
                group.SharedDreams,
                SharedOfSmallerPatternPercent = Math.Round(group.SharedDreams * 100m / Math.Min(group.FirstDreams, group.SecondDreams), 1),
                RelativeLift = Math.Round(group.SharedDreams * totalDreams / (decimal)(group.FirstDreams * group.SecondDreams), 2)
            })
            .Where(group => group.SharedOfSmallerPatternPercent >= 50m && group.RelativeLift >= MinimumRelationshipLift)
            .OrderByDescending(group => group.RelativeLift)
            .ThenByDescending(group => group.SharedDreams)
            .ThenByDescending(group => group.SharedOfSmallerPatternPercent)
            .ThenBy(group => group.Key.First.Type, StringComparer.Ordinal)
            .ThenBy(group => group.Key.First.NormalizedValue, StringComparer.Ordinal)
            .Take(5)
            .Select(group => new RelationshipInsightResponse(
                group.Key.First.Type,
                FindDisplayValue(qualified, group.Key.First),
                group.Key.Second.Type,
                FindDisplayValue(qualified, group.Key.Second),
                group.SharedDreams,
                group.FirstDreams,
                group.SecondDreams,
                group.SharedOfSmallerPatternPercent,
                group.RelativeLift,
                group.Evidence
                    .OrderByDescending(item => ReadObservedDate(dreamsById[item.DreamId]))
                    .Take(5)
                    .Select(item => new DreamRelationshipEvidenceResponse(
                        item.DreamId,
                        DreamTitleGenerator.Create(dreamsById[item.DreamId].Title, DreamMapper.ReadSummary(dreamsById[item.DreamId]), dreamsById[item.DreamId].Text),
                        ReadObservedDate(dreamsById[item.DreamId]),
                        item.First.ExtractionConfidence,
                        item.Second.ExtractionConfidence))
                    .ToArray()))
            .ToArray();

        return pairs;
    }

    private static string FindDisplayValue(IEnumerable<DreamFactRecord> facts, RelationshipFactKey key) => facts
        .Where(fact => string.Equals(fact.FactType, key.Type, StringComparison.Ordinal)
            && string.Equals(fact.NormalizedValue, key.NormalizedValue, StringComparison.Ordinal))
        .OrderByDescending(fact => fact.DisplayValue.Length)
        .Select(fact => fact.DisplayValue)
        .First();

    private sealed record RelationshipFactKey(string Type, string NormalizedValue);

    private sealed record RelationshipPairKey(RelationshipFactKey First, RelationshipFactKey Second);

    private sealed record RelationshipPair(RelationshipPairKey Key, Guid DreamId, DreamFactRecord First, DreamFactRecord Second)
    {
        public static RelationshipPair Create(DreamFactRecord first, DreamFactRecord second)
        {
            var firstKey = new RelationshipFactKey(first.FactType, first.NormalizedValue);
            var secondKey = new RelationshipFactKey(second.FactType, second.NormalizedValue);
            return Compare(firstKey, secondKey) <= 0
                ? new RelationshipPair(new RelationshipPairKey(firstKey, secondKey), first.DreamId, first, second)
                : new RelationshipPair(new RelationshipPairKey(secondKey, firstKey), first.DreamId, second, first);
        }

        private static int Compare(RelationshipFactKey left, RelationshipFactKey right)
        {
            var typeComparison = Array.IndexOf(FactTypeOrder, left.Type).CompareTo(Array.IndexOf(FactTypeOrder, right.Type));
            return typeComparison != 0 ? typeComparison : string.Compare(left.NormalizedValue, right.NormalizedValue, StringComparison.Ordinal);
        }
    }

    private static MonthlyDreamCountResponse[] BuildMonthlyDreamCounts(IEnumerable<DateOnly> dates)
    {
        return dates
            .GroupBy(date => new DateOnly(date.Year, date.Month, 1))
            .OrderBy(group => group.Key)
            .Select(group => new MonthlyDreamCountResponse(group.Key, group.Count()))
            .TakeLast(12)
            .ToArray();
    }

    private static ThemeInsightResponse[] ReadLegacyThemes(IEnumerable<DreamRecord> dreams)
    {
        return dreams
            .SelectMany(ReadThemes)
            .GroupBy(theme => theme, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ThemeInsightResponse(group.Key, group.Count()))
            .OrderByDescending(theme => theme.Count)
            .ThenBy(theme => theme.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<string> ReadThemes(DreamRecord dream)
    {
        if (string.IsNullOrWhiteSpace(dream.ResultJson)) yield break;
        using var document = JsonDocument.Parse(dream.ResultJson);
        if (!document.RootElement.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array) yield break;
        foreach (var section in sections.EnumerateArray())
        {
            if (!section.TryGetProperty("title", out var title)
                || !string.Equals(title.GetString(), "Themes", StringComparison.OrdinalIgnoreCase)
                || !section.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array) continue;
            foreach (var theme in content.EnumerateArray())
            {
                if (theme.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(theme.GetString())) yield return theme.GetString()!;
            }
        }
    }

    private static int CalculateCurrentStreakDays(IEnumerable<DateOnly> dates)
    {
        var ordered = dates.Distinct().OrderByDescending(date => date).ToArray();
        if (ordered.Length == 0) return 0;
        var streak = 1;
        var previous = ordered[0];
        foreach (var date in ordered.Skip(1))
        {
            if (date != previous.AddDays(-1)) break;
            streak++;
            previous = date;
        }
        return streak;
    }

    private static bool IsWeekend(DateOnly date) => date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

    private static DateOnly? ReadDreamDate(DreamRecord dream)
    {
        return DateOnly.TryParse(dream.OccurredAt, out var occurredAt)
            ? occurredAt
            : DateOnly.FromDateTime(dream.CreatedAt.UtcDateTime);
    }

    private static DateOnly ReadObservedDate(DreamRecord dream) => ReadDreamDate(dream)
        ?? DateOnly.FromDateTime(dream.CreatedAt.UtcDateTime);
}
