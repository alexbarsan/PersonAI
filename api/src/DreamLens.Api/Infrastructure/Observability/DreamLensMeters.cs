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
}
