using PersonaKit.Context;

namespace PersonaKit.Pipeline;

public sealed record InterpretationRequest(
    string PersonaId,
    ContextBuildRequest ContextRequest,
    InterpretationExecutionOptions? Execution = null);

public sealed record InterpretationExecutionOptions(
    string? ModelId = null,
    int? MaxOutputTokens = null,
    float? Temperature = null);
