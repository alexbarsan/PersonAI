using System.Text.Json;
using DreamLens.Api.Infrastructure.Identity;
using DreamLens.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DreamLens.Api.Features.Insights;

public sealed class GetInsightsHandler(DreamLensDbContext dbContext, ICurrentUser currentUser)
{
    public async Task<InsightsResponse> HandleAsync(CancellationToken cancellationToken)
    {
        var dreams = await dbContext.Dreams
            .AsNoTracking()
            .Where(dream => dream.UserSubject == currentUser.Subject && dream.Status == "completed")
            .OrderByDescending(dream => dream.CreatedAt)
            .ToArrayAsync(cancellationToken);

        var themes = dreams
            .SelectMany(ReadThemes)
            .GroupBy(theme => theme, StringComparer.OrdinalIgnoreCase)
            .Select(group => new ThemeInsightResponse(group.Key, group.Count()))
            .OrderByDescending(theme => theme.Count)
            .ThenBy(theme => theme.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new InsightsResponse(dreams.Length, CalculateCurrentStreakDays(dreams), themes);
    }

    private static IEnumerable<string> ReadThemes(DreamRecord dream)
    {
        if (string.IsNullOrWhiteSpace(dream.ResultJson))
        {
            yield break;
        }

        using var document = JsonDocument.Parse(dream.ResultJson);
        if (!document.RootElement.TryGetProperty("sections", out var sections) || sections.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var section in sections.EnumerateArray())
        {
            if (!section.TryGetProperty("title", out var title)
                || !string.Equals(title.GetString(), "Themes", StringComparison.OrdinalIgnoreCase)
                || !section.TryGetProperty("content", out var content)
                || content.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var theme in content.EnumerateArray())
            {
                if (theme.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(theme.GetString()))
                {
                    yield return theme.GetString()!;
                }
            }
        }
    }

    private static int CalculateCurrentStreakDays(IEnumerable<DreamRecord> dreams)
    {
        var dates = dreams
            .Select(ReadDreamDate)
            .Where(date => date is not null)
            .Select(date => date!.Value)
            .Distinct()
            .OrderByDescending(date => date)
            .ToArray();

        if (dates.Length == 0)
        {
            return 0;
        }

        var streak = 1;
        var previous = dates[0];
        foreach (var date in dates.Skip(1))
        {
            if (date == previous.AddDays(-1))
            {
                streak++;
                previous = date;
                continue;
            }

            break;
        }

        return streak;
    }

    private static DateOnly? ReadDreamDate(DreamRecord dream)
    {
        if (DateOnly.TryParse(dream.OccurredAt, out var occurredAt))
        {
            return occurredAt;
        }

        return DateOnly.FromDateTime(dream.CreatedAt.UtcDateTime);
    }
}
