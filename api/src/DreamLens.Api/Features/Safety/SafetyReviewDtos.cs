namespace DreamLens.Api.Features.Safety;

public sealed record SensitiveSafetyReviewResponse(
    Guid Id,
    Guid DreamId,
    string SubjectPseudonym,
    string Category,
    decimal Confidence,
    string Severity,
    bool RestrictsElaboration,
    string Status,
    DateTimeOffset DetectedAt,
    DateTimeOffset ExpiresAt);

public sealed record SensitiveSafetyRawAccessRequest(string? Purpose);

public sealed record SensitiveSafetyRawAccessResponse(
    Guid SafetyEventId,
    Guid DreamId,
    string DreamText,
    DateTimeOffset ExpiresAt);
