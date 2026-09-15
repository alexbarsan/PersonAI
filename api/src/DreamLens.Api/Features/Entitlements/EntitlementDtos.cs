namespace DreamLens.Api.Features.Entitlements;

public sealed record EntitlementResponse(
    string Tier,
    int DailyDreamLimit,
    bool DeepAnalysisEnabled,
    bool QuotaExempt,
    int AskDailyLimit,
    int? AskRemaining,
    DateTimeOffset? AskResetsAt,
    bool AskQuotaExempt);
