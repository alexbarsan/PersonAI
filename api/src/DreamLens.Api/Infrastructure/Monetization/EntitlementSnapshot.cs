namespace DreamLens.Api.Infrastructure.Monetization;

public sealed record EntitlementSnapshot(
    EntitlementTier Tier,
    int DailyDreamLimit,
    bool DeepAnalysisEnabled);
