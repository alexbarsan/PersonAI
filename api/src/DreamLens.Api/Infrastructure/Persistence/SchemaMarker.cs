namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class SchemaMarker
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
