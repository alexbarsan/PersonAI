namespace DreamLens.Api.Features.Insights;

public static class DreamPatternMeaningCatalog
{
    private static readonly DreamPatternMeaningSourceResponse ContinuitySource = new(
        "schredl-hofmann-2003",
        "Continuity between waking activities and dream activities",
        "https://pubmed.ncbi.nlm.nih.gov/12763010/",
        2003);

    private static readonly DreamPatternMeaningSourceResponse MemorySource = new(
        "malinowski-horton-2014",
        "Memory sources of dreams: the incorporation of autobiographical rather than episodic experiences",
        "https://pubmed.ncbi.nlm.nih.gov/24635722/",
        2014);

    private static readonly DreamPatternMeaningSourceResponse ContentSource = new(
        "fogli-et-al-2020",
        "Our dreams, our selves: automatic analysis of dream reports",
        "https://pubmed.ncbi.nlm.nih.gov/32968499/",
        2020);

    public static DreamPatternMeaningResponse[] Get(string type, string value)
    {
        var displayValue = value.Trim();
        var association = type switch
        {
            "person" => $"A recurring person such as {displayValue} can be explored through your own relationship, memories, and recent concerns rather than treated as a literal message about that person.",
            "location" => $"A recurring place such as {displayValue} may combine fragments of autobiographical memory. Its personal history and emotional tone are more useful than a universal location dictionary.",
            "emotion" => $"The repeated presence of {displayValue} is best read as an observed emotional pattern in this journal, not as a diagnosis or prediction.",
            _ => $"Research does not establish one fixed meaning for {displayValue}. A useful lens is how it connects with your waking concerns, emotions, and current experiences."
        };

        var associationSource = type is "person" or "location" ? MemorySource : ContinuitySource;
        return
        [
            new DreamPatternMeaningResponse(association, associationSource),
            new DreamPatternMeaningResponse(
                $"{char.ToUpperInvariant(type[0])}{type[1..]} patterns can be compared across dreams as journal evidence. Frequency and co-occurrence describe the reports; they do not establish cause or a clinical conclusion.",
                ContentSource)
        ];
    }
}
