using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Insights;

public sealed class GetInsightsHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
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

        var factGroups = BuildFactGroups(facts, dreams.Length);
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
            BuildMonthlyDreamCounts(dates));
    }

    private static FactInsightGroupResponse[] BuildFactGroups(IEnumerable<DreamFactRecord> facts, int totalDreams)
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
                        return new FactInsightResponse(
                            rows.OrderByDescending(value => value.DisplayValue.Length).First().DisplayValue,
                            count,
                            totalDreams == 0 ? 0 : Math.Round(count * 100m / totalDreams, 1),
                            scoredRows.Length == 0 ? null : Math.Round(scoredRows.Average(value => value.Score!.Value), 2));
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
}
