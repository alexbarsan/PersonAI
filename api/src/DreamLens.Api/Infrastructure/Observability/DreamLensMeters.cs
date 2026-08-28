using System.Diagnostics.Metrics;

namespace DreamLens.Api.Infrastructure.Observability;

public static class DreamLensMeters
{
    public const string MeterName = "DreamLens.Api";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Counter<long> RateLimitRejections = Meter.CreateCounter<long>(
        "dreamlens.rate_limit.rejections",
        unit: "requests",
        description: "Requests rejected by rate limiting.");

    public static readonly Counter<long> QuotaRejections = Meter.CreateCounter<long>(
        "dreamlens.quota.rejections",
        unit: "requests",
        description: "Dream submissions rejected by quota checks.");

    public static readonly Counter<long> ProviderFailures = Meter.CreateCounter<long>(
        "dreamlens.provider.failures",
        unit: "calls",
        description: "AI provider calls that failed or returned invalid output.");

    public static readonly Counter<long> AsyncJobsCompleted = Meter.CreateCounter<long>(
        "dreamlens.async_jobs.completed",
        unit: "jobs",
        description: "Asynchronous jobs completed successfully.");

    public static readonly Counter<long> AsyncJobsRetried = Meter.CreateCounter<long>(
        "dreamlens.async_jobs.retried",
        unit: "jobs",
        description: "Asynchronous jobs scheduled for another processing attempt.");

    public static readonly Counter<long> AsyncJobsFailed = Meter.CreateCounter<long>(
        "dreamlens.async_jobs.failed",
        unit: "jobs",
        description: "Asynchronous jobs that exhausted their processing attempts.");

    public static readonly Counter<long> AsyncJobsLeaseSkipped = Meter.CreateCounter<long>(
        "dreamlens.async_jobs.lease_skipped",
        unit: "messages",
        description: "Queue messages skipped because their jobs could not be leased.");

    public static readonly Histogram<double> AsyncJobProcessingDuration = Meter.CreateHistogram<double>(
        "dreamlens.async_jobs.processing.duration",
        unit: "ms",
        description: "End-to-end processing duration for an asynchronous job message.");
}
