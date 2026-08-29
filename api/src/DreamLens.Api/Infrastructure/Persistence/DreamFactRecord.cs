namespace DreamLens.Api.Infrastructure.Persistence;

public sealed class DreamFactRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid DreamId { get; set; }

    public required string UserSubject { get; set; }

    public required string FactType { get; set; }

    public required string NormalizedValue { get; set; }

    public required string DisplayValue { get; set; }

    public decimal? Score { get; set; }

    public decimal? ExtractionConfidence { get; set; }

    public required string SourceSchemaVersion { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
