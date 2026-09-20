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

    public static DreamPatternMeaningResponse[] GetResearchLenses(string type)
    {
        var association = type switch
        {
            "person" => "Research on dream social content can be a useful lens for considering memory incorporation and interpersonal simulation. It does not assign a fixed meaning to any individual person.",
            "location" => "Autobiographical and spatial memory can shape dream locations. A place's personal history and emotional tone are more useful than a universal location dictionary.",
            "emotion" => "Dream affect can be explored through emotional continuity and waking-life incorporation. Repeated emotion is an observation in this journal, not a diagnosis or prediction.",
            "scenario" => "Recurring scenarios can be considered alongside waking concerns and continuity across reports. The research lens does not establish a single meaning for a scenario.",
            _ => "Memory incorporation and dream content analysis offer context for recurring details. They do not establish a fixed universal meaning for a symbol or object."
        };

        var associationSource = type is "person" or "location" ? MemorySource : ContinuitySource;
        return
        [
            new DreamPatternMeaningResponse(association, associationSource),
            new DreamPatternMeaningResponse(
                "Frequency and co-occurrence describe reported dream content. They do not establish cause, a universal symbol dictionary, or a clinical conclusion.",
                ContentSource)
        ];
    }
}
