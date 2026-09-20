namespace DreamLens.Api.Features.Insights;

public sealed class DreamPatternRelationshipOptions
{
    public const string SectionName = "DreamMapRelationships";

    public int MinimumSourceDreamCount { get; set; } = 3;

    public int MinimumJointDreamCount { get; set; } = 2;

    public decimal MinimumCoOccurrenceRate { get; set; } = 0.20m;

    public decimal MinimumLift { get; set; } = 1.40m;

    public int MaximumRelatedPatterns { get; set; } = 8;
}

public sealed record DreamPatternFact(
    Guid DreamId,
    string Type,
    string NormalizedValue,
    string DisplayValue);

public sealed record DreamPatternRelationship(
    string PatternId,
    string PatternType,
    string Name,
    int JointDreamCount,
    int SourceDreamCount,
    int TotalPatternDreamCount,
    decimal CoOccurrenceRate,
    decimal BaseRate,
    decimal Lift,
    string EvidenceLevel);

public sealed record DreamPatternRelationshipResult(
    int CompletedDreamCount,
    int SourceDreamCount,
    bool HasSufficientSourceEvidence,
    DreamPatternRelationship[] Relationships);

public static class DreamPatternRelationshipCalculator
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.Ordinal)
    {
        "symbol", "emotion", "theme", "person", "location", "object", "scenario"
    };

    public static DreamPatternRelationshipResult Calculate(
        IEnumerable<DreamPatternFact> facts,
        int completedDreamCount,
        string sourceType,
        string sourceNormalizedValue,
        DreamPatternRelationshipOptions options)
    {
        var safeOptions = new DreamPatternRelationshipOptions
        {
            MinimumSourceDreamCount = Math.Max(2, options.MinimumSourceDreamCount),
            MinimumJointDreamCount = Math.Max(2, options.MinimumJointDreamCount),
            MinimumCoOccurrenceRate = Math.Clamp(options.MinimumCoOccurrenceRate, 0m, 1m),
            MinimumLift = Math.Max(1m, options.MinimumLift),
            MaximumRelatedPatterns = Math.Clamp(options.MaximumRelatedPatterns, 1, 12)
        };
        var sourceKey = new PatternKey(sourceType, sourceNormalizedValue);
        var distinctFacts = facts
            .Where(fact => SupportedTypes.Contains(fact.Type)
                && !string.IsNullOrWhiteSpace(fact.NormalizedValue))
            .GroupBy(fact => new { fact.DreamId, fact.Type, fact.NormalizedValue })
            .Select(group => group.OrderByDescending(fact => fact.DisplayValue.Length).First())
            .ToArray();
        var patterns = distinctFacts
            .GroupBy(fact => new PatternKey(fact.Type, fact.NormalizedValue))
            .ToDictionary(
                group => group.Key,
                group => new PatternOccurrence(
                    group.OrderByDescending(fact => fact.DisplayValue.Length).First().DisplayValue,
                    group.Select(fact => fact.DreamId).ToHashSet()));
        if (!patterns.TryGetValue(sourceKey, out var source) || completedDreamCount <= 0)
        {
            return new DreamPatternRelationshipResult(completedDreamCount, 0, false, []);
        }

        if (source.DreamIds.Count < safeOptions.MinimumSourceDreamCount)
        {
            return new DreamPatternRelationshipResult(completedDreamCount, source.DreamIds.Count, false, []);
        }

        var relationships = patterns
            .Where(pair => pair.Key != sourceKey)
            .Select(pair => BuildRelationship(pair.Key, pair.Value, source, completedDreamCount))
            .Where(candidate => candidate.JointDreamCount >= safeOptions.MinimumJointDreamCount
                && (candidate.CoOccurrenceRate >= safeOptions.MinimumCoOccurrenceRate
                    || candidate.Lift >= safeOptions.MinimumLift))
            .OrderByDescending(candidate => StrengthScore(candidate))
            .ThenByDescending(candidate => candidate.JointDreamCount)
            .ThenByDescending(candidate => candidate.Lift)
            .ThenByDescending(candidate => candidate.CoOccurrenceRate)
            .ThenBy(candidate => candidate.PatternType, StringComparer.Ordinal)
            .ThenBy(candidate => candidate.Name, StringComparer.OrdinalIgnoreCase)
            .Take(safeOptions.MaximumRelatedPatterns)
            .ToArray();

        return new DreamPatternRelationshipResult(completedDreamCount, source.DreamIds.Count, true, relationships);
    }

    private static DreamPatternRelationship BuildRelationship(
        PatternKey key,
        PatternOccurrence candidate,
        PatternOccurrence source,
        int completedDreamCount)
    {
        var joint = source.DreamIds.Intersect(candidate.DreamIds).Count();
        var coOccurrenceRate = source.DreamIds.Count == 0 ? 0m : joint / (decimal)source.DreamIds.Count;
        var baseRate = completedDreamCount == 0 ? 0m : candidate.DreamIds.Count / (decimal)completedDreamCount;
        var lift = baseRate == 0m ? 0m : coOccurrenceRate / baseRate;
        return new DreamPatternRelationship(
            $"{key.Type}:{key.NormalizedValue}",
            key.Type,
            candidate.DisplayValue,
            joint,
            source.DreamIds.Count,
            candidate.DreamIds.Count,
            Math.Round(coOccurrenceRate, 3),
            Math.Round(baseRate, 3),
            Math.Round(lift, 2),
            EvidenceLevel(joint, coOccurrenceRate, lift));
    }

    // Emphasize disproportionate overlap while still rewarding repeated observations.
    private static decimal StrengthScore(DreamPatternRelationship relationship) =>
        relationship.CoOccurrenceRate * relationship.Lift * relationship.Lift * (decimal)Math.Sqrt(relationship.JointDreamCount);

    private static string EvidenceLevel(int jointDreamCount, decimal coOccurrenceRate, decimal lift) =>
        jointDreamCount >= 4 && coOccurrenceRate >= 0.35m && lift >= 1.8m ? "strong"
        : jointDreamCount >= 3 && (coOccurrenceRate >= 0.35m || lift >= 1.6m) ? "moderate"
        : "weak";

    private sealed record PatternKey(string Type, string NormalizedValue);

    private sealed record PatternOccurrence(string DisplayValue, HashSet<Guid> DreamIds);
}
