namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamJournalSynthesisRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string UserSubject { get; set; }

    public required string EncryptedResultJson { get; set; }

    public int SourceDreamCount { get; set; }

    public DateTimeOffset SourceLatestDreamAt { get; set; }

    public required string Provider { get; set; }

    public required string Model { get; set; }

    public required string PromptVersion { get; set; }

    public DateTimeOffset GeneratedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
