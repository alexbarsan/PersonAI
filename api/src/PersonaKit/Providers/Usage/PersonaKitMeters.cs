using System.Diagnostics.Metrics;

namespace PersonaKit.Providers.Usage;

public static class PersonaKitMeters
{
    public const string MeterName = "PersonaKit";

    private static readonly Meter Meter = new(MeterName, "1.0.0");

    public static readonly Histogram<double> AiEstimatedCostUsd = Meter.CreateHistogram<double>(
        "personakit.ai.estimated_cost_usd",
        unit: "USD",
        description: "Estimated AI provider cost in USD.");

    public static readonly Histogram<double> AiTokens = Meter.CreateHistogram<double>(
        "personakit.ai.tokens",
        unit: "tokens",
        description: "AI provider token usage.");
}
