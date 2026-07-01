namespace PersonaKit.Pipeline;

public sealed record InterpretationResponse(
    string Id,
    InterpretationStatus Status,
    InterpretationResult? Result,
    string? ErrorMessage);

public enum InterpretationStatus
{
    Completed,
    Failed
}

public sealed record InterpretationResult(
    string Summary,
    InterpretationSection[] Sections,
    string[] FollowUpQuestions,
    string RawJson);

public sealed record InterpretationSection(string Kind, string Title, object? Content);
