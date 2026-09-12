namespace DreamLens.Api.Features.AdminOperations;

public sealed record AdminOperationsResponse(
    DateTimeOffset GeneratedAt,
    AdminOperationsQueueResponse Queue,
    AdminOperationsJobSummaryResponse Jobs,
    AdminOperationsWorkloadResponse[] Workloads,
    AdminOperationsIssueResponse[] Issues,
    AdminOperationsProviderResponse[] Providers);

public sealed record AdminOperationsQueueResponse(
    string Status,
    int Available,
    int InFlight,
    int Delayed,
    int DeadLetter,
    string? Error);

public sealed record AdminOperationsJobSummaryResponse(
    int Pending,
    int Processing,
    int Failed,
    long? OldestPendingSeconds);

public sealed record AdminOperationsWorkloadResponse(
    string Source,
    int Pending,
    int Processing,
    int Failed,
    long? OldestActiveSeconds);

public sealed record AdminOperationsIssueResponse(
    Guid Id,
    Guid? JobId,
    string Source,
    string OperationType,
    string Status,
    int AttemptCount,
    long AgeSeconds,
    string? Failure,
    bool Acknowledged,
    bool CanRequeue,
    DateTimeOffset UpdatedAt);

public sealed record AdminOperationsProviderResponse(
    string Provider,
    string OperationType,
    int Operations,
    int Failed,
    decimal EstimatedCostUsd,
    long AverageLatencyMilliseconds,
    long P95LatencyMilliseconds);

public sealed record AdminOperationsActionRequest(string? Reason);

public sealed record AdminOperationsActionResponse(
    Guid TargetId,
    Guid? JobId,
    string Action,
    string Status,
    DateTimeOffset CreatedAt);
