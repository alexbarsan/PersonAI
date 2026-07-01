namespace PersonaKit.Personas;

internal sealed record PersonaConfig(
    string Id,
    string Version,
    string DisplayName,
    string PromptTemplate,
    string OutputSchema,
    string SectionMap);
