namespace PersonaKit.Context;

public sealed record ContextBuildRequest(
    string RequestId,
    string Locale,
    ContextPersona Persona,
    ContextUserSource User,
    ContextHistory? History,
    DreamInput Input);

public sealed record ContextPersona(string Id, string Version);

public sealed record ContextUserSource(
    string InternalUserId,
    string? Email,
    string? Name,
    int? Age,
    string? Sex,
    string? GenderIdentity,
    string Language,
    string Timezone,
    ContextTraits Traits,
    ContextConsent Consent);

public sealed record ContextTraits(
    string[] Fears,
    string[] Allergies,
    string[] Interests,
    string? Occupation,
    string? RelationshipStatus,
    string? CulturalBackground,
    string? SleepPattern,
    string? StressLevel,
    string[] RecentLifeEvents);

public sealed record ContextConsent(bool AiProcessing, bool SensitiveTraits, bool HistoryUse);

public sealed record ContextHistory(string[] RecentThemes, int InteractionCount, string? LastSummary);

public sealed record DreamInput(
    string Text,
    string? Mood,
    int? SleepQuality,
    string[] Tags,
    string? OccurredAt);
