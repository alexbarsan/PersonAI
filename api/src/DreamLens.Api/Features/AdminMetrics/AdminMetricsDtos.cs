namespace DreamLens.Api.Features.AdminMetrics;

public sealed record AdminMetricsResponse(
    DateOnly From,
    DateOnly To,
    int ActiveUsers,
    int PremiumActiveUsers,
    decimal PremiumConversionPercent,
    int DreamsSubmitted,
    int DreamsCompleted,
    decimal CompletionPercent,
    decimal AiCostUsd,
    decimal AiCostPerActiveUserUsd,
    AdminMetricCostBreakdownResponse[] AiCostsByOperation,
    AdminMetricUnavailableValueResponse Revenue,
    AdminMetricUnavailableValueResponse AwsCost,
    AdminMetricUnavailableValueResponse GrossMargin);

public sealed record AdminMetricCostBreakdownResponse(
    string OperationType,
    int Operations,
    int SuccessfulOperations,
    decimal EstimatedCostUsd,
    long AverageLatencyMilliseconds,
    long P95LatencyMilliseconds);

public sealed record AdminMetricUnavailableValueResponse(string Status, string Reason);
