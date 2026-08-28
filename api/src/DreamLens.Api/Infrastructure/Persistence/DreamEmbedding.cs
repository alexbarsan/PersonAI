using Pgvector;

namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamEmbedding
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public required Vector Embedding { get; set; }

    public required string Provider { get; set; }

    public required string Model { get; set; }

    public int Dimensions { get; set; }

    public required string Version { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
