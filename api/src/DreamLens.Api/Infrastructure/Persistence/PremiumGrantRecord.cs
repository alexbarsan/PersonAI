namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class PremiumGrantRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserSubject { get; set; }

    public required string EmailNormalized { get; set; }

    public required string GrantedBySubject { get; set; }

    public required string GrantedByEmail { get; set; }

    public DateTimeOffset GrantedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? RevokedAt { get; set; }

    public string? RevokedBySubject { get; set; }

    public DateTimeOffset? PremiumWelcomeEmailSentAt { get; set; }

    public string? PremiumWelcomeEmailProviderMessageId { get; set; }
}
