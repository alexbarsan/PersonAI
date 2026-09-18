namespace DreamLens.Api.Features.Insights;

public sealed record InsightsResponse(
    int TotalDreams,
    int CurrentStreakDays,
    ThemeInsightResponse[] RecurringThemes,
    InsightDateRangeResponse? DateRange,
    FactInsightGroupResponse[] FactGroups,
    TimingPatternInsightResponse[] TimingPatterns,
    RelationshipInsightResponse[] Relationships,
    RelationshipReadinessResponse RelationshipReadiness,
    MonthlyDreamCountResponse[] MonthlyDreamCounts,
    JournalSynthesisResponse JournalSynthesis);

public sealed record ThemeInsightResponse(string Name, int Count);

public sealed record InsightDateRangeResponse(DateOnly Start, DateOnly End);

public sealed record FactInsightGroupResponse(string Type, string Title, FactInsightResponse[] Facts);

public sealed record FactInsightResponse(
    string Value,
    int Count,
    decimal PercentageOfDreams,
    decimal? AverageScore,
    decimal? AverageExtractionConfidence,
    string[] SourceFields,
    DateOnly? LastObservedAt);

public sealed record DreamObservationResponse(
    string Type,
    string Value,
    int TotalDreams,
    decimal? AverageExtractionConfidence,
    string[] SourceFields,
    DreamObservationEvidenceResponse[] Evidence);

public sealed record DreamObservationEvidenceResponse(
    Guid DreamId,
    string Title,
    DateOnly ObservedAt,
    decimal? Score,
    decimal? ExtractionConfidence,
    string SourceField,
    string SourceSchemaVersion,
    string NormalizationVersion);

public sealed record TimingPatternInsightResponse(
    string Type,
    string Value,
    int Occurrences,
    int WeekdayDreams,
    int WeekendDreams,
    decimal WeekdayRate,
    decimal WeekendRate,
    decimal WeekdayToWeekendRatio);

public sealed record RelationshipInsightResponse(
    string FirstType,
    string FirstValue,
    string SecondType,
    string SecondValue,
    int SharedDreams,
    int FirstDreams,
    int SecondDreams,
    decimal SharedOfSmallerPatternPercent,
    decimal RelativeLift,
    DreamRelationshipEvidenceResponse[] Evidence);

public sealed record RelationshipReadinessResponse(
    int MinimumCompletedDreams,
    int CompletedDreams,
    int QualifiedFactPatterns,
    int SupportedRelationships);

public sealed record DreamRelationshipEvidenceResponse(
    Guid DreamId,
    string Title,
    DateOnly ObservedAt,
    decimal? FirstExtractionConfidence,
    decimal? SecondExtractionConfidence);

public sealed record MonthlyDreamCountResponse(DateOnly Month, int Count);

public sealed record JournalSynthesisResponse(
    string Status,
    int MinimumCompletedDreams,
    int CompletedDreams,
    int? SourceDreamCount,
    DateTimeOffset? GeneratedAt,
    string? Summary,
    JournalSynthesisObservationResponse[] Observations,
    string[] ReflectionQuestions);

public sealed record JournalSynthesisObservationResponse(
    string Title,
    string Reflection,
    JournalSynthesisEvidenceResponse[] Evidence);

public sealed record JournalSynthesisEvidenceResponse(
    Guid DreamId,
    string Title,
    DateOnly ObservedAt);
