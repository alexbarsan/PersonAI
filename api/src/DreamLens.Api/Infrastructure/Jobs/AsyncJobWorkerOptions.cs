namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class AsyncJobWorkerOptions
{
    public bool Enabled { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public int PollWaitSeconds { get; set; } = 20;

    public int RetryBaseDelaySeconds { get; set; } = 60;

    public int RetryMaxDelaySeconds { get; set; } = 3600;
}
