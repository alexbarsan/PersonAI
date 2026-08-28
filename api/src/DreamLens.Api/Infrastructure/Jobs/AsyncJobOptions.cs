namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class AsyncJobOptions
{
    public string QueueUrl { get; set; } = "";

    public int VisibilityTimeoutSeconds { get; set; } = 300;
}
