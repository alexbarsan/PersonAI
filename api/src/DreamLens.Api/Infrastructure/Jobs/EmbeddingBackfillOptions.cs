namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class EmbeddingBackfillOptions
{
    public bool Enabled { get; set; }

    public int BatchSize { get; set; } = 100;

    public int MaxJobsPerRun { get; set; } = 1000;
}
