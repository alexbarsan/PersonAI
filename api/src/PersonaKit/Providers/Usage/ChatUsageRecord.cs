namespace PersonaKit.Providers.Usage;

public sealed record ChatUsageRecord(
    string Model,
    TimeSpan Latency,
    long? InputTokens,
    long? OutputTokens,
    long? TotalTokens,
    decimal EstimatedCostUsd);
