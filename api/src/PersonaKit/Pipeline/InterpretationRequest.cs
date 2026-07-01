using PersonaKit.Context;

namespace PersonaKit.Pipeline;

public sealed record InterpretationRequest(string PersonaId, ContextBuildRequest ContextRequest);
