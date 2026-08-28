namespace DreamLens.Api.Infrastructure.Jobs;

public sealed class AsyncJobWorkerOptions
{
    public bool Enabled { get; set; }

    public int MaxAttempts { get; set; } = 5;

    public int PollWaitSeconds { get; set; } = 20;
}
