namespace PersonaKit.Pipeline;

public sealed record InterpretationRecord(
    string Id,
    string PersonaId,
    string Summary,
    string OutputJson,
    DateTimeOffset CreatedAt);
