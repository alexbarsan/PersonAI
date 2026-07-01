namespace DreamLens.Api.Infrastructure.RateLimiting;

public sealed class DreamRateLimitOptions
{
    public int PermitLimit { get; set; } = 100;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);

    public int QueueLimit { get; set; }
}
