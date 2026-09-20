using DreamLens.Api.Features.Insights;

namespace DreamLens.Api.Tests;

public sealed class DreamPatternRelationshipCalculatorTests
{
    private static readonly DreamPatternRelationshipOptions Options = new();

    [Fact]
    public void RanksDisproportionateOverlapAheadOfACommonGlobalEmotion()
    {
        var facts = new List<DreamPatternFact>();
        for (var index = 1; index <= 12; index++)
        {
            facts.Add(Fact(index, "location", "rivergate station"));
            facts.Add(Fact(index, "emotion", "anxiety"));
        }

        for (var index = 13; index <= 30; index++)
        {
            facts.Add(Fact(index, "emotion", "anxiety"));
        }

        for (var index = 1; index <= 6; index++)
        {
            facts.Add(Fact(index, "scenario", "being late"));
        }

        var result = Calculate(facts, 30, "location", "rivergate station");

        Assert.Collection(result.Relationships,
            first => Assert.Equal("being late", first.Name),
            second => Assert.Equal("anxiety", second.Name));
        Assert.Equal(2.5m, result.Relationships[0].Lift);
        Assert.Equal(1m, result.Relationships[1].Lift);
    }

    [Fact]
    public void ExcludesAOneDreamOverlap()
    {
        var facts = new List<DreamPatternFact>();
        for (var index = 1; index <= 3; index++) facts.Add(Fact(index, "symbol", "lantern"));
        facts.Add(Fact(1, "emotion", "hope"));
        facts.Add(Fact(4, "emotion", "hope"));

        var result = Calculate(facts, 5, "symbol", "lantern");

        Assert.Empty(result.Relationships);
    }

    [Fact]
    public void ExcludesTheSelectedPatternButKeepsTheSameNameFromAnotherType()
    {
        var facts = new List<DreamPatternFact>();
        for (var index = 1; index <= 3; index++)
        {
            facts.Add(Fact(index, "symbol", "water"));
            facts.Add(Fact(index, "theme", "water"));
        }

        var result = Calculate(facts, 3, "symbol", "water");

        var related = Assert.Single(result.Relationships);
        Assert.Equal("theme", related.PatternType);
        Assert.Equal("water", related.Name);
        Assert.DoesNotContain(result.Relationships, relation => relation.PatternType == "symbol" && relation.Name == "water");
    }

    [Fact]
    public void HandlesEmptyAndSmallJournalsWithoutDivisionErrors()
    {
        var noFacts = Calculate([], 0, "symbol", "water");
        var oneOccurrence = Calculate([Fact(1, "symbol", "water")], 1, "symbol", "water");

        Assert.Empty(noFacts.Relationships);
        Assert.False(noFacts.HasSufficientSourceEvidence);
        Assert.Empty(oneOccurrence.Relationships);
        Assert.Equal(1, oneOccurrence.SourceDreamCount);
        Assert.False(oneOccurrence.HasSufficientSourceEvidence);
    }

    [Fact]
    public void CalculatesConditionalAndBaselineRatesForARealJournalRelationship()
    {
        var facts = new List<DreamPatternFact>();
        for (var index = 1; index <= 18; index++) facts.Add(Fact(index, "location", "rivergate station"));
        for (var index = 1; index <= 11; index++) facts.Add(Fact(index, "emotion", "anxiety"));
        for (var index = 19; index <= 44; index++) facts.Add(Fact(index, "emotion", "anxiety"));

        var result = Calculate(facts, 116, "location", "rivergate station");
        var anxiety = Assert.Single(result.Relationships);

        Assert.Equal(11, anxiety.JointDreamCount);
        Assert.Equal(18, anxiety.SourceDreamCount);
        Assert.Equal(37, anxiety.TotalPatternDreamCount);
        Assert.Equal(0.611m, anxiety.CoOccurrenceRate);
        Assert.Equal(0.319m, anxiety.BaseRate);
        Assert.Equal(1.92m, anxiety.Lift);
        Assert.Equal("strong", anxiety.EvidenceLevel);
    }

    private static DreamPatternRelationshipResult Calculate(
        IReadOnlyCollection<DreamPatternFact> facts,
        int totalDreams,
        string type,
        string value) => DreamPatternRelationshipCalculator.Calculate(facts, totalDreams, type, value, Options);

    private static DreamPatternFact Fact(int dreamNumber, string type, string value) => new(
        Guid.Parse($"00000000-0000-0000-0000-{dreamNumber:D12}"),
        type,
        value,
        value);
}
