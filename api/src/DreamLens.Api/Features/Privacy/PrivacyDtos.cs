using DreamLens.Api.Features.Profile;

namespace DreamLens.Api.Features.Privacy;

public sealed record AnonymizationRequestResponse(
    Guid Id,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? ReviewedAt,
    DateTimeOffset? CompletedAt);

public sealed record AdminAnonymizationRequestResponse(
    Guid Id,
    string RequesterPseudonym,
    string Status,
    DateTimeOffset RequestedAt);

public sealed record UserDataExportResponse(
    DateTimeOffset GeneratedAt,
    ProfileResponse Profile,
    UserDataExportDream[] Dreams,
    UserDataExportCost[] AiOperations);

public sealed record UserDataExportDream(
    Guid Id,
    DateTimeOffset CreatedAt,
    string Text,
    string? Mood,
    int? SleepQuality,
    string[] Tags,
    string? OccurredAt,
    string? JournalNote,
    string Status,
    string? ResultJson,
    string? ErrorMessage,
    UserDataExportFact[] Facts,
    UserDataExportImage[] Images);

public sealed record UserDataExportFact(string Type, string Value, decimal? Score, decimal? ExtractionConfidence);

public sealed record UserDataExportImage(Guid Id, string Status, string Style, string? DownloadUrl, DateTimeOffset CreatedAt);

public sealed record UserDataExportCost(
    Guid Id,
    Guid? DreamId,
    string Provider,
    string Model,
    string OperationType,
    string Status,
    int AttemptCount,
    long LatencyMilliseconds,
    decimal EstimatedCostUsd,
    DateTimeOffset CreatedAt);
