namespace DreamLens.Api.Features.Insights;

public sealed record InsightsResponse(
    int TotalDreams,
    int CurrentStreakDays,
    ThemeInsightResponse[] RecurringThemes,
    InsightDateRangeResponse? DateRange,
    FactInsightGroupResponse[] FactGroups,
    TimingPatternInsightResponse[] TimingPatterns,
    MonthlyDreamCountResponse[] MonthlyDreamCounts);

public sealed record ThemeInsightResponse(string Name, int Count);

public sealed record InsightDateRangeResponse(DateOnly Start, DateOnly End);

public sealed record FactInsightGroupResponse(string Type, string Title, FactInsightResponse[] Facts);

public sealed record FactInsightResponse(string Value, int Count, decimal PercentageOfDreams, decimal? AverageScore);

public sealed record TimingPatternInsightResponse(
    string Type,
    string Value,
    int Occurrences,
    int WeekdayDreams,
    int WeekendDreams,
    decimal WeekdayRate,
    decimal WeekendRate,
    decimal WeekdayToWeekendRatio);

public sealed record MonthlyDreamCountResponse(DateOnly Month, int Count);
