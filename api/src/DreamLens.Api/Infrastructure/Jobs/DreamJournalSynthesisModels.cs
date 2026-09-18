namespace DreamLens.Api.Infrastructure.Jobs;

public sealed record DreamJournalSynthesisDocument(
    string Summary,
    DreamJournalSynthesisObservation[] Observations,
    string[] ReflectionQuestions);

public sealed record DreamJournalSynthesisObservation(
    string Title,
    string Reflection,
    Guid[] EvidenceDreamIds);
