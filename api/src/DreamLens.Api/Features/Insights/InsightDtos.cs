namespace DreamLens.Api.Features.Insights;

public sealed record InsightsResponse(int TotalDreams, int CurrentStreakDays, ThemeInsightResponse[] RecurringThemes);

public sealed record ThemeInsightResponse(string Name, int Count);
