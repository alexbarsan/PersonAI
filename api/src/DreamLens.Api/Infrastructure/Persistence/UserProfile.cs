namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class UserProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserSubject { get; set; }

    public int? Age { get; set; }

    public string? Sex { get; set; }

    public string? GenderIdentity { get; set; }

    public string Language { get; set; } = "en";

    public string Timezone { get; set; } = "UTC";

    public required string EncryptedTraitsJson { get; set; }

    public bool ConsentAiProcessing { get; set; }

    public bool ConsentSensitiveTraits { get; set; }

    public bool ConsentHistoryUse { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
