namespace DreamLens.Api.Features.Profile;

public sealed record ProfileResponse(
    int? Age,
    string? Sex,
    string? GenderIdentity,
    string Language,
    string Timezone,
    ProfileTraitsDto Traits,
    ConsentDto Consent);

public sealed record UpdateProfileRequest(
    int? Age,
    string? Sex,
    string? GenderIdentity,
    string Language,
    string Timezone,
    ProfileTraitsDto? Traits,
    ConsentDto? Consent);

public sealed record ProfileTraitsDto(
    string[] Fears,
    string[] Allergies,
    string[] Interests,
    string? Occupation,
    string? RelationshipStatus,
    string? CulturalBackground,
    string? SleepPattern,
    string? StressLevel,
    string[] RecentLifeEvents)
{
    public static ProfileTraitsDto Empty { get; } = new(
        [],
        [],
        [],
        null,
        null,
        null,
        null,
        null,
        []);
}

public sealed record ConsentDto(bool AiProcessing, bool SensitiveTraits, bool HistoryUse)
{
    public static ConsentDto Empty { get; } = new(false, false, false);
}
