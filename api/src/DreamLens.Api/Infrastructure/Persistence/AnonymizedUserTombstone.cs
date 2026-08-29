namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class AnonymizedUserTombstone
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string SubjectPseudonym { get; set; }

    public DateTimeOffset AnonymizedAt { get; set; } = DateTimeOffset.UtcNow;
}
