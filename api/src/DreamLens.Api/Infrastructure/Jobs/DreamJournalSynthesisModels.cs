namespace DreamLens.Api.Infrastructure.Jobs;

public sealed record DreamJournalSynthesisDocument(
    string Summary,
    DreamJournalSynthesisObservation[] Observations,
    string[] ReflectionQuestions,
    DreamJournalSynthesisPattern[]? Patterns = null);

public sealed record DreamJournalSynthesisObservation(
    string Title,
    string Reflection,
    Guid[] EvidenceDreamIds);

public sealed record DreamJournalSynthesisPattern(
    string Type,
    string Value,
    string Reflection,
    Guid[] EvidenceDreamIds);
