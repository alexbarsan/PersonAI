namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamDeepInterpretationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public required string ResultJson { get; set; }

    public string SourcesJson { get; set; } = "[]";

    public required string Provider { get; set; }

    public required string Model { get; set; }

    public required string PersonaVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
